using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SnapRoll.API.Hubs;
using SnapRoll.Application.Configuration;
using SnapRoll.Application.Interfaces;
using SnapRoll.Application.Services;
using SnapRoll.Domain.Entities;
using SnapRoll.Infrastructure.Data;
using SnapRoll.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<QrSettings>(builder.Configuration.GetSection(QrSettings.SectionName));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Parse Render's postgres:// URL if applicable
if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
{
    var databaseUri = new Uri(connectionString);
    string dbName = databaseUri.AbsolutePath.TrimStart('/');
    string[] userInfo = databaseUri.UserInfo.Split(new[] { ':' }, 2);
    
    // Default to port 5432 if not specified (common in postgresql:// URIs)
    int port = databaseUri.Port > 0 ? databaseUri.Port : 5432;
    
    // Correctly split username and password
    string username = userInfo[0];
    string password = userInfo.Length > 1 ? userInfo[1] : "";
    
    // Determine SSL mode: Internal (dpg-...) connections on Render do not use SSL. External do.
    // If we enforce SSL on internal connections, it will fail.
    bool isInternal = !databaseUri.Host.Contains("render.com");
    string sslMode = isInternal ? "Disable" : "Require";

    connectionString = $"Host={databaseUri.Host};Port={port};Database={dbName};Username={username};Password={password};SslMode={sslMode};Trust Server Certificate=true";
}

builder.Services.AddDbContext<SnapRollDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<SnapRollDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // SignalR JWT configuration
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddSingleton<IQrEngine, QrEngine>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Controllers & SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "SnapRoll API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token"
    });
    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(
                    "http://localhost:3000", "https://localhost:3000",
                    "http://localhost:5173", "https://localhost:5173",
                    "http://10.0.8.13:5173",
                    "https://snaproll.onrender.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SnapRollDbContext>();
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SessionHub>("/hubs/session");

app.Run();
