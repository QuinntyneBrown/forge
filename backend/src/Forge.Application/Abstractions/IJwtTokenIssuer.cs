using Forge.Domain;

namespace Forge.Application.Abstractions;

public interface IJwtTokenIssuer
{
    string Issue(User user);
}
