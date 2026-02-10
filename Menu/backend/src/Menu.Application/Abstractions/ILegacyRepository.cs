using Menu.Domain.Enums;

namespace Menu.Application.Abstractions;

public interface ILegacyRepository
{
    Task<IReadOnlyList<dynamic>> GetCategoriasAsync(CancellationToken ct);
    Task<IReadOnlyList<dynamic>> GetProdutosAsync(CancellationToken ct);
    Task<decimal> GetTotalRegistroDiarioAsync(CancellationToken ct);
    Task<decimal> GetTotalMesasAsync(CancellationToken ct);
    Task<IReadOnlyList<dynamic>> GetMesasAsync(CancellationToken ct);
    Task<IReadOnlyList<dynamic>> GetBalcoesAsync(CancellationToken ct);

    Task<dynamic?> GetComandaAsync(AtendimentoTipo tipo, int atendimentoId, CancellationToken ct);
    Task SalvarComandaAsync(AtendimentoTipo tipo, int atendimentoId, string produtosCsv, decimal total, decimal taxa, decimal totalTaxa, decimal restante, CancellationToken ct);
    Task SalvarPagamentoAsync(AtendimentoTipo tipo, int atendimentoId, decimal pago, string pagamentos, decimal restante, CancellationToken ct);
    Task RegistrarFechamentoAsync(string atendimentoTipo, decimal total, string pagamentos, string produtos, CancellationToken ct);
    Task EncerrarComandaAsync(AtendimentoTipo tipo, int atendimentoId, CancellationToken ct);

    Task<int> CriarPedidoBalcaoAsync(CancellationToken ct);
    Task ExcluirPedidoBalcaoAsync(int id, CancellationToken ct);
}
