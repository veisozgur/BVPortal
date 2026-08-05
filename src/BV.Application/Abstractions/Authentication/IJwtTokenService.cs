using BV.Domain.Entities;

namespace BV.Application.Abstractions.Authentication;

public interface IJwtTokenService
{
    string CreateAccessToken(User user, IReadOnlyCollection<string> roles);
    string CreateRefreshToken();
}
