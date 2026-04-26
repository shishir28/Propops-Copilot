FROM python:3.12-slim

WORKDIR /app

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

COPY src/ai/propops-ai-service/requirements.txt ./requirements.txt
RUN pip install --no-cache-dir -r requirements.txt

COPY src/ai/propops-ai-service/propops_ai_service ./propops_ai_service

EXPOSE 8000

CMD ["uvicorn", "propops_ai_service.main:app", "--host", "0.0.0.0", "--port", "8000"]
