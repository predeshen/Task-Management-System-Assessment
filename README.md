# Task Management System

A modern, full-stack task management application built with Angular and .NET Core.

## Quick Start

### Option 1: Automated Setup (Recommended)
1. Clone the repository
2. Run the setup script
3. Start using the app!

```bash
git clone https://github.com/predeshen/Task-Management-System-Assessment.git
cd Task-Management-System-Assessment
setup-development.bat
```

The script will automatically:
- Check prerequisites
- Install dependencies
- Set up the database
- Build the projects
- Start both servers
- Open the app in your browser

### Option 2: Manual Setup

#### Prerequisites
Make sure you have these installed on your PC:

- **Node.js 18+** - [Download here](https://nodejs.org/)
- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download)
- **Angular CLI** - Install with: `npm install -g @angular/cli`

#### Step-by-Step Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/predeshen/Task-Management-System-Assessment.git
   cd Task-Management-System-Assessment
   ```

2. **Install frontend dependencies**
   ```bash
   cd frontend
   npm install
   ```

3. **Install backend dependencies**
   ```bash
   cd ../backend
   dotnet restore
   ```

4. **Set up the database**
   ```bash
   cd src/TaskManagement.Api/TaskManagement.Api
   dotnet ef database update
   ```

5. **Start the backend server** (Terminal 1)
   ```bash
   cd backend/src/TaskManagement.Api/TaskManagement.Api
   dotnet run --launch-profile https
   ```

6. **Start the frontend server** (Terminal 2)
   ```bash
   cd frontend
   npm start
   ```

## Access the Application

Once both servers are running, open your browser and go to:

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:4200 | Main application interface |
| **Backend API** | https://localhost:7000 | REST API endpoints |
| **Swagger UI** | https://localhost:7000/swagger | API documentation |

## Login Credentials

The application comes with pre-seeded data. Use these credentials to log in:

```
Username: admin
Password: password123
```

## Features

### User Authentication
- Secure login/logout
- JWT token-based authentication
- Session management

### Task Management
- **Create Tasks** - Add new tasks with title and description
- **View Tasks** - See all your tasks in a clean dashboard
- **Edit Tasks** - Update task details and status
- **Delete Tasks** - Remove tasks with confirmation
- **Task Status** - Track progress (To Do, In Progress, Completed)

## Technology Stack

### Frontend
- **Angular 17** - Modern web framework
- **TypeScript** - Type-safe JavaScript
- **RxJS** - Reactive programming
- **Angular Material** - UI components
- **CSS3** - Modern styling

### Backend
- **.NET 8** - Cross-platform framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - Database ORM
- **SQL Server LocalDB** - Database
- **JWT Authentication** - Security

## 🔧 Development

### Running Tests
```bash
# Backend tests
cd backend
dotnet test

# Frontend tests
cd frontend
npm test
```

### Building for Production
```bash
# Backend
cd backend
dotnet publish -c Release

# Frontend
cd frontend
npm run build --prod
```

## 🐛 Troubleshooting

### Common Issues

**Port already in use:**
- Backend: Change port in `launchSettings.json`
- Frontend: Run `ng serve --port 4201`

**Database connection issues:**
- Ensure SQL Server LocalDB is installed
- Run `dotnet ef database update` again

**Node.js/npm issues:**
- Clear npm cache: `npm cache clean --force`
- Delete `node_modules` and run `npm install` again

**CORS errors:**
- Ensure backend is running on https://localhost:7000
- Check CORS configuration in `Program.cs`

### Getting Help

If you encounter any issues:

1. Check that all prerequisites are installed
2. Ensure both servers are running
3. Check the browser console for errors
4. Verify the backend logs for API errors

## 📝 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | User authentication |
| GET | `/api/tasks` | Get all tasks |
| POST | `/api/tasks` | Create new task |
| PUT | `/api/tasks/{id}` | Update task |
| DELETE | `/api/tasks/{id}` | Delete task |
