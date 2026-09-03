# QueueLess — Healthcare Patient Flow & Appointment Management

**QueueLess** is a healthcare patient-flow and appointment management system designed for **clinics, hospitals, and outpatient departments (OPDs)**.

The platform helps reduce physical waiting times by allowing patients to manage appointments, check in digitally, join virtual queues, track their position in real time, and receive notifications when their consultation is approaching.

Healthcare staff can manage doctors, departments, appointments, patient check-ins, and queues from a centralized system.

## Project Goals

* Reduce patient waiting and congestion in healthcare facilities
* Allow patients to join virtual queues remotely
* Provide real-time queue position and status updates
* Manage appointments, check-ins, and queue entries
* Help doctors and reception staff efficiently manage patient flow
* Provide appointment and queue notifications
* Provide a simple and responsive experience for patients and healthcare staff
* Maintain secure, role-based access to healthcare operational data

## Target Users

QueueLess is designed around the following healthcare roles:

* **Patient** — Books appointments, checks in, joins queues, and tracks queue status
* **Doctor** — Views appointments, manages the patient queue, and handles patient flow
* **Receptionist** — Registers patients, manages appointments, checks patients in, and operates queues
* **Healthcare Admin** — Manages doctors, departments, staff, schedules, queues, and reports
* **Platform Admin** — Manages healthcare organizations and platform-level operations

## Core Healthcare Workflow

```text
Patient
   │
   ├── Book Appointment
   │
   └── Walk-in
          │
          ▼
      Check-In
          │
          ▼
     Queue Token
          │
          ▼
    Virtual Queue
          │
          ▼
    Doctor Calls Patient
          │
          ▼
      Consultation
          │
          ▼
       Completed
```

## Tech Stack

### Frontend

* React
* TypeScript
* Vite
* Tailwind CSS
* Redux Toolkit / RTK Query
* React Router

### Backend

* ASP.NET Core Web API
* C#
* Entity Framework Core
* SQL Server
* ASP.NET Core Identity
* JWT Authentication
* SignalR

### Development

* Git & GitHub
* REST APIs
* Swagger / OpenAPI
* Clean Architecture

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
┌─────────────────────────────┐
│          Frontend           │
│     React + TypeScript      │
└──────────────┬──────────────┘
               │
          REST / SignalR
               │
               ▼
┌─────────────────────────────┐
│             API             │
│         Controllers         │
└──────────────┬──────────────┘
               ▼
┌─────────────────────────────┐
│        Application          │
│     Services / DTOs         │
└──────────────┬──────────────┘
               ▼
┌─────────────────────────────┐
│           Domain            │
│   Entities / Business Rules │
└──────────────┬──────────────┘
               ▼
┌─────────────────────────────┐
│       Infrastructure        │
│     EF Core / Database      │
└──────────────┬──────────────┘
               ▼
         ┌───────────┐
         │ SQL Server│
         └───────────┘
```

## Core Healthcare Modules

The system will be organized around the following healthcare modules:

```text
Authentication
      │
      ├── Users & Roles
      │
      ▼
Healthcare Organization
      │
      ├── Locations
      ├── Departments
      ├── Doctors
      └── Staff
             │
             ▼
        Doctor Schedule
             │
             ▼
        Appointments
             │
             ▼
          Check-In
             │
             ▼
           Queue
             │
             ▼
        Queue Token
             │
             ▼
      Patient Flow
             │
             ▼
       Notifications
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

### Authentication & Authorization

* [ ] User registration and login
* [ ] JWT access and refresh tokens
* [ ] Role-based authorization
* [ ] Permission-based authorization
* [ ] Protected routes
* [ ] Secure token handling

### Healthcare Organization

* [ ] Healthcare organization management
* [ ] Multiple locations
* [ ] Department management
* [ ] Doctor management
* [ ] Staff management
* [ ] Role and permission management

### Patient Management

* [ ] Patient registration
* [ ] Patient profile
* [ ] Patient appointment history
* [ ] Patient queue history

### Doctor & Scheduling

* [ ] Doctor schedules
* [ ] Availability management
* [ ] Healthcare services
* [ ] Appointment slot generation
* [ ] Doctor appointment dashboard

### Appointment Management

* [ ] Book appointments
* [ ] Reschedule appointments
* [ ] Cancel appointments
* [ ] Appointment status management
* [ ] Appointment reminders

### Queue Management

* [ ] Create and configure healthcare queues
* [ ] Digital queue joining
* [ ] Queue token generation
* [ ] Patient check-in
* [ ] Queue position tracking
* [ ] Call next patient
* [ ] Skip patient
* [ ] Recall patient
* [ ] Complete patient visit
* [ ] No-show handling
* [ ] Queue pause/resume

### Real-Time Features

* [ ] SignalR integration
* [ ] Real-time queue position
* [ ] Patient called notifications
* [ ] Queue status updates
* [ ] Doctor dashboard updates
* [ ] Reception dashboard updates

### Notifications

* [ ] In-app notifications
* [ ] Appointment reminders
* [ ] Queue approaching notifications
* [ ] Patient called notifications
* [ ] Email notifications
* [ ] Push notifications — later
* [ ] SMS notifications — later

### Analytics & Reporting

* [ ] Average patient waiting time
* [ ] Average consultation time
* [ ] Patients served
* [ ] No-show statistics
* [ ] Appointment utilization
* [ ] Doctor workload
* [ ] Department workload
* [ ] Peak OPD hours

### Additional Features

* [ ] QR-based patient check-in
* [ ] Multiple doctors per queue
* [ ] Multiple counters
* [ ] Configurable queue priority rules
* [ ] Audit logging
* [ ] Background jobs
* [ ] Smart wait-time estimation
* [ ] AI/ML-based wait-time prediction — later

## MVP Workflow

The first version will focus on the core outpatient patient-flow experience:

```text
1. Patient registers
        ↓
2. Healthcare Admin creates department
        ↓
3. Admin adds doctor
        ↓
4. Doctor schedule is configured
        ↓
5. Patient books appointment
        ↓
6. Patient arrives and checks in
        ↓
7. Queue token is generated
        ↓
8. Patient joins virtual queue
        ↓
9. Doctor calls patient
        ↓
10. Patient receives real-time update
        ↓
11. Doctor completes the visit
```

The goal of the MVP is to make this workflow functional before adding advanced analytics, notifications, QR check-in, AI, and other enhancements.

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

## Scope

QueueLess initially focuses on **outpatient patient-flow and appointment management**.

The initial scope does not include:

* Electronic Medical Records
* Prescriptions
* Full clinical history
* Pharmacy management
* Insurance management
* Laboratory management
* Inpatient/bed management

These may be considered as separate future modules.

The core focus remains:

```text
Appointment / Walk-in
        ↓
     Check-In
        ↓
      Queue
        ↓
   Queue Token
        ↓
     Doctor
        ↓
  Consultation
        ↓
    Completed
```

## License

This project is currently under development. License information will be added later.
