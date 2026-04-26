# Architecture overview

## Intent

This application establishes the operational foundation for PropOps Copilot before any AI or inference modules are introduced.

## Domain focus

The current bounded context is **maintenance intake**. The core aggregate is `MaintenanceRequest`, which captures:

- resident or submitter details
- property and unit details
- issue description
- category, priority, and channel metadata
- operational assignment and response target

## Backend layering

The .NET solution follows a DDD-oriented structure:

| Layer | Responsibility |
| --- | --- |
| `PropOpsCopilot.Domain` | Domain entities and enums |
| `PropOpsCopilot.Application` | Use-case services, DTOs, and repository abstractions |
| `PropOpsCopilot.Infrastructure` | EF Core persistence, repository implementations, and seed data |
| `PropOpsCopilot.Api` | REST endpoints, dependency wiring, CORS, and Swagger |

## Frontend structure

The Angular application uses standalone components and is organized around:

- a shared application shell
- a **Login** page for authenticated internal staff access
- an **Overview** page for operational visibility
- an **Intake** page for structured request creation
- a simple API service and typed models for backend communication

## Portal identity

The portal now uses **ASP.NET Core Identity** for multi-role authentication.

- `AppUser` is stored in PostgreSQL alongside the operational data model
- staff authenticate through `/api/auth/login`
- the Angular application stores the returned JWT bearer token and attaches it to protected API calls
- overview endpoints are restricted to staff roles
- maintenance request creation is allowed for staff, tenants, and property owners
- vendor accounts authenticate successfully but remain outside staff-only and request-creation APIs for now

## Container topology

`docker-compose.yml` starts three services:

1. `postgres` for persistence
2. `api` for the ASP.NET Core backend
3. `frontend` for the Angular application served by Nginx

## Future extension path

The current architecture leaves room for future modules such as:

- policy and SOP retrieval
- AI triage orchestration
- guardrails and confidence scoring
- dispatch workflows and vendor integration

Those capabilities can be introduced as separate application services or bounded contexts without replacing the initial intake foundation.
