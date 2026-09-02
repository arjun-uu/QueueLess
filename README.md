# QueueLess

**QueueLess** is a queue management and appointment system designed to reduce physical waiting times and provide a better experience for both customers and service providers.

The application allows users to join and manage queues digitally instead of waiting physically at a service location.

## Project Goals

* Reduce physical waiting time
* Allow users to join queues remotely
* Provide real-time queue status
* Manage appointments and queue entries
* Help service providers efficiently manage their queues
* Provide a simple and responsive user experience

## Tech Stack

### Frontend

* React
* TypeScript
* Vite
* Tailwind CSS

### Backend

* ASP.NET Core Web API
* C#
* Entity Framework Core
* SQL Server
* JWT Authentication

### Development

* Git & GitHub
* REST APIs
* Swagger / OpenAPI

## Project Structure

```text
QueueLess/
│
├── Frontend/
│   └── React + TypeScript application
│
├── QueueLessAPI/
│   ├── API/
│   ├── Application/
│   ├── Domain/
│   ├── Infrastructure/
│   └── Shared/
│
├── .gitignore
└── README.md
```

## Architecture

QueueLess follows a **Clean Architecture** approach:

```text
┌─────────────────────┐
│      Frontend       │
│   React + TypeScript │
└──────────┬──────────┘
           │ REST API
           ▼
┌─────────────────────┐
│        API          │
│    Controllers      │
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│    Application      │
│ Services / DTOs     │
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│       Domain        │
│ Entities / Rules    │
└──────────┬──────────┘
           ▼
┌─────────────────────┐
│   Infrastructure    │
│ EF Core / Database  │
└─────────────────────┘
```

## Git Workflow

The project uses a feature-based Git workflow:

```text
feature/task
      │
      ▼
    PR
      │
      ▼
development
      │
      ▼
    PR
      │
      ▼
main
```

### Create a new feature

```bash
git checkout development
git pull origin development
git checkout -b feature/my-task
```

### Commit and push

```bash
git add .
git commit -m "Implement my task"
git push -u origin feature/my-task
```

Then create a Pull Request:

```text
feature/my-task → development
```

When the changes are ready for release:

```text
development → main
```

## Current Status

**Project:** Initial development

Currently implemented:

* Project structure
* Clean Architecture foundation
* ASP.NET Core Web API setup
* Application dependency injection
* Infrastructure dependency injection
* Swagger/OpenAPI configuration
* JWT Bearer authorization support in Swagger

## Planned Features

* [ ] User registration and login
* [ ] JWT access and refresh tokens
* [ ] Role-based authorization
* [ ] Queue creation and management
* [ ] Digital queue joining
* [ ] Queue position tracking
* [ ] Appointment management
* [ ] Real-time queue updates
* [ ] Notifications
* [ ] Admin/service-provider dashboard
* [ ] Customer dashboard
* [ ] Deployment

## Getting Started

### Backend

Navigate to the API project:

```bash
cd QueueLessAPI
```

Run the application:

```bash
dotnet run
```

Swagger will be available at:

```text
https://localhost:<port>/swagger
```

### Frontend

Navigate to the frontend:

```bash
cd Frontend
```

Install dependencies:

```bash
npm install
```

Start the development server:

```bash
npm run dev
```

## Environment Variables

Environment-specific configuration should be stored in `.env` files locally and **should not be committed to Git**.

Example:

```env
VITE_API_URL=https://localhost:<api-port>
```

## License

This project is currently under development. License information will be added later.
