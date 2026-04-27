from fastapi import FastAPI

from propops_ai_service.models import MaintenanceTriageInputContract
from propops_ai_service.retrieval import build_contracts_response, prepare_triage_context


app = FastAPI(
    title="PropOps Copilot AI Service",
    version="0.1.0",
    description="Dedicated Python runtime for AI-specific PropOps Copilot capabilities."
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "healthy", "service": "propops-ai-service", "mode": "stub"}


@app.get("/v1/maintenance/contracts")
def maintenance_contracts():
    return build_contracts_response()


@app.post("/v1/maintenance/triage/prepare")
def prepare_maintenance_triage(request: MaintenanceTriageInputContract):
    return prepare_triage_context(request)
