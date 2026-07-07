using Dapper;
using Menu.Application.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Menu.Infrastructure.Repositories;

public sealed class DapperAuthRepository(IOptions<Menu.Infrastructure.Data.DatabaseOptions> options) : IAuthRepository
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<dynamic?> GetUserByUsernameAsync(string username, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return await cn.QueryFirstOrDefaultAsync(new CommandDefinition("SELECT * FROM tb_users WHERE username = @username", new { username }, cancellationToken: ct));
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        var count = await cn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM tb_users WHERE username = @username", new { username }, cancellationToken: ct));
        return count > 0;
    }

    public async Task CreateUserAsync(string username, string hashedPassword, string nome, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(new CommandDefinition("INSERT INTO tb_users (username, password, nome) VALUES (@username, @password, @nome)", new { username, password = hashedPassword, nome }, cancellationToken: ct));
    }
}
