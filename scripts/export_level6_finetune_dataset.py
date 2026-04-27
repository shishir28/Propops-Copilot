#!/usr/bin/env python3
"""Export Level 5 candidates and convert them to chat-format JSONL."""

# 1. Logs in to the local PropOps API as manager
# 2. Fetches Level 5 fine-tuning candidates
# 3. Keeps only rows with status = Candidate
# 4. Deduplicates by maintenance request, keeping the latest candidate
# 5. Fetches triage context/knowledge for each request
# 6. Converts each row into chat conversation format
# 7. Splits into train/eval JSONL files
# It uses Level 5 candidates to decide which requests are training-worthy,
# but for the assistant output it prefers the saved staff triage review when available. 
# That makes the dataset better for training the triage model, 
# because it learns the triage decision, not the final repair note.

from __future__ import annotations

import argparse
import json
from pathlib import Path
from urllib import request


SYSTEM_PROMPT = (
    "You are a property maintenance triage assistant. "
    "Return only valid JSON that matches the requested keys exactly."
)


def post_json(url: str, payload: dict, token: str | None = None) -> dict:
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    raw_request = request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers=headers,
        method="POST",
    )
    with request.urlopen(raw_request, timeout=120) as response:
        return json.loads(response.read().decode("utf-8"))


def get_json(url: str, token: str) -> dict | list[dict]:
    raw_request = request.Request(url, headers={"Authorization": f"Bearer {token}"})
    with request.urlopen(raw_request, timeout=120) as response:
        return json.loads(response.read().decode("utf-8"))


def build_prompt(input_snapshot: dict, prepared: dict) -> str:
    knowledge_items = prepared.get("knowledgeItems", [])
    knowledge = "\n\n".join(
        (
            f"{item['sourceType']}: {item['title']}\n"
            f"{item['content']}\n"
            f"Reason: {item['rationale']}"
        )
        for item in knowledge_items
    )
    return (
        "Produce a maintenance triage decision for the following request.\n\n"
        f"Request reference: {input_snapshot['reference_number']}\n"
        f"Normalized text: {input_snapshot['normalized_text']}\n"
        f"Property: {input_snapshot['property_name']}\n"
        f"Unit: {input_snapshot.get('unit_number') or 'N/A'}\n"
        f"Channel: {input_snapshot['channel']}\n"
        f"Category hint: {input_snapshot.get('category_hint') or 'None'}\n"
        f"Priority hint: {input_snapshot.get('priority_hint') or 'None'}\n"
        "After hours: False\n\n"
        "Retrieved knowledge:\n"
        f"{knowledge}\n\n"
        "Return JSON with exactly these keys:\n"
        "- category: one of Plumbing, Electrical, HVAC, Appliances, Security, General\n"
        "- priority: one of Low, Normal, High, Emergency\n"
        "- vendor_type: short contractor/vendor type\n"
        "- dispatch_decision: operational dispatch instruction\n"
        "- internal_summary: short staff-facing summary\n"
        "- tenant_response_draft: tenant-facing response\n"
        "- confidence_score: decimal number from 0.0 to 1.0, for example 0.85, never 85\n"
    )


def derived_confidence(output_snapshot: dict, input_snapshot: dict) -> float:
    confidence = 0.82
    if output_snapshot["category"] == input_snapshot.get("category_hint"):
        confidence += 0.05
    if output_snapshot["priority"] == input_snapshot.get("priority_hint"):
        confidence += 0.05
    if output_snapshot["priority"] in {"High", "Emergency"}:
        confidence += 0.03
    return round(min(confidence, 0.95), 2)


