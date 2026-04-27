from dataclasses import dataclass
from functools import lru_cache
import json
import os
from typing import TypedDict
from urllib import error, request as urllib_request

from langgraph.graph import END, START, StateGraph
from pydantic import ValidationError

from .models import (
    MaintenanceTriageGuardrailIssue,
    MaintenanceTriageGuardrailResult,
    MaintenanceTriageInferenceMetadata,
    MaintenanceTriageInferenceResponse,
    MaintenanceTriageInputContract,
    MaintenanceTriageModelCandidate,
    MaintenanceTriageOutputContract,
    MaintenanceTriagePreparationResponse,
)
from .retrieval import (
    EMERGENCY_KEYWORDS,
    VENDOR_TYPE_BY_CATEGORY,
    build_output_template,
    prepare_triage_context,
    resolve_dispatch_decision,
)

DEFAULT_MODEL_NAME = "Qwen/Qwen2.5-7B-Instruct"
SUPPORTED_MODES = {"heuristic", "openai-compatible"}
CATEGORY_KEYWORDS = {
    "Plumbing": ("leak", "pipe", "tap", "sink", "toilet", "water"),
    "Electrical": ("power", "sparking", "outlet", "breaker", "light", "electrical"),
    "HVAC": ("aircon", "ac", "heating", "cooling", "hvac", "thermostat"),
    "Appliances": ("dishwasher", "washer", "dryer", "fridge", "oven", "appliance"),
    "Security": ("lock", "door", "secure", "security", "key", "entry"),
}
HIGH_PRIORITY_KEYWORDS = ("urgent", "asap", "quickly", "tonight", "today")
LOW_SIGNAL_PHRASES = (
    "something is broken",
    "not sure",
    "weird",
    "strange",
    "issue",
    "problem",
)


@dataclass(frozen=True)
class InferenceSettings:
    mode: str
    model_name: str
    confidence_threshold: float
    openai_base_url: str | None
    openai_api_key: str | None


class InferenceState(TypedDict, total=False):
    request: MaintenanceTriageInputContract
    settings: InferenceSettings
    prepared: MaintenanceTriagePreparationResponse
    metadata: MaintenanceTriageInferenceMetadata
    candidate: MaintenanceTriageModelCandidate
    output_contract: MaintenanceTriageOutputContract
    issues: list[MaintenanceTriageGuardrailIssue]
    policy_passed: bool
    emergency_keyword_check_passed: bool
    schema_error: str
    guardrails: MaintenanceTriageGuardrailResult
    response: MaintenanceTriageInferenceResponse


def infer_triage_decision(request: MaintenanceTriageInputContract) -> MaintenanceTriageInferenceResponse:
    settings = load_inference_settings()
    graph = build_inference_graph()
    final_state = graph.invoke({"request": request, "settings": settings})
    return final_state["response"]


def prepare_context_node(state: InferenceState) -> InferenceState:
    request = state["request"]
    settings = state["settings"]
    prepared = prepare_triage_context(request)
    metadata = MaintenanceTriageInferenceMetadata(provider_mode=settings.mode, model_name=settings.model_name)
    return {
        "prepared": prepared,
        "metadata": metadata,
    }


def generate_candidate_node(state: InferenceState) -> InferenceState:
    request = state["request"]
    prepared = state["prepared"]
    settings = state["settings"]

    try:
        candidate = generate_model_candidate(request, prepared, settings)
    except (ValidationError, json.JSONDecodeError, ValueError) as exception:
        return {"schema_error": str(exception)}

    return {"candidate": candidate}


