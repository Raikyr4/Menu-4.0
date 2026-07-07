using Menu.Domain.Enums;

namespace Menu.Domain.Entities;

public sealed class Comanda
{
    public int Id { get; init; }
    public AtendimentoTipo Tipo { get; init; }
    public decimal Total { get; private set; }
    public decimal TaxaServico { get; private set; }
    public decimal TotalComTaxa { get; private set; }
    public decimal Pago { get; private set; }
    public decimal Restante { get; private set; }
    public bool TaxaHabilitada { get; private set; } = true;

    public void Recalcular(IEnumerable<ProdutoComanda> itens)
    {
        Total = itens.Sum(x => x.Preco * x.Quantidade);
        TaxaServico = Tipo is AtendimentoTipo.Balcao || !TaxaHabilitada ? 0m : decimal.Round(Total * 0.10m, 2);
        TotalComTaxa = Total + TaxaServico;
        Restante = (TaxaHabilitada ? TotalComTaxa : Total) - Pago;
    }

    public void ConfigurarTaxa(bool habilitada)
    {
        TaxaHabilitada = Tipo is AtendimentoTipo.Balcao ? false : habilitada;
    }

    public void RegistrarPagamento(decimal valor)
    {
        Pago += valor;
        Restante = (TaxaHabilitada ? TotalComTaxa : Total) - Pago;
    }
}
