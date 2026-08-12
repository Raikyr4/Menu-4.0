using MenuRestaurante.Api.Modelos;

namespace MenuRestaurante.Api.Servicos;

/// <summary>
/// Toda a aritmética de dinheiro da comanda, separada de banco e de HTTP para poder
/// ser testada sozinha. Nenhum outro lugar do sistema calcula total, taxa ou restante.
/// </summary>
public static class CalculadoraComanda
{
    /// <summary>
    /// Arredondamento de dinheiro: meio para cima (0,005 vira 0,01).
    ///
    /// O padrão do .NET é <c>MidpointRounding.ToEven</c> (arredondamento bancário), mas o
    /// Postgres arredonda meio para cima no <c>ROUND(numeric, 2)</c> usado na listagem de mesas.
    /// Com o padrão do .NET as duas telas divergiam em um centavo — por exemplo, consumo de
    /// R$ 10,05 gera taxa bruta de 1,005, que o .NET arredondava para 1,00 e o Postgres para 1,01.
    /// </summary>
    public static decimal ArredondarDinheiro(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    public record Totais(
        decimal Total,
        decimal TaxaServico,
        decimal TotalComTaxa,
        decimal Pago,
        decimal TotalDescontos,
        decimal TotalSangrias,
        decimal Restante);

    /// <summary>
    /// Regras:
    /// - total é a soma de quantidade × preço congelado de cada item;
    /// - taxa só existe quando a comanda a aplica (balcão nunca aplica — regra do serviço);
    /// - descontos e sangrias abatem o que falta pagar, mas não entram no faturamento;
    /// - restante nunca é negativo.
    /// </summary>
    public static Totais Calcular(
        IReadOnlyCollection<ComandaItem> itens,
        IReadOnlyCollection<Pagamento> pagamentos,
        IReadOnlyCollection<ComandaAjuste> ajustes,
        bool taxaServicoAplicada,
        decimal percentualTaxa)
    {
        var total = itens.Sum(item => item.Quantidade * item.PrecoUnitario);
        var taxa = taxaServicoAplicada ? ArredondarDinheiro(total * percentualTaxa) : 0m;
        var totalComTaxa = total + taxa;

        var pago = pagamentos.Sum(pagamento => pagamento.Valor);
        var descontos = ajustes.Where(a => a.Tipo == TipoAjuste.Desconto).Sum(a => a.Valor);
        var sangrias = ajustes.Where(a => a.Tipo == TipoAjuste.Sangria).Sum(a => a.Valor);

        return new Totais(
            Total: total,
            TaxaServico: taxa,
            TotalComTaxa: totalComTaxa,
            Pago: pago,
            TotalDescontos: descontos,
            TotalSangrias: sangrias,
            Restante: Math.Max(totalComTaxa - pago - descontos - sangrias, 0));
    }
}
