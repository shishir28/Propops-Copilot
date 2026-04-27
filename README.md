# PropOps Copilot

PropOps Copilot is a UI-first property maintenance operations platform. The current implementation covers structured intake, queue visibility, deterministic normalization, Level 2 rules-and-knowledge preparation, and Level 3 baseline AI inference with guardrails using Angular, ASP.NET Core, PostgreSQL, and a separate Python AI service.

## Stack

- **Frontend:** Angular 21 with standalone components and modern SCSS styling
- **Backend:** ASP.NET Core Web API with Domain, Application, Infrastructure, and Api projects
- **AI runtime:** Python FastAPI service using LangGraph for inference orchestration, retrieval, and guardrails
- **Persistence:** PostgreSQL via Entity Framework Core
- **Local orchestration:** Docker Compose

## Project structure

```text
.
├── docker-compose.yml
├── docs/
│   ├── architecture.md
│   └── propops-copilot-flow.txt
├── src/
│   ├── ai/
│   │   └── propops-ai-service/
│   ├── backend/
│   │   ├── PropOpsCopilot.Api/
│   │   ├── PropOpsCopilot.Application/
│   │   ├── PropOpsCopilot.Domain/
│   │   └── PropOpsCopilot.Infrastructure/
│   └── frontend/
└── README.md
```

## Running with Docker Compose

```bash
docker compose up --build
```

Once the stack is up:

- **Frontend:** http://localhost:4315
- **API:** http://localhost:8095
- **Swagger:** http://localhost:8095/swagger/index.html
- **Python AI service:** internal-only at `http://ai-service:8000` inside Docker Compose

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

### Python AI service

```bash
cd src/ai/propops-ai-service
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn propops_ai_service.main:app --reload --host 0.0.0.0 --port 8000
```

The .NET API is configured to call the Python service through `AiService:BaseUrl`, which defaults to `http://localhost:8000` outside Docker and `http://ai-service:8000` inside Docker Compose.

The Python AI service uses LangGraph to orchestrate the inference path and defaults to a local-safe heuristic mode for development and tests:

- `PROP_OPS_AI_INFERENCE_MODE=heuristic`
- `PROP_OPS_AI_MODEL_NAME=Qwen/Qwen2.5-7B-Instruct`
- `PROP_OPS_AI_CONFIDENCE_THRESHOLD=0.68`

Switch to an OpenAI-compatible instruct model endpoint by setting:

- `PROP_OPS_AI_INFERENCE_MODE=openai-compatible`
- `PROP_OPS_AI_OPENAI_BASE_URL=<your-openai-compatible-base-url>`
- `PROP_OPS_AI_OPENAI_API_KEY=<optional-api-key>`

Levels 2 and 3 are now implemented across that boundary:

- the Python service owns the structured maintenance knowledge base and triage contract schemas
- the Python service can run baseline triage inference and guardrails through a LangGraph workflow behind a provider adapter
- the .NET API exposes staff-facing endpoints that prepare rules, retrieve contracts, and infer a triage decision for an existing maintenance request

## Playwright tests

The frontend workspace now includes Playwright coverage for:

- manager request creation through the Angular portal
- Level 1 intake and normalization through the API connector endpoints
- Level 2 rules and knowledge retrieval through the .NET API
- Level 3 baseline inference and guardrail fallback behavior through the .NET API

Start the full stack first:

```bash
docker compose up --build
```

Then run the tests from the frontend workspace:

```bash
cd src/frontend
npm run test:e2e
```

Useful variants:

```bash
npm run test:e2e:headed
npm run test:e2e:ui
```

If you need to reinstall the Playwright browser:

```bash
npm run test:e2e:install
```

Optional environment overrides:

- `PLAYWRIGHT_FRONTEND_URL` defaults to `http://127.0.0.1:4315`
- `PLAYWRIGHT_API_URL` defaults to `http://127.0.0.1:8095`

### Running Playwright through Docker Compose

Playwright is also available as an optional Compose service.

Build and run it manually:

```bash
docker compose --profile test run --rm playwright
```

The Compose runner is for headless execution:

```bash
docker compose --profile test run --rm playwright
```

Use headed mode only on the host machine:

```bash
cd src/frontend
npm run test:e2e:headed
```

Inside Compose, the Playwright service targets:

- frontend: `http://host.docker.internal:4315`
- api: `http://host.docker.internal:8095`

## Current capabilities

- Secure internal portal login with ASP.NET Core Identity and JWT-based API auth
- Review maintenance intake KPIs on the overview screen
- Inspect recent maintenance requests
- Create new maintenance requests through the Angular UI
- Persist requests to PostgreSQL
- Seed a starter queue for local development
- Keep the user-facing runtime clean so current and future AI work happens in the dedicated Python service instead of the portal API
- Retrieve maintenance SOPs, vendor rules, emergency policy, and property notes through the Python AI service
- Return explicit Level 2 AI input/output contracts for maintenance triage preparation
- Run Level 3 baseline triage inference through the Python AI service and return guardrail metadata, confidence, and human-review fallback signals

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

This version intentionally excludes human-review tooling, action integrations, fine-tuning, and vLLM-backed serving. The runtime boundary is already active, though: Angular talks only to the .NET API, and the .NET API is the only application allowed to call the Python AI service for current and future AI features.
