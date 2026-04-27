# Architecture overview

## Intent

This application establishes the operational foundation for PropOps Copilot with a live split-runtime design: the portal and operational workflows run through the .NET API, while Levels 2 and 3 run in a separate Python AI service for rules retrieval, LangGraph-orchestrated baseline inference, and guardrails.

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

The .NET API remains the single backend surface for the Angular frontend. Any current or future AI task must be delegated from the API to the separate Python AI service rather than implemented inside the .NET portal runtime.

## AI runtime boundary

The repository now reserves `src/ai/propops-ai-service` for all AI-specific work, including:

- retrieval and business-rule orchestration for AI flows
- LangGraph state orchestration, prompt construction, and model adapters
- guardrails and evaluation pipelines
- fine-tuned model integration and vLLM-facing logic

The Python service is intentionally isolated behind an internal HTTP boundary:

1. Angular calls the .NET API.
2. The .NET API performs identity, validation, persistence, and workflow orchestration.
3. When AI is needed, the .NET API calls the Python AI service.
4. The Python service can then call model hosts such as vLLM without exposing them to the frontend.

Levels 2 and 3 currently use this split to implement:

- a structured maintenance knowledge base in the Python service
- retrieval of maintenance SOPs, vendor routing rules, emergency policy, and property notes
- explicit AI triage input/output contracts exposed back through the .NET API
- a baseline inference path orchestrated through LangGraph with explicit guardrail validation and human-review fallback

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

`docker-compose.yml` starts four primary services plus one optional test service:

1. `postgres` for persistence
2. `ai-service` for the dedicated Python AI runtime
3. `api` for the ASP.NET Core backend
4. `frontend` for the Angular application served by Nginx
5. `playwright` as an on-demand browser test runner in the `test` profile

## Future extension path

The current architecture leaves room for future modules such as:

- stronger instruct-model integration behind the same adapter boundary
- dispatch workflows and vendor integration
- human-review tooling and feedback capture

Those capabilities can be introduced without replacing the initial intake foundation because the user-facing API and the AI runtime are already separated.
