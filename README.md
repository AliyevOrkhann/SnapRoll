## SnapRoll

SnapRoll is a real-time attendance tracking system for classrooms. Instructors create courses and live sessions, students join via QR codes, and the system records attendance and scan logs with live updates.

### Tech stack

- **Backend**: ASP.NET Core 8 (`SnapRoll.API`)
- **Application layer**: `SnapRoll.Application`
- **Domain layer**: `SnapRoll.Domain`
- **Infrastructure**: `SnapRoll.Infrastructure` (EF Core, database access, services)
- **Frontend**: React + Vite + Tailwind CSS (`client`)

- **PostgreSQL** instance (local or remote)

### Features

- **Authentication**: Secure Instructor / Student login.
- **Courses & Sessions**: Instructors manage courses and live sessions.
- **Secure QR Attendance**: 
  - Dynamic rotational QR codes (updates every 2s).
  - Strict 4-second validity window to prevent code sharing.
  - **Privacy First**: No GPS location tracking required.
- **Real-time Updates**: SignalR hub broadcasts instant attendance stats.
- **Dashboards**: Live analytics for instructors.

## Prerequisites

- **.NET 8 SDK**
- **Node.js 18+** and **npm**
- A **SQL Server** instance (local or remote)

---

## Backend setup (API)

1. **Configure database & JWT/QR settings**

   Edit `src/SnapRoll.API/appsettings.Development.json`:

   - Set a valid **connection string** under `ConnectionStrings`.
   - Fill in the **JWT** section to match `JwtSettings` (issuer, audience, signing key).
   - Fill in the **QR** settings to match `QrSettings` (e.g. secret, expiry).

2. **Apply database migrations** (optional but recommended)

   From the solution root:

   ```bash
   cd src/SnapRoll.API
   dotnet ef database update
   ```

   Requires the .NET EF Core CLI tools installed:

   ```bash
   dotnet tool install --global dotnet-ef
   ```

3. **Run the API**

   From `src/SnapRoll.API`:

   ```bash
   dotnet run
   ```

   By default the API will start on the port configured in `launchSettings.json`
   (for example `https://localhost:5001`).

---

## Frontend setup (client)

1. **Install dependencies**

   From the project root:

   ```bash
   cd client
   npm install
   ```

2. **Configure frontend environment (optional)**

   If needed, create `client/.env` (or `.env.local`) and set the API base URL, for example:

   ```env
   VITE_API_BASE_URL=https://localhost:5001
   ```

3. **Run the dev server**

   ```bash
   npm run dev
   ```

   Vite will start on a port like `http://localhost:5173`.  
   The frontend is configured to talk to the backend API via the base URL in
   `VITE_API_BASE_URL` or the default used in `client/src/api/axios.js`.

---

## Running the whole system

1. **Terminal 1 – Backend**

   ```bash
   cd src/SnapRoll.API
   dotnet run
   ```

2. **Terminal 2 – Frontend**

   ```bash
   cd client
   npm run dev
   ```

3. Open the frontend URL (e.g. `http://localhost:5173`) in your browser, log in as instructor/student, and use the UI to manage courses, sessions, and attendance.

---

## Development notes

- **Solution file**: Open `SnapRoll.sln` in Visual Studio / Rider to work on the backend.
- **Client**: Open `client` in VS Code or your preferred editor.
- **Migrations**: New DB changes should be added via EF Core migrations in `SnapRoll.Infrastructure/Migrations`.