from propops_ai_service.retrieval import build_contracts_response, prepare_triage_context


def test_build_contracts_response_exposes_current_contract_schemas():
    response = build_contracts_response()

    assert response.rules_version == "2026.04-level2"
    assert response.input_contract_schema["properties"]["request_id"]["type"] == "string"
    assert response.output_contract_schema["properties"]["vendor_type"]["type"] == "string"
    assert "Queue for standard triage" in response.output_contract_template.dispatch_decision


def test_prepare_triage_context_includes_property_and_emergency_knowledge(make_request):
    request = make_request(
        normalized_text="Front door lock is jammed and the property cannot secure properly tonight.",
        property_name="Elm Street Townhomes",
        unit_number="3",
        category_hint="Security",
        priority_hint="Emergency",
    )

    response = prepare_triage_context(request)

    source_types = [item.source_type for item in response.knowledge_items]

    assert response.output_contract_template.vendor_type == "Emergency Locksmith / Security Contractor"
    assert "Dispatch immediately" in response.output_contract_template.dispatch_decision
    assert source_types == ["MaintenanceSop", "VendorRule", "PropertyNote", "EmergencyPolicy"]