def evaluate_guardrails_node(state: InferenceState) -> InferenceState:
    request = state["request"]
    candidate = state["candidate"]

    issues: list[MaintenanceTriageGuardrailIssue] = []
    policy_passed = True
    emergency_keyword_check_passed = True

    output_contract = MaintenanceTriageOutputContract.model_validate(candidate.model_dump(exclude={"confidence_score"}))
    expected_emergency = is_emergency_request(request)
    dispatch_text = output_contract.dispatch_decision.lower()
    vendor_text = output_contract.vendor_type.lower()

    if output_contract.category == "Security" and not any(term in vendor_text for term in ("security", "locksmith")):
        policy_passed = False
        issues.append(
            MaintenanceTriageGuardrailIssue(
                code="SECURITY_VENDOR_POLICY",
                severity="Blocking",
                message="Security incidents must route to a locksmith or security contractor.",
            )
        )

    if output_contract.priority == "Emergency" and not any(
        term in dispatch_text for term in ("dispatch immediately", "on-call", "immediate")
    ):
        policy_passed = False
        issues.append(
            MaintenanceTriageGuardrailIssue(
                code="EMERGENCY_DISPATCH_POLICY",
                severity="Blocking",
                message="Emergency incidents must include immediate dispatch or on-call escalation guidance.",
            )
        )

    if request.is_after_hours and output_contract.priority in {"High", "Emergency"} and "business hours" in dispatch_text:
        policy_passed = False
        issues.append(
            MaintenanceTriageGuardrailIssue(
                code="AFTER_HOURS_POLICY",
                severity="Blocking",
                message="High-priority after-hours incidents cannot be queued for normal business-hours review.",
            )
        )

    if expected_emergency and output_contract.priority != "Emergency":
        emergency_keyword_check_passed = False
        issues.append(
            MaintenanceTriageGuardrailIssue(
                code="EMERGENCY_KEYWORD_CHECK",
                severity="Blocking",
                message="Emergency keywords or hints were detected, but the inferred priority was below Emergency.",
            )
        )

    if candidate.confidence_score < state["settings"].confidence_threshold:
        issues.append(
            MaintenanceTriageGuardrailIssue(
                code="LOW_CONFIDENCE",
                severity="Blocking",
                message=(
                    f"Inference confidence {candidate.confidence_score:.2f} is below the configured "
                    f"threshold of {state['settings'].confidence_threshold:.2f}."
                ),
            )
        )

    return {
        "output_contract": output_contract,
        "issues": issues,
        "policy_passed": policy_passed,
        "emergency_keyword_check_passed": emergency_keyword_check_passed,
    }


def accept_output_node(state: InferenceState) -> InferenceState:
    candidate = state["candidate"]
    guardrails = MaintenanceTriageGuardrailResult(
        schema_valid=True,
        policy_passed=state["policy_passed"],
        emergency_keyword_check_passed=state["emergency_keyword_check_passed"],
        confidence_score=candidate.confidence_score,
        confidence_threshold=state["settings"].confidence_threshold,
        requires_human_review=False,
        fallback_applied=False,
        issues=[],
    )
    return {"guardrails": guardrails}


def apply_fallback_node(state: InferenceState) -> InferenceState:
    request = state["request"]
    prepared = state["prepared"]
    candidate = state["candidate"]
    issues = state["issues"]
    final_output = build_human_review_fallback(
        state["output_contract"],
        request,
        [issue.message for issue in issues],
    )
    guardrails = MaintenanceTriageGuardrailResult(
        schema_valid=True,
        policy_passed=state["policy_passed"],
        emergency_keyword_check_passed=state["emergency_keyword_check_passed"],
        confidence_score=candidate.confidence_score,
        confidence_threshold=state["settings"].confidence_threshold,
        requires_human_review=True,
        fallback_applied=True,
        issues=issues,
    )
    return {
        "output_contract": final_output,
        "guardrails": guardrails,
    }


