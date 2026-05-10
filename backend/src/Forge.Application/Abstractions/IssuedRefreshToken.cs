using Forge.Domain;

namespace Forge.Application.Abstractions;

public record IssuedRefreshToken(string RawToken, RefreshToken Stored);
