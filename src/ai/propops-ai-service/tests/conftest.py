from collections.abc import Iterator

import pytest

from propops_ai_service.inference import load_inference_settings
from propops_ai_service.models import MaintenanceTriageInputContract


@pytest.fixture(autouse=True)
def clear_inference_settings_cache() -> Iterator[None]:
    load_inference_settings.cache_clear()
    yield
    load_inference_settings.cache_clear()


@pytest.fixture
def make_request():
    def factory(**overrides: object) -> MaintenanceTriageInputContract:
        payload = {
            "request_id": "request-1",
            "reference_number": "MR-202604270001",
            "normalized_text": "Kitchen sink pipe is leaking heavily under the cabinet.",
            "property_name": "Harbour View Residences",
            "unit_number": "22A",
            "channel": "Portal",
            "category_hint": "Plumbing",
            "priority_hint": "High",
            "is_after_hours": False,
            "submitted_at_utc": "2026-04-27T00:00:00Z",
        }
        payload.update(overrides)
        return MaintenanceTriageInputContract.model_validate(payload)

    return factory
