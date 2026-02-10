using Menu.Domain.Entities;
using Menu.Domain.Enums;

namespace Menu.Application.Abstractions;

public interface IComandaRepository
{
    Task<IReadOnlyList<int>> ObterProdutoIdsAsync(AtendimentoTipo tipo, int atendimentoId, CancellationToken ct);
    Task<ProdutoComanda?> ObterProdutoAsync(int produtoId, CancellationToken ct);
    Task SalvarComandaAsync(AtendimentoTipo tipo, int atendimentoId, string produtosCsv, decimal total, decimal taxa, decimal totalTaxa, decimal restante, CancellationToken ct);
}
