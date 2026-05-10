namespace Forge.Application.Sessions;

public record SessionPage(
    IReadOnlyList<SessionDto> Items,
    int Page,
    int PageSize,
    int Total
);
