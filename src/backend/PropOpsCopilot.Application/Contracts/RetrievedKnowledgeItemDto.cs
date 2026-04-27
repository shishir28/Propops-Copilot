namespace PropOpsCopilot.Application.Contracts;

public sealed record RetrievedKnowledgeItemDto(
    string SourceType,
    string Key,
    string Title,
    string Content,
    string Rationale);
