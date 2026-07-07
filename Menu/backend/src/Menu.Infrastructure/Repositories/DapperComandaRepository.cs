using Dapper;
using Menu.Application.Abstractions;
using Menu.Domain.Entities;
using Menu.Domain.Enums;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Menu.Infrastructure.Repositories;

public sealed class DapperComandaRepository(IOptions<Menu.Infrastructure.Data.DatabaseOptions> options) : IComandaRepository
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<int>> ObterProdutoIdsAsync(AtendimentoTipo tipo, int atendimentoId, CancellationToken ct)
    {
        const string mesaSql = "SELECT produtos FROM tb_mesa WHERE id = @id";
        const string balcaoSql = "SELECT produtos FROM tb_balcao WHERE id = @id";

        await using var cn = new NpgsqlConnection(_connectionString);
        var csv = await cn.ExecuteScalarAsync<string?>(new CommandDefinition(tipo == AtendimentoTipo.Mesa ? mesaSql : balcaoSql, new { id = atendimentoId }, cancellationToken: ct));

        return string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
    }

    public async Task<ProdutoComanda?> ObterProdutoAsync(int produtoId, CancellationToken ct)
    {
        const string sql = "SELECT id, produto_nm AS Nome, preco FROM tb_produto WHERE id = @id";
        await using var cn = new NpgsqlConnection(_connectionString);
        var row = await cn.QueryFirstOrDefaultAsync(sql, new { id = produtoId });
        return row is null ? null : new ProdutoComanda((int)row.id, (string)row.nome, (decimal)row.preco, 1);
    }

    public async Task SalvarComandaAsync(AtendimentoTipo tipo, int atendimentoId, string produtosCsv, decimal total, decimal taxa, decimal totalTaxa, decimal restante, CancellationToken ct)
    {
        const string mesaSql = "UPDATE tb_mesa SET produtos = @produtos, total = @total, taxa = @taxa, total_taxa = @totalTaxa, restante = @restante WHERE id = @id";
        const string balcaoSql = "UPDATE tb_balcao SET produtos = @produtos, total = @total, restante = @restante WHERE id = @id";

        await using var cn = new NpgsqlConnection(_connectionString);
        var sql = tipo == AtendimentoTipo.Mesa ? mesaSql : balcaoSql;
        await cn.ExecuteAsync(new CommandDefinition(sql, new
        {
            id = atendimentoId,
            produtos = produtosCsv,
            total,
            taxa,
            totalTaxa,
            restante
        }, cancellationToken: ct));
    }
}
