using Application.DTOs;

namespace Application.Interfaces
{
    public interface ITokenService
    {
        TokenResponse GenerateTokens(
            string userId,
            string email,
            IEnumerable<string> roles);
    }
}
