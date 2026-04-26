from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field


class MaintenanceTriageRequest(BaseModel):
    request_id: str = Field(..., description="Maintenance request identifier from the .NET API.")
    normalized_text: str = Field(..., description="Normalized issue text prepared by the .NET API.")
    property_name: str = Field(..., description="Resolved property name.")
    unit_number: str | None = Field(default=None, description="Resolved unit number when available.")
    category_hint: str | None = Field(default=None, description="Optional deterministic hint from the API.")
    priority_hint: str | None = Field(default=None, description="Optional deterministic hint from the API.")


app = FastAPI(
    title="PropOps Copilot AI Service",
    version="0.1.0",
    description="Dedicated Python runtime for AI-specific PropOps Copilot capabilities."
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "healthy", "service": "propops-ai-service", "mode": "stub"}


@app.post("/v1/maintenance/triage")
def triage_maintenance(_: MaintenanceTriageRequest) -> None:
    raise HTTPException(
        status_code=501,
        detail="AI triage is not implemented yet. Future AI capabilities must be delivered from this Python service."
    )
