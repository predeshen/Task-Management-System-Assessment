@echo off
echo ========================================
echo    Task Management System Setup
echo ========================================
echo.

echo Checking prerequisites...
echo.

:: Check Node.js
node --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Node.js not found. Please install Node.js 18 or later.
    echo Download from: https://nodejs.org/
    pause
    exit /b 1
) else (
    echo [OK] Node.js found: 
    node --version
)

:: Check .NET
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK not found. Please install .NET 8 SDK.
    echo Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
) else (
    echo [OK] .NET SDK found: 
    dotnet --version
)

:: Angular CLI will be installed locally with npm install, so no global check needed
echo [INFO] Angular CLI will be available after npm install

echo.
echo ========================================
echo Installing Dependencies...
echo ========================================

echo.
echo [1/4] Installing frontend dependencies...
cd frontend
call npm install
if %errorlevel% neq 0 (
    echo [ERROR] Failed to install frontend dependencies
    pause
    exit /b 1
) else (
    echo [OK] Frontend dependencies installed
)

echo.
echo [2/4] Restoring backend dependencies...
cd ..\backend
call dotnet restore
if %errorlevel% neq 0 (
    echo [ERROR] Failed to restore backend dependencies
    pause
    exit /b 1
) else (
    echo [OK] Backend dependencies restored
)

echo.
echo [3/4] Setting up database...
cd src\TaskManagement.Api\TaskManagement.Api
call dotnet ef database update
if %errorlevel% neq 0 (
    echo [ERROR] Database setup failed
    pause
    exit /b 1
) else (
    echo [OK] Database created and seeded
)

echo.
echo [4/4] Building backend...
call dotnet build
if %errorlevel% neq 0 (
    echo [ERROR] Backend build failed
    pause
    exit /b 1
) else (
    echo [OK] Backend build successful
)

:: Go back to root directory
cd ..\..\..\..
echo [OK] Setup complete - frontend will build automatically when started

echo.
echo ========================================
echo Starting Development Servers...
echo ========================================

echo.
echo Starting backend server...
start "Backend Server" cmd /k "cd backend\src\TaskManagement.Api\TaskManagement.Api && dotnet run --launch-profile https"

echo Waiting for backend to start...
timeout /t 5 /nobreak >nul

echo.
echo Starting frontend server...
start "Frontend Server" cmd /k "cd frontend && npx ng serve"

echo.
echo ========================================
echo Setup Complete!
echo ========================================
echo.
echo Your Task Management System is starting up...
echo.
echo URLs:
echo  Frontend:  http://localhost:4200
echo  Backend:   https://localhost:7000
echo  Swagger:   https://localhost:7000/swagger
echo.
echo Login Credentials:
echo  Username: admin
echo  Password: password123
echo.
echo The application will open automatically in a few seconds...
echo.

timeout /t 10 /nobreak >nul
start http://localhost:4200

echo Press any key to exit...
pause >nul