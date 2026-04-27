from functools import lru_cache
from importlib.resources import files
import json
from typing import Any


@lru_cache
def load_knowledge_base() -> dict[str, Any]:
    knowledge_path = files("propops_ai_service.data").joinpath("knowledge_base.json")
    with knowledge_path.open("r", encoding="utf-8") as handle:
        return json.load(handle)
