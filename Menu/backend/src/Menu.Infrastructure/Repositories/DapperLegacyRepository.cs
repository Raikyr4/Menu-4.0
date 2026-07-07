using Dapper;
using Menu.Application.Abstractions;
using Menu.Domain.Enums;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Menu.Infrastructure.Repositories;

public sealed class DapperLegacyRepository(IOptions<Menu.Infrastructure.Data.DatabaseOptions> options) : ILegacyRepository
{
    private readonly string _connectionString = options.Value.ConnectionString;

    private NpgsqlConnection Open() => new(_connectionString);

    public async Task<IReadOnlyList<dynamic>> GetCategoriasAsync(CancellationToken ct)
    {
        await using var cn = Open();
        var rows = await cn.QueryAsync(new CommandDefinition("SELECT * FROM tb_categoria ORDER BY id", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<dynamic>> GetProdutosAsync(CancellationToken ct)
    {
        await using var cn = Open();
        var rows = await cn.QueryAsync(new CommandDefinition("SELECT * FROM tb_produto ORDER BY id", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<decimal> GetTotalRegistroDiarioAsync(CancellationToken ct)
    {
        await using var cn = Open();
        return await cn.ExecuteScalarAsync<decimal?>(new CommandDefinition("SELECT COALESCE(SUM(total),0) FROM tb_registro_diario", cancellationToken: ct)) ?? 0;
    }

    public async Task<decimal> GetTotalMesasAsync(CancellationToken ct)
    {
        await using var cn = Open();
        return await cn.ExecuteScalarAsync<decimal?>(new CommandDefinition("SELECT COALESCE(SUM(total),0) FROM tb_mesa", cancellationToken: ct)) ?? 0;
    }

    public async Task<IReadOnlyList<dynamic>> GetMesasAsync(CancellationToken ct)
    {
        await using var cn = Open();
        var rows = await cn.QueryAsync(new CommandDefinition("SELECT * FROM tb_mesa ORDER BY id", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<dynamic>> GetBalcoesAsync(CancellationToken ct)
    {
        await using var cn = Open();
        var rows = await cn.QueryAsync(new CommandDefinition("SELECT * FROM tb_balcao ORDER BY id", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<dynamic?> GetComandaAsync(AtendimentoTipo tipo, int atendimentoId, CancellationToken ct)
    {
        await using var cn = Open();
        var sql = tipo == AtendimentoTipo.Mesa ? "SELECT * FROM tb_mesa WHERE id = @id" : "SELECT * FROM tb_balcao WHERE id = @id";
        return await cn.QueryFirstOrDefaultAsync(new CommandDefinition(sql, new { id = atendimentoId }, cancellationToken: ct));
    }

    public async Task SalvarComandaAsync(AtendimentoTipo tipo, int atendimentoId, string produtosCsv, decimal total, decimal taxa, decimal totalTaxa, decimal restante, CancellationToken ct)
    {
        await using var cn = Open();
        var sql = tipo == AtendimentoTipo.Mesa
            ? "UPDATE tb_mesa SET produtos = @produtos, total = @total, total_taxa = @totalTaxa, taxa = @taxa, restante = @restante WHERE id = @id"
            : "UPDATE tb_balcao SET produtos = @produtos, total = @total, restante = @restante WHERE id = @id";

        await cn.ExecuteAsync(new CommandDefinition(sql, new { id = atendimentoId, produtos = produtosCsv, total, totalTaxa, taxa, restante }, cancellationToken: ct));
    }

    public async Task SalvarPagamentoAsync(AtendimentoTipo tipo, int atendimentoId, decimal pago, string pagamentos, decimal restante, CancellationToken ct)
    {
        await using var cn = Open();
        var sql = tipo == AtendimentoTipo.Mesa
            ? "UPDATE tb_mesa SET pago = @pago, pagamentos = @pagamentos, restante = @restante WHERE id = @id"
            : "UPDATE tb_balcao SET pago = @pago, pagamentos = @pagamentos, restante = @restante WHERE id = @id";

        await cn.ExecuteAsync(new CommandDefinition(sql, new { id = atendimentoId, pago, pagamentos, restante }, cancellationToken: ct));
    }

    public async Task RegistrarFechamentoAsync(string atendimentoTipo, decimal total, string pagamentos, string produtos, CancellationToken ct)
    {
        await using var cn = Open();
        const string sql = "INSERT INTO tb_registro_diario (atendimento_tipo, total, pagamentos, produtos) VALUES (@atendimentoTipo, @total, @pagamentos, @produtos)";
        await cn.ExecuteAsync(new CommandDefinition(sql, new { atendimentoTipo, total, pagamentos, produtos }, cancellationToken: ct));
    }

    public async Task EncerrarComandaAsync(AtendimentoTipo tipo, int atendimentoId, CancellationToken ct)
    {
        await using var cn = Open();
        var sql = tipo == AtendimentoTipo.Mesa
            ? "UPDATE tb_mesa SET total = 0, total_taxa = 0, taxa = 0, pago = 0, restante = 0, produtos = '', pagamentos = '' WHERE id = @id"
            : "UPDATE tb_balcao SET fechado = true WHERE id = @id";
        await cn.ExecuteAsync(new CommandDefinition(sql, new { id = atendimentoId }, cancellationToken: ct));
    }

    public async Task<int> CriarPedidoBalcaoAsync(CancellationToken ct)
    {
        await using var cn = Open();
        return await cn.ExecuteScalarAsync<int>(new CommandDefinition("INSERT INTO tb_balcao (horario) VALUES (CURRENT_TIME) RETURNING id", cancellationToken: ct));
    }

    public async Task ExcluirPedidoBalcaoAsync(int id, CancellationToken ct)
    {
        await using var cn = Open();
        await cn.ExecuteAsync(new CommandDefinition("DELETE FROM tb_balcao WHERE id = @id", new { id }, cancellationToken: ct));
    }
}
