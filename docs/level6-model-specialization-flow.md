# Level 6 Model Specialization Flow

This diagram captures the current Level 6 learning path for PropOps Copilot: local model serving with vLLM, dataset export, LoRA training, evaluation, and later serving the tuned adapter.

```mermaid
flowchart TD
    A[Level 5 feedback loop complete] --> B[Export fine-tuning candidates]
    B --> C[Convert candidates to chat JSONL]
    C --> D[Split dataset]
    D --> D1[train.chat.jsonl]
    D --> D2[eval.chat.jsonl]
    D --> D3[manifest.json and raw export]

    D2 --> E[3A: Baseline eval]
    E --> E1[Run eval against base Qwen via vLLM]
    E1 --> E2[Save baseline-eval-results.json]
    E2 --> E3[Baseline metrics: schema, category, priority]

    E3 --> F[3B: Choose training target]
    F --> F1[Base model: Qwen/Qwen2.5-3B-Instruct]
    F --> F2[Method: Hugging Face + TRL + PEFT]
    F --> F3[First successful path: regular LoRA]
    F --> F4[QLoRA deferred because 4-bit loading was slow on this stack]

    F --> G[3C: Train adapter]
    G --> G1[Create .venv-finetune]
    G1 --> G2[Install torch, transformers, datasets, peft, trl, accelerate, bitsandbytes]
    G2 --> G3[Create train_level6_lora.py]
    G3 --> G4[Stop vLLM to free GPU memory]
    G4 --> G5[Run 2-step smoke LoRA training]
    G5 --> G6[Confirm smoke adapter files]
    G6 --> G7[Run 20-step LoRA training]
    G7 --> G8[Confirm adapter_config.json and adapter_model.safetensors]

    G8 --> H[3D: Compare base vs adapter]
    H --> H1[Keep vLLM stopped for local Python comparison]
    H1 --> H2[Load base Qwen + LoRA adapter with PEFT]
    H2 --> H3[Run eval.chat.jsonl]
    H3 --> H4[Save lora-eval-results.json]
    H4 --> H5[Compare metrics against baseline]

    H5 --> I{Did adapter improve enough?}
    I -->|No| J[Improve dataset or training settings]
    J --> J1[Add more reviewed feedback examples]
    J --> J2[Improve train/eval split]
    J --> J3[Try more steps or adjusted LoRA settings]
    J3 --> G7

    I -->|Yes or ready to learn serving| K[3E: Serve adapter with vLLM]
    K --> K1[Start vLLM with base model and LoRA support]
    K1 --> K2[Mount local adapter directory into container]
    K2 --> K3[Expose adapter as model name]
    K3 --> K4[Point PropOps AI service model name to adapter]
    K4 --> K5[Test Angular -> .NET -> Python AI service -> vLLM -> base + adapter]
```

## Runtime Separation

```mermaid
flowchart LR
    subgraph Serving[Serving / Inference]
        A1[Angular UI] --> A2[.NET API]
        A2 --> A3[Python AI service]
        A3 --> A4[vLLM]
        A4 --> A5[Base model or base model + LoRA adapter]
    end

    subgraph Training[Training / Fine-Tuning]
        B1[train.chat.jsonl] --> B2[train_level6_lora.py]
        B2 --> B3[PyTorch + TRL + PEFT]
        B3 --> B4[Base model loaded for training]
        B4 --> B5[LoRA adapter weights updated]
        B5 --> B6[adapter_config.json and adapter_model.safetensors]
    end
```

Key rule:

```text
vLLM is for inference/serving.
PyTorch + TRL + PEFT is for training.
Stop vLLM during local training/eval if GPU memory is needed.
Restart vLLM when serving the base model or tuned adapter.
```

## Current Checkpoint

```text
Step 12 serving base model with vLLM: done
3A baseline eval: done
3B target/method choice: done
3C LoRA adapter training: done
3D base vs LoRA comparison: done
3E serving LoRA through vLLM: done
```

Current app-facing model:

```text
PROP_OPS_AI_MODEL_NAME=propops-qwen2.5-3b-lora
PROP_OPS_AI_OPENAI_BASE_URL=http://host.docker.internal:8001/v1
```

The verified runtime path is:

```text
Angular -> .NET API -> Python AI service -> vLLM -> propops-qwen2.5-3b-lora
```
