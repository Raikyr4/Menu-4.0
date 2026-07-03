using Npgsql;

namespace MenuRestaurante.Api.Repositorios;

/// <summary>Cria conexões com o Postgres a partir da connection string configurada.</summary>
public class FabricaConexao(IConfiguration configuracao)
{
    private readonly string _connectionString =
        configuracao.GetConnectionString("MenuRestaurante")
        ?? throw new InvalidOperationException("Connection string 'MenuRestaurante' não configurada.");

    public NpgsqlConnection CriarConexao() => new(_connectionString);
}