def to_chat_example(candidate: dict, prepared: dict, operations_detail: dict) -> dict:
    input_snapshot = json.loads(candidate["inputSnapshotJson"])
    output_snapshot = json.loads(candidate["outputSnapshotJson"])
    metadata_snapshot = json.loads(candidate["metadataSnapshotJson"])
    latest_review = operations_detail.get("latestReview")
    if latest_review:
        output_snapshot = {
            "category": latest_review["finalCategory"],
            "priority": latest_review["finalPriority"],
            "vendor_type": latest_review["finalVendorType"],
            "dispatch_decision": latest_review["finalDispatchDecision"],
            "internal_summary": latest_review["finalInternalSummary"],
            "tenant_response_draft": latest_review["finalTenantResponseDraft"],
        }
    assistant_payload = {
        "category": output_snapshot["category"],
        "priority": output_snapshot["priority"],
        "vendor_type": output_snapshot["vendor_type"],
        "dispatch_decision": output_snapshot["dispatch_decision"],
        "internal_summary": output_snapshot["internal_summary"],
        "tenant_response_draft": output_snapshot["tenant_response_draft"],
        "confidence_score": derived_confidence(output_snapshot, input_snapshot),
    }
    return {
        "messages": [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": build_prompt(input_snapshot, prepared)},
            {
                "role": "assistant",
                "content": json.dumps(assistant_payload, ensure_ascii=False, separators=(",", ":")),
            },
        ],
        "metadata": {
            "candidate_id": candidate["id"],
            "maintenance_request_id": candidate["maintenanceRequestId"],
            "reference_number": input_snapshot["reference_number"],
            "category": output_snapshot["category"],
            "priority": output_snapshot["priority"],
            "source": "level4-review" if latest_review else metadata_snapshot.get("generated_from", "level5-feedback"),
            "created_at_utc": candidate["createdAtUtc"],
        },
    }


def latest_candidate_per_request(candidates: list[dict]) -> list[dict]:
    latest: dict[str, dict] = {}
    for candidate in candidates:
        request_id = candidate["maintenanceRequestId"]
        if request_id not in latest or candidate["createdAtUtc"] > latest[request_id]["createdAtUtc"]:
            latest[request_id] = candidate
    return sorted(latest.values(), key=lambda candidate: candidate["createdAtUtc"])


def write_jsonl(path: Path, rows: list[dict]) -> None:
    with path.open("w", encoding="utf-8") as file:
        for row in rows:
            file.write(json.dumps(row, ensure_ascii=False, separators=(",", ":")) + "\n")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url", default="http://localhost:8095/api")
    parser.add_argument("--email", default="manager@propops.local")
    parser.add_argument("--password", default="PropOps!Manager1")
    parser.add_argument("--output-dir", default="data/level6-finetuning")
    parser.add_argument("--eval-count", type=int, default=5)
    args = parser.parse_args()

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    login = post_json(
        f"{args.base_url}/auth/login",
        {"email": args.email, "password": args.password},
    )
    token = login["accessToken"]

    all_candidates = get_json(f"{args.base_url}/learning/dataset/candidates", token)
    if not isinstance(all_candidates, list):
        raise RuntimeError("Expected candidate list from API.")

    usable_candidates = [candidate for candidate in all_candidates if candidate["status"] == "Candidate"]
    deduped_candidates = latest_candidate_per_request(usable_candidates)

    raw_export = {
        "allCandidateRows": len(all_candidates),
        "usableCandidateRows": len(usable_candidates),
        "dedupedCandidateRows": len(deduped_candidates),
        "candidates": deduped_candidates,
    }
    (output_dir / "level5-candidates.raw.json").write_text(
        json.dumps(raw_export, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )

    chat_examples: list[dict] = []
    for candidate in deduped_candidates:
        prepared = post_json(
            f"{args.base_url}/ai/maintenance-triage/prepare",
            {"maintenanceRequestId": candidate["maintenanceRequestId"]},
            token,
        )
        operations_detail = get_json(
            f"{args.base_url}/maintenanceRequests/{candidate['maintenanceRequestId']}/operations",
            token,
        )
        if not isinstance(operations_detail, dict):
            raise RuntimeError("Expected operations detail from API.")
        chat_examples.append(to_chat_example(candidate, prepared, operations_detail))

    eval_count = min(args.eval_count, max(1, len(chat_examples) // 4))
    train_rows = chat_examples[:-eval_count]
    eval_rows = chat_examples[-eval_count:]

    write_jsonl(output_dir / "train.chat.jsonl", train_rows)
    write_jsonl(output_dir / "eval.chat.jsonl", eval_rows)
    write_jsonl(output_dir / "all.chat.jsonl", chat_examples)

    manifest = {
        "baseUrl": args.base_url,
        "allCandidateRows": len(all_candidates),
        "usableCandidateRows": len(usable_candidates),
        "dedupedCandidateRows": len(deduped_candidates),
        "trainRows": len(train_rows),
        "evalRows": len(eval_rows),
        "format": "chat messages JSONL",
        "notes": [
            "Only candidates with status Candidate are used.",
            "When multiple candidates exist for one maintenance request, only the latest row is used.",
            "Assistant outputs prefer the saved staff triage review when present, because triage fine-tuning should not learn post-repair resolution wording.",
            "confidence_score is derived during conversion because Level 5 output snapshots do not persist model confidence.",
        ],
    }
    (output_dir / "manifest.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
