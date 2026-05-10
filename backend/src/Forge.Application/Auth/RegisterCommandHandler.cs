using Forge.Application.Abstractions;
using Forge.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Forge.Application.Auth;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenIssuer _tokens;

    public RegisterCommandHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenIssuer tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existing = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (existing)
        {
            throw new EmailAlreadyRegisteredException(normalizedEmail);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Role = "User",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var token = _tokens.Issue(user);
        return new AuthResult(token, user.Id, user.Email, user.Role);
    }
}