def build_schema_failure_response_node(state: InferenceState) -> InferenceState:
    request = state["request"]
    prepared = state["prepared"]
    settings = state["settings"]

    fallback_output = build_human_review_fallback(
        prepared.output_contract_template,
        request,
        ["The model response could not be validated against the required output schema."],
    )
    guardrails = MaintenanceTriageGuardrailResult(
        schema_valid=False,
        policy_passed=False,
        emergency_keyword_check_passed=not is_emergency_request(request),
        confidence_score=0.0,
        confidence_threshold=settings.confidence_threshold,
        requires_human_review=True,
        fallback_applied=True,
        issues=[
            MaintenanceTriageGuardrailIssue(
                code="SCHEMA_VALIDATION_FAILED",
                severity="Blocking",
                message=(
                    "Model output was rejected because it did not match the required schema: "
                    f"{state['schema_error']}"
                ),
            )
        ],
    )

    response = MaintenanceTriageInferenceResponse(
        rules_version=prepared.rules_version,
        input_contract=prepared.input_contract,
        output_contract=fallback_output,
        output_contract_schema=prepared.output_contract_schema,
        knowledge_items=prepared.knowledge_items,
        guardrails=guardrails,
        inference_metadata=state["metadata"],
    )
    return {"response": response}


def finalize_response_node(state: InferenceState) -> InferenceState:
    prepared = state["prepared"]
    response = MaintenanceTriageInferenceResponse(
        rules_version=prepared.rules_version,
        input_contract=prepared.input_contract,
        output_contract=state["output_contract"],
        output_contract_schema=prepared.output_contract_schema,
        knowledge_items=prepared.knowledge_items,
        guardrails=state["guardrails"],
        inference_metadata=state["metadata"],
    )
    return {"response": response}


def route_after_candidate(state: InferenceState) -> str:
    return "schema_failure" if state.get("schema_error") else "evaluate_guardrails"


def route_after_guardrails(state: InferenceState) -> str:
    return "apply_fallback" if state["issues"] else "accept_output"


@lru_cache
def build_inference_graph():
    graph = StateGraph(InferenceState)
    graph.add_node("prepare_context", prepare_context_node)
    graph.add_node("generate_candidate", generate_candidate_node)
    graph.add_node("evaluate_guardrails", evaluate_guardrails_node)
    graph.add_node("accept_output", accept_output_node)
    graph.add_node("apply_fallback", apply_fallback_node)
    graph.add_node("schema_failure", build_schema_failure_response_node)
    graph.add_node("finalize_response", finalize_response_node)

    graph.add_edge(START, "prepare_context")
    graph.add_edge("prepare_context", "generate_candidate")
    graph.add_conditional_edges(
        "generate_candidate",
        route_after_candidate,
        {
            "schema_failure": "schema_failure",
            "evaluate_guardrails": "evaluate_guardrails",
        },
    )
    graph.add_edge("schema_failure", END)
    graph.add_conditional_edges(
        "evaluate_guardrails",
        route_after_guardrails,
        {
            "accept_output": "accept_output",
            "apply_fallback": "apply_fallback",
        },
    )
    graph.add_edge("accept_output", "finalize_response")
    graph.add_edge("apply_fallback", "finalize_response")
    graph.add_edge("finalize_response", END)
    return graph.compile()


def generate_model_candidate(
    request: MaintenanceTriageInputContract,
    prepared: MaintenanceTriagePreparationResponse,
    settings: InferenceSettings,
) -> MaintenanceTriageModelCandidate:
    if settings.mode == "heuristic":
        return heuristic_infer(request, prepared)
    if settings.mode == "openai-compatible":
        return openai_compatible_infer(request, prepared, settings)

    raise RuntimeError(f"Unsupported inference mode '{settings.mode}'.")


def heuristic_infer(
    request: MaintenanceTriageInputContract,
    prepared: MaintenanceTriagePreparationResponse,
) -> MaintenanceTriageModelCandidate:
    normalized_text = request.normalized_text.lower()
    category = infer_category(normalized_text, request.category_hint)
    priority = infer_priority(normalized_text, request.priority_hint)
    vendor_type = VENDOR_TYPE_BY_CATEGORY[category]
    dispatch_decision = resolve_dispatch_decision(priority, request.is_after_hours)
    location = request.property_name if not request.unit_number else f"{request.property_name} / {request.unit_number}"
    knowledge_titles = ", ".join(item.title for item in prepared.knowledge_items[:2])

    return MaintenanceTriageModelCandidate(
        category=category,
        priority=priority,
        vendor_type=vendor_type,
        dispatch_decision=dispatch_decision,
        internal_summary=(
            f"{priority} {category} issue at {location}. "
            f"Baseline inference used the retrieved guidance from: {knowledge_titles}."
        ),
        tenant_response_draft=(
            f"Thanks for reporting the issue at {location}. "
            f"We have logged it as {priority.lower()} priority and our team will follow the current dispatch guidance."
        ),
        confidence_score=calculate_confidence(normalized_text, category, priority, request),
    )


