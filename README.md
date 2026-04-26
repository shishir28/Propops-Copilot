# PropOps Copilot

PropOps Copilot is the first UI-first foundation for a property maintenance operations platform. This version focuses on structured intake, queue visibility, and a DDD-oriented application backbone using Angular, ASP.NET Core, and PostgreSQL.

## Stack

- **Frontend:** Angular 21 with standalone components and modern SCSS styling
- **Backend:** ASP.NET Core Web API with Domain, Application, Infrastructure, and Api projects
- **Persistence:** PostgreSQL via Entity Framework Core
- **Local orchestration:** Docker Compose

## Project structure

```text
.
├── docker-compose.yml
├── docs/
│   └── architecture.md
├── src/
│   ├── backend/
│   │   ├── PropOpsCopilot.Api/
│   │   ├── PropOpsCopilot.Application/
│   │   ├── PropOpsCopilot.Domain/
│   │   └── PropOpsCopilot.Infrastructure/
│   └── frontend/
└── propops-copilot-flow.txt
```

## Running with Docker Compose

```bash
docker compose up --build
```

Once the stack is up:

- **Frontend:** http://localhost:4315
- **API:** http://localhost:8095
- **Swagger:** http://localhost:8095/swagger/index.html

## Running locally without Docker

### Backend

Start PostgreSQL locally, then run:

```bash
cd src/backend/PropOpsCopilot.Api
dotnet run
```

The API expects a PostgreSQL database at:

```text
Host=localhost;Port=5432;Database=propops;Username=propops;Password=propops
```

### Frontend

```bash
cd src/frontend
npm install
npm start
```

The frontend runs on `http://localhost:4200` in local dev and calls the API at `http://<hostname>:8095/api`.

## Current capabilities

- Secure internal portal login with ASP.NET Core Identity and JWT-based API auth
- Review maintenance intake KPIs on the overview screen
- Inspect recent maintenance requests
- Create new maintenance requests through the Angular UI
- Persist requests to PostgreSQL
- Seed a starter queue for local development

## Portal authentication

The portal supports these authenticated user types:

- **Property manager:** `manager@propops.local` / `PropOps!Manager1`
- **Dispatcher:** `dispatcher@propops.local` / `PropOps!Dispatch1`
- **Tenant:** `tenant@propops.local` / `PropOps!Tenant1`
- **Property owner:** `owner@propops.local` / `PropOps!Owner1`
- **Vendor:** `vendor@propops.local` / `PropOps!Vendor1`

The Angular portal signs in through `POST /api/auth/login` and uses the returned bearer token for protected API requests.

Current access boundaries:

- **Property managers / dispatchers:** dashboard access, request queue access, request creation
- **Tenants / property owners:** authenticated portal access and request creation
- **Vendors:** authenticated portal access only, with operational and intake APIs still restricted

## Deferred scope

This version intentionally excludes inference, LLM orchestration, and AI-assisted triage. The backend contracts and modular structure are designed so those capabilities can be introduced later without reworking the whole application foundation.
