#!/usr/bin/env python3
"""Run chat-format eval rows against an OpenAI-compatible endpoint."""

# It reads:data/level6-finetuning/eval.chat.jsonl, calls the current vLLM endpoint: http://localhost:8001/v1
# Qwen/Qwen2.5-3B-Instruct and saves data/level6-finetuning/baseline-eval-results.json
 

from __future__ import annotations

import argparse
import json
from pathlib import Path
from urllib import request


VALID_CATEGORIES = {"Plumbing", "Electrical", "HVAC", "Appliances", "Security", "General"}
VALID_PRIORITIES = {"Low", "Normal", "High", "Emergency"}
REQUIRED_KEYS = {
    "category",
    "priority",
    "vendor_type",
    "dispatch_decision",
    "internal_summary",
    "tenant_response_draft",
    "confidence_score",
}


def extract_json_object(content: str) -> dict:
    stripped = content.strip()
    if stripped.startswith("{"):
        return json.loads(stripped)

    start = stripped.find("{")
    end = stripped.rfind("}")
    if start == -1 or end == -1 or end <= start:
        raise ValueError("No JSON object found in model response.")
    return json.loads(stripped[start : end + 1])


def validate_payload(payload: dict) -> list[str]:
    issues: list[str] = []
    keys = set(payload)
    missing = sorted(REQUIRED_KEYS - keys)
    extra = sorted(keys - REQUIRED_KEYS)
    if missing:
        issues.append(f"missing keys: {', '.join(missing)}")
    if extra:
        issues.append(f"extra keys: {', '.join(extra)}")
    if payload.get("category") not in VALID_CATEGORIES:
        issues.append("invalid category")
    if payload.get("priority") not in VALID_PRIORITIES:
        issues.append("invalid priority")
    confidence = payload.get("confidence_score")
    if not isinstance(confidence, int | float) or confidence < 0 or confidence > 1:
        issues.append("confidence_score must be a number from 0.0 to 1.0")
    return issues


def chat_completion(base_url: str, model: str, messages: list[dict], timeout: int) -> str:
    payload = {
        "model": model,
        "messages": messages,
        "temperature": 0,
        "max_tokens": 700,
        "response_format": {"type": "json_object"},
    }
    raw_request = request.Request(
        base_url.rstrip("/") + "/chat/completions",
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with request.urlopen(raw_request, timeout=timeout) as response:
        body = json.loads(response.read().decode("utf-8"))
    return body["choices"][0]["message"]["content"]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--eval-file", default="data/level6-finetuning/eval.chat.jsonl")
    parser.add_argument("--output-file", default="data/level6-finetuning/baseline-eval-results.json")
    parser.add_argument("--base-url", default="http://localhost:8001/v1")
    parser.add_argument("--model", default="Qwen/Qwen2.5-3B-Instruct")
    parser.add_argument("--label", default="baseline")
    parser.add_argument("--timeout", type=int, default=180)
    args = parser.parse_args()

    eval_rows = [
        json.loads(line)
        for line in Path(args.eval_file).read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]

    results = []
    for index, row in enumerate(eval_rows, 1):
        messages = row["messages"][:2]
        expected = json.loads(row["messages"][2]["content"])
        raw_content = chat_completion(args.base_url, args.model, messages, args.timeout)

        parsed_payload = None
        parse_error = None
        validation_issues: list[str] = []
        try:
            parsed_payload = extract_json_object(raw_content)
            validation_issues = validate_payload(parsed_payload)
        except (json.JSONDecodeError, ValueError) as exception:
            parse_error = str(exception)
            validation_issues = ["invalid JSON"]

        result = {
            "index": index,
            "label": args.label,
            "metadata": row.get("metadata", {}),
            "expected": expected,
            "rawContent": raw_content,
            "parsed": parsed_payload,
            "parseError": parse_error,
            "validationIssues": validation_issues,
            "schemaValid": not validation_issues,
            "categoryMatches": bool(parsed_payload and parsed_payload.get("category") == expected.get("category")),
            "priorityMatches": bool(parsed_payload and parsed_payload.get("priority") == expected.get("priority")),
        }
        results.append(result)
        status = "ok" if result["schemaValid"] else "invalid"
        print(
            f"{index}. {row['metadata'].get('reference_number', 'unknown')} "
            f"{status} categoryMatch={result['categoryMatches']} priorityMatch={result['priorityMatches']}"
        )

    summary = {
        "label": args.label,
        "model": args.model,
        "baseUrl": args.base_url,
        "evalFile": args.eval_file,
        "rowCount": len(results),
        "schemaValidCount": sum(1 for result in results if result["schemaValid"]),
        "categoryMatchCount": sum(1 for result in results if result["categoryMatches"]),
        "priorityMatchCount": sum(1 for result in results if result["priorityMatches"]),
        "results": results,
    }
    output_path = Path(args.output_file)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(summary, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps({key: summary[key] for key in summary if key != "results"}, indent=2))


if __name__ == "__main__":
    main()
