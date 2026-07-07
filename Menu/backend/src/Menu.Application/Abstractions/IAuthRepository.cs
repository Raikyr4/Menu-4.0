namespace Menu.Application.Abstractions;

public interface IAuthRepository
{
    Task<dynamic?> GetUserByUsernameAsync(string username, CancellationToken ct);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct);
    Task CreateUserAsync(string username, string hashedPassword, string nome, CancellationToken ct);
}
