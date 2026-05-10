namespace Forge.Application.Profile;

public record LeaderboardEntryDto(Guid UserId, string FirstName, string LastName, int Points, int Rank);