def openai_compatible_infer(
    request: MaintenanceTriageInputContract,
    prepared: MaintenanceTriagePreparationResponse,
    settings: InferenceSettings,
) -> MaintenanceTriageModelCandidate:
    if not settings.openai_base_url:
        raise RuntimeError("PROP_OPS_AI_OPENAI_BASE_URL is required when using openai-compatible inference mode.")

    endpoint = settings.openai_base_url.rstrip("/") + "/chat/completions"
    messages = [
        {
            "role": "system",
            "content": (
                "You are a property maintenance triage assistant. "
                "Return only valid JSON that matches the requested keys exactly."
            ),
        },
        {
            "role": "user",
            "content": build_inference_prompt(request, prepared),
        },
    ]
    payload = {
        "model": settings.model_name,
        "temperature": 0.1,
        "response_format": {"type": "json_object"},
        "messages": messages,
    }
    headers = {"Content-Type": "application/json"}
    if settings.openai_api_key:
        headers["Authorization"] = f"Bearer {settings.openai_api_key}"

    raw_request = urllib_request.Request(
        endpoint,
        data=json.dumps(payload).encode("utf-8"),
        headers=headers,
        method="POST",
    )

    try:
        with urllib_request.urlopen(raw_request, timeout=60) as response:
            body = json.loads(response.read().decode("utf-8"))
    except error.URLError as exception:
        raise RuntimeError(f"Unable to reach the configured OpenAI-compatible endpoint: {exception}") from exception

    content = body["choices"][0]["message"]["content"]
    parsed = extract_json_object(content)
    return MaintenanceTriageModelCandidate.model_validate(parsed)


def build_human_review_fallback(
    template: MaintenanceTriageOutputContract,
    request: MaintenanceTriageInputContract,
    reasons: list[str],
) -> MaintenanceTriageOutputContract:
    summary_reason = "; ".join(reasons)
    location = request.property_name if not request.unit_number else f"{request.property_name} / {request.unit_number}"
    dispatch_decision = (
        "Escalate to staff immediately for emergency review and confirm the on-call dispatch path."
        if is_emergency_request(request)
        else "Route to staff triage review before dispatch."
    )
    return build_output_template(
        category=template.category,
        priority="Emergency" if is_emergency_request(request) else template.priority,
        vendor_type=template.vendor_type,
        dispatch_decision=dispatch_decision,
        property_name=request.property_name,
        unit_number=request.unit_number,
    ).model_copy(
        update={
            "internal_summary": (
                f"{template.internal_summary} Human review required before automated routing. Guardrail reasons: {summary_reason}"
            ),
            "tenant_response_draft": (
                f"Thanks for reporting the issue at {location}. "
                "Our operations team is reviewing the request now and will confirm the next action shortly."
            ),
        }
    )


def build_inference_prompt(
    request: MaintenanceTriageInputContract,
    prepared: MaintenanceTriagePreparationResponse,
) -> str:
    knowledge = "\n\n".join(
        f"{item.source_type}: {item.title}\n{item.content}\nReason: {item.rationale}" for item in prepared.knowledge_items
    )
    return (
        "Produce a maintenance triage decision for the following request.\n\n"
        f"Request reference: {request.reference_number}\n"
        f"Normalized text: {request.normalized_text}\n"
        f"Property: {request.property_name}\n"
        f"Unit: {request.unit_number or 'N/A'}\n"
        f"Channel: {request.channel}\n"
        f"Category hint: {request.category_hint or 'None'}\n"
        f"Priority hint: {request.priority_hint or 'None'}\n"
        f"After hours: {request.is_after_hours}\n\n"
        "Retrieved knowledge:\n"
        f"{knowledge}\n\n"
        "Return JSON with exactly these keys:\n"
        "- category\n"
        "- priority\n"
        "- vendor_type\n"
        "- dispatch_decision\n"
        "- internal_summary\n"
        "- tenant_response_draft\n"
        "- confidence_score\n"
    )


