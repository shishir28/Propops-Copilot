import pytest

from propops_ai_service.inference import extract_json_object, infer_triage_decision, load_inference_settings


def test_infer_triage_decision_returns_accepted_heuristic_result(monkeypatch, make_request):
    monkeypatch.setenv("PROP_OPS_AI_INFERENCE_MODE", "heuristic")
    monkeypatch.setenv("PROP_OPS_AI_CONFIDENCE_THRESHOLD", "0.68")
    request = make_request()

    response = infer_triage_decision(request)

    assert response.rules_version == "2026.04-level2"
    assert response.output_contract.category == "Plumbing"
    assert response.output_contract.priority == "High"
    assert response.guardrails.schema_valid is True
    assert response.guardrails.requires_human_review is False
    assert response.inference_metadata.provider_mode == "heuristic"


def test_infer_triage_decision_falls_back_to_human_review_for_low_signal_requests(monkeypatch, make_request):
    monkeypatch.setenv("PROP_OPS_AI_INFERENCE_MODE", "heuristic")
    monkeypatch.setenv("PROP_OPS_AI_CONFIDENCE_THRESHOLD", "0.68")
    request = make_request(
        normalized_text="Something is broken and there is a weird issue somewhere.",
        category_hint="General",
        priority_hint="Normal",
    )

    response = infer_triage_decision(request)
    issue_codes = [issue.code for issue in response.guardrails.issues]

    assert response.guardrails.requires_human_review is True
    assert response.guardrails.fallback_applied is True
    assert "LOW_CONFIDENCE" in issue_codes
    assert "staff triage review" in response.output_contract.dispatch_decision


def test_extract_json_object_reads_wrapped_json_payloads():
    payload = """
    Here is the model output:
    {"category":"Plumbing","priority":"High"}
    """

    parsed = extract_json_object(payload)

    assert parsed == {"category": "Plumbing", "priority": "High"}


def test_load_inference_settings_rejects_invalid_threshold(monkeypatch):
    monkeypatch.setenv("PROP_OPS_AI_CONFIDENCE_THRESHOLD", "1.5")

    with pytest.raises(RuntimeError, match="PROP_OPS_AI_CONFIDENCE_THRESHOLD must be a decimal between 0 and 1."):
        load_inference_settings()
