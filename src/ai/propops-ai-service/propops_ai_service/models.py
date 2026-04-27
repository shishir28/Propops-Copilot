from datetime import datetime
from typing import Any, Literal

from pydantic import BaseModel, Field


CategoryValue = Literal["Plumbing", "Electrical", "HVAC", "Appliances", "Security", "General"]
PriorityValue = Literal["Low", "Normal", "High", "Emergency"]
ChannelValue = Literal["Portal", "Email", "SmsChat", "PhoneNote"]


class MaintenanceTriageInputContract(BaseModel):
    request_id: str = Field(..., description="Maintenance request identifier from the .NET API.")
    reference_number: str = Field(..., description="Human-readable maintenance request reference.")
    normalized_text: str = Field(..., description="Normalized issue text prepared by the .NET API.")
    property_name: str = Field(..., description="Resolved property name.")
    unit_number: str | None = Field(default=None, description="Resolved unit number when available.")
    channel: ChannelValue = Field(..., description="Origin channel for the intake or maintenance request.")
    category_hint: CategoryValue | None = Field(default=None, description="Deterministic category hint from the API.")
    priority_hint: PriorityValue | None = Field(default=None, description="Deterministic priority hint from the API.")
    is_after_hours: bool = Field(..., description="Whether the request landed outside configured business hours.")
    submitted_at_utc: datetime | None = Field(default=None, description="Original request submission timestamp.")


class RetrievedKnowledgeItem(BaseModel):
    source_type: Literal["MaintenanceSop", "VendorRule", "EmergencyPolicy", "PropertyNote"]
    key: str
    title: str
    content: str
    rationale: str


class MaintenanceTriageOutputContract(BaseModel):
    category: CategoryValue
    priority: PriorityValue
    vendor_type: str
    dispatch_decision: str
    internal_summary: str
    tenant_response_draft: str


class MaintenanceTriageContractsResponse(BaseModel):
    rules_version: str
    input_contract_schema: dict[str, Any]
    output_contract_schema: dict[str, Any]
    output_contract_template: MaintenanceTriageOutputContract


class MaintenanceTriagePreparationResponse(BaseModel):
    rules_version: str
    input_contract: MaintenanceTriageInputContract
    output_contract_template: MaintenanceTriageOutputContract
    output_contract_schema: dict[str, Any]
    knowledge_items: list[RetrievedKnowledgeItem]