def infer_category(normalized_text: str, category_hint: str | None) -> str:
    if category_hint and category_hint != "General":
        return category_hint

    for category, keywords in CATEGORY_KEYWORDS.items():
        if any(keyword in normalized_text for keyword in keywords):
            return category

    return category_hint or "General"


def infer_priority(normalized_text: str, priority_hint: str | None) -> str:
    if priority_hint == "Emergency" or any(keyword in normalized_text for keyword in EMERGENCY_KEYWORDS):
        return "Emergency"
    if priority_hint == "High" or any(keyword in normalized_text for keyword in HIGH_PRIORITY_KEYWORDS):
        return "High"
    return priority_hint or "Normal"


def calculate_confidence(
    normalized_text: str,
    category: str,
    priority: str,
    request: MaintenanceTriageInputContract,
) -> float:
    confidence = 0.42

    if request.category_hint and request.category_hint != "General":
        confidence += 0.18
    if request.priority_hint and request.priority_hint in {"High", "Emergency"}:
        confidence += 0.12
    if category != "General":
        confidence += 0.1
    if priority in {"High", "Emergency"}:
        confidence += 0.08
    if any(keyword in normalized_text for keyword in EMERGENCY_KEYWORDS):
        confidence += 0.1
    if any(phrase in normalized_text for phrase in LOW_SIGNAL_PHRASES):
        confidence -= 0.18
    if len(normalized_text.split()) < 8:
        confidence -= 0.08

    return round(min(0.98, max(0.18, confidence)), 2)


def is_emergency_request(request: MaintenanceTriageInputContract) -> bool:
    normalized_text = request.normalized_text.lower()
    return request.priority_hint == "Emergency" or any(keyword in normalized_text for keyword in EMERGENCY_KEYWORDS)


def extract_json_object(content: str) -> dict:
    stripped = content.strip()
    if stripped.startswith("{"):
        return json.loads(stripped)

    start = stripped.find("{")
    end = stripped.rfind("}")
    if start == -1 or end == -1 or end <= start:
        raise ValueError("No JSON object found in model response.")

    return json.loads(stripped[start : end + 1])


@lru_cache
def load_inference_settings() -> InferenceSettings:
    mode = os.getenv("PROP_OPS_AI_INFERENCE_MODE", "heuristic").strip().lower().replace("_", "-")
    if mode not in SUPPORTED_MODES:
        raise RuntimeError(
            f"Unsupported PROP_OPS_AI_INFERENCE_MODE '{mode}'. Supported values: {', '.join(sorted(SUPPORTED_MODES))}."
        )

    threshold_raw = os.getenv("PROP_OPS_AI_CONFIDENCE_THRESHOLD", "0.68").strip()
    try:
        confidence_threshold = float(threshold_raw)
    except ValueError as exception:
        raise RuntimeError("PROP_OPS_AI_CONFIDENCE_THRESHOLD must be a decimal between 0 and 1.") from exception

    if confidence_threshold < 0 or confidence_threshold > 1:
        raise RuntimeError("PROP_OPS_AI_CONFIDENCE_THRESHOLD must be a decimal between 0 and 1.")

    return InferenceSettings(
        mode=mode,
        model_name=os.getenv("PROP_OPS_AI_MODEL_NAME", DEFAULT_MODEL_NAME).strip() or DEFAULT_MODEL_NAME,
        confidence_threshold=confidence_threshold,
        openai_base_url=os.getenv("PROP_OPS_AI_OPENAI_BASE_URL"),
        openai_api_key=os.getenv("PROP_OPS_AI_OPENAI_API_KEY"),
    )
