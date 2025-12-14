# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SnapRoll.sln", "./"]
COPY ["src/SnapRoll.API/SnapRoll.API.csproj", "src/SnapRoll.API/"]
COPY ["src/SnapRoll.Application/SnapRoll.Application.csproj", "src/SnapRoll.Application/"]
COPY ["src/SnapRoll.Infrastructure/SnapRoll.Infrastructure.csproj", "src/SnapRoll.Infrastructure/"]
COPY ["src/SnapRoll.Domain/SnapRoll.Domain.csproj", "src/SnapRoll.Domain/"]

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build and Publish
WORKDIR "/src/src/SnapRoll.API"
RUN dotnet publish -c Release -o /app/publish

# Use the runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SnapRoll.API.dll"]
