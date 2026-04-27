from .knowledge_base import load_knowledge_base
from .models import (
    MaintenanceTriageContractsResponse,
    MaintenanceTriageInputContract,
    MaintenanceTriageOutputContract,
    MaintenanceTriagePreparationResponse,
    RetrievedKnowledgeItem,
)


EMERGENCY_KEYWORDS = (
    "cannot secure",
    "fire",
    "sparking",
    "gas",
    "flood",
    "burst pipe",
    "no power",
    "unsafe",
)

VENDOR_TYPE_BY_CATEGORY = {
    "Plumbing": "Licensed Plumber",
    "Electrical": "Licensed Electrician",
    "HVAC": "HVAC Technician",
    "Appliances": "Appliance Technician",
    "Security": "Emergency Locksmith / Security Contractor",
    "General": "General Maintenance Contractor",
}


def build_contracts_response() -> MaintenanceTriageContractsResponse:
    knowledge_base = load_knowledge_base()

    return MaintenanceTriageContractsResponse(
        rules_version=knowledge_base["version"],
        input_contract_schema=MaintenanceTriageInputContract.model_json_schema(),
        output_contract_schema=MaintenanceTriageOutputContract.model_json_schema(),
        output_contract_template=build_output_template(
            category="General",
            priority="Normal",
            vendor_type=VENDOR_TYPE_BY_CATEGORY["General"],
            dispatch_decision="Queue for standard triage and assign during business hours.",
            property_name="Example Property",
            unit_number=None,
        ),
    )


def prepare_triage_context(request: MaintenanceTriageInputContract) -> MaintenanceTriagePreparationResponse:
    knowledge_base = load_knowledge_base()
    category = request.category_hint or "General"
    priority = request.priority_hint or "Normal"
    vendor_type = VENDOR_TYPE_BY_CATEGORY[category]
    dispatch_decision = resolve_dispatch_decision(priority, request.is_after_hours)

    knowledge_items = [
        retrieve_sop(knowledge_base, category),
        retrieve_vendor_rule(knowledge_base, category, request.is_after_hours),
        retrieve_property_note(knowledge_base, request.property_name),
    ]

    emergency_policy = retrieve_emergency_policy(knowledge_base, request)
    if emergency_policy is not None:
        knowledge_items.append(emergency_policy)

    return MaintenanceTriagePreparationResponse(
        rules_version=knowledge_base["version"],
        input_contract=request,
        output_contract_template=build_output_template(
            category=category,
            priority=priority,
            vendor_type=vendor_type,
            dispatch_decision=dispatch_decision,
            property_name=request.property_name,
            unit_number=request.unit_number,
        ),
        output_contract_schema=MaintenanceTriageOutputContract.model_json_schema(),
        knowledge_items=[item for item in knowledge_items if item is not None],
    )


def retrieve_sop(knowledge_base: dict, category: str) -> RetrievedKnowledgeItem:
    sop = knowledge_base["maintenance_sops"].get(category, knowledge_base["maintenance_sops"]["General"])
    return RetrievedKnowledgeItem(
        source_type="MaintenanceSop",
        key=category.lower(),
        title=sop["title"],
        content=join_lines(sop["content"]),
        rationale=f"Matched maintenance SOP using category hint '{category}'.",
    )


def retrieve_vendor_rule(knowledge_base: dict, category: str, is_after_hours: bool) -> RetrievedKnowledgeItem:
    vendor_rule = knowledge_base["vendor_rules"].get(category, knowledge_base["vendor_rules"]["General"])[0]
    timing_note = "Request is after-hours, so on-call routing rules apply." if is_after_hours else "Standard dispatch window applies."
    return RetrievedKnowledgeItem(
        source_type="VendorRule",
        key=category.lower(),
        title=vendor_rule["title"],
        content=f"{join_lines(vendor_rule['conditions'])}\n- {timing_note}",
        rationale=f"Selected vendor routing for category '{category}'.",
    )


def retrieve_property_note(knowledge_base: dict, property_name: str) -> RetrievedKnowledgeItem:
    property_notes = knowledge_base["property_notes"].get(
        property_name,
        {
            "title": f"{property_name} notes",
            "content": ["No property-specific notes are stored yet. Use the default routing and capture any site access constraints."]
        },
    )
    return RetrievedKnowledgeItem(
        source_type="PropertyNote",
        key=property_name.lower().replace(" ", "-"),
        title=property_notes["title"],
        content=join_lines(property_notes["content"]),
        rationale=f"Property notes retrieved for '{property_name}'.",
    )


def retrieve_emergency_policy(
    knowledge_base: dict, request: MaintenanceTriageInputContract
) -> RetrievedKnowledgeItem | None:
    normalized_text = request.normalized_text.lower()
    is_emergency = request.priority_hint == "Emergency" or any(
        keyword in normalized_text for keyword in EMERGENCY_KEYWORDS
    )
    if not is_emergency:
        return None

    policy = knowledge_base["emergency_policy"]
    return RetrievedKnowledgeItem(
        source_type="EmergencyPolicy",
        key="emergency-policy",
        title=policy["title"],
        content=join_lines(policy["content"]),
        rationale="Emergency routing policy attached because the request is classified as emergency or contains emergency keywords.",
    )


def build_output_template(
    category: str,
    priority: str,
    vendor_type: str,
    dispatch_decision: str,
    property_name: str,
    unit_number: str | None,
) -> MaintenanceTriageOutputContract:
    location = property_name if not unit_number else f"{property_name} / {unit_number}"
    return MaintenanceTriageOutputContract(
        category=category,
        priority=priority,
        vendor_type=vendor_type,
        dispatch_decision=dispatch_decision,
        internal_summary=f"{priority} {category} issue at {location}. Use retrieved rules and property notes before dispatch.",
        tenant_response_draft=(
            f"Thanks for reporting the issue at {location}. "
            f"We have logged it as {priority.lower()} priority and will follow the current dispatch guidance: {dispatch_decision.lower()}"
        ),
    )


def resolve_dispatch_decision(priority: str, is_after_hours: bool) -> str:
    if priority == "Emergency":
        return "Dispatch immediately to the emergency on-call vendor and notify the resident."
    if priority == "High" and is_after_hours:
        return "Route to the on-call coordinator for urgent after-hours review."
    if priority == "High":
        return "Create an urgent work order and assign the preferred vendor next."
    return "Queue for standard triage and assign during business hours."


def join_lines(lines: list[str]) -> str:
    return "\n".join(f"- {line}" for line in lines)
