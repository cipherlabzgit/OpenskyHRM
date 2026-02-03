using Tenant.Domain.Entities;

namespace Tenant.Application.Interfaces;

public interface IAuthService
{
    Task<(User User, string AccessToken, string RefreshToken, DateTime ExpiresAt)> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<User> CreateUserAsync(
        string email,
        string password,
        string fullName,
        Guid? roleId = null,
        CancellationToken cancellationToken = default);

    string HashPassword(string password);
}
