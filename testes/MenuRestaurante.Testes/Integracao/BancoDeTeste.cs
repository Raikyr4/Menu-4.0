using System.Text.Json;
using Dapper;
using MenuRestaurante.Api.Servicos;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace MenuRestaurante.Testes.Integracao;

/// <summary>
/// Cria um banco descartável, aplica as migrações nele e apaga no fim.
///
/// Corrida só se prova com Postgres de verdade: <c>SELECT ... FOR UPDATE</c> e violação de
/// índice único não existem em dublê de memória. Quando não há Postgres configurado, os
/// testes que dependem daqui são pulados em vez de falhar — máquina sem banco continua
/// rodando <c>dotnet test</c>.
///
/// A origem da conexão é, em ordem: a variável de ambiente <c>MENU_TESTES_CONEXAO</c> ou
/// o <c>back-end/appsettings.json</c> local. Nunca um banco nomeado na configuração: o
/// nome do banco é sempre trocado por um descartável.
/// </summary>
public sealed class BancoDeTeste : IAsyncDisposable
{
    private readonly string _conexaoServidor;

    public string Nome { get; }
    public string ConnectionString { get; }

    private BancoDeTeste(string conexaoServidor, string nome, string connectionString)
    {
        _conexaoServidor = conexaoServidor;
        Nome = nome;
        ConnectionString = connectionString;
    }

    /// <summary>Motivo do "pulado" quando não há Postgres alcançável; <c>null</c> se há.</summary>
    public static string? MotivoIndisponivel()
    {
        if (ConexaoBase() is null)
            return "Sem Postgres configurado: defina MENU_TESTES_CONEXAO ou back-end/appsettings.json.";
        return null;
    }

    public static async Task<BancoDeTeste> Criar(string prefixo)
    {
        var baseConexao = ConexaoBase()
            ?? throw new InvalidOperationException(MotivoIndisponivel());

        // Conecta no banco 'postgres' só para poder emitir CREATE DATABASE.
        var servidor = new NpgsqlConnectionStringBuilder(baseConexao) { Database = "postgres" }
            .ConnectionString;

        // Nome de banco no Postgres cabe em 63 caracteres; o sufixo aleatório evita colisão
        // entre execuções paralelas e entre uma execução e o resto que ficou de outra.
        var nome = $"teste_{prefixo}_{Guid.NewGuid():N}".ToLowerInvariant();

        await using (var conexao = new NpgsqlConnection(servidor))
        {
            await conexao.OpenAsync();
            await conexao.ExecuteAsync($"CREATE DATABASE \"{nome}\"");
        }

        var destino = new NpgsqlConnectionStringBuilder(baseConexao) { Database = nome }.ConnectionString;
        return new BancoDeTeste(servidor, nome, destino);
    }

    public void AplicarMigracoes() => MigradorBanco.Aplicar(ConnectionString, NullLogger.Instance);

    public NpgsqlConnection Conectar() => new(ConnectionString);

    public async Task<T?> Escalar<T>(string sql, object? parametros = null)
    {
        await using var conexao = Conectar();
        return await conexao.ExecuteScalarAsync<T>(sql, parametros);
    }

    public async Task Executar(string sql, object? parametros = null)
    {
        await using var conexao = Conectar();
        await conexao.ExecuteAsync(sql, parametros);
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var conexao = new NpgsqlConnection(_conexaoServidor);
        await conexao.OpenAsync();
        await conexao.ExecuteAsync($"DROP DATABASE IF EXISTS \"{Nome}\" WITH (FORCE)");
    }

    private static string? ConexaoBase()
    {
        var doAmbiente = Environment.GetEnvironmentVariable("MENU_TESTES_CONEXAO");
        if (!string.IsNullOrWhiteSpace(doAmbiente))
            return doAmbiente;

        var arquivo = LocalizarAppSettings();
        if (arquivo is null) return null;

        try
        {
            using var documento = JsonDocument.Parse(File.ReadAllText(arquivo));
            return documento.RootElement
                .TryGetProperty("ConnectionStrings", out var secao)
                && secao.TryGetProperty("MenuRestaurante", out var valor)
                    ? valor.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? LocalizarAppSettings()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null)
        {
            var candidato = Path.Combine(diretorio.FullName, "back-end", "appsettings.json");
            if (File.Exists(candidato)) return candidato;
            diretorio = diretorio.Parent;
        }
        return null;
    }
}
