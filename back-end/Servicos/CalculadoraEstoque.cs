namespace MenuRestaurante.Api.Servicos;

/// <summary>
/// A aritmética do estoque, separada de banco e de HTTP para poder ser testada sozinha —
/// mesma razão de <see cref="CalculadoraComanda"/>. Nenhum outro lugar calcula custo médio.
/// </summary>
public static class CalculadoraEstoque
{
    /// <summary>
    /// Custo de insumo tem quatro casas, não duas. Uma esfiha leva 0,06 kg de massa: com duas
    /// casas o custo unitário do grama arredondaria para zero e o CMV inteiro sumiria.
    /// O arredondamento é meio para cima, igual ao do dinheiro.
    /// </summary>
    public static decimal ArredondarCusto(decimal valor) =>
        Math.Round(valor, 4, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Custo médio ponderado móvel (RF-24): a média do que está em casa com o que acabou
    /// de chegar, ponderada pelas quantidades.
    ///
    /// Saldo anterior zerado ou negativo devolve o custo da entrada. Ponderar por saldo
    /// negativo daria uma média sem sentido — e saldo negativo é estado válido aqui,
    /// porque falta de estoque não bloqueia venda (AD-06).
    /// </summary>
    public static decimal CustoMedioPonderado(
        decimal saldoAnterior, decimal custoMedioAnterior,
        decimal quantidadeEntrada, decimal custoDaEntrada)
    {
        if (quantidadeEntrada <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidadeEntrada),
                "Entrada de estoque precisa de quantidade maior que zero.");

        if (saldoAnterior <= 0)
            return ArredondarCusto(custoDaEntrada);

        var valorEmCasa = saldoAnterior * custoMedioAnterior;
        var valorQueChegou = quantidadeEntrada * custoDaEntrada;
        return ArredondarCusto((valorEmCasa + valorQueChegou) / (saldoAnterior + quantidadeEntrada));
    }

    /// <summary>Quanto o saldo em casa vale, ao custo médio vigente (RF-30).</summary>
    public static decimal ValorImobilizado(decimal saldo, decimal custoMedio) =>
        saldo <= 0 ? 0m : CalculadoraComanda.ArredondarDinheiro(saldo * custoMedio);

    /// <summary>
    /// Quanto comprar para voltar ao mínimo (RF-31). Saldo negativo entra na conta: quem
    /// deve 3 kg e quer 10 de mínimo precisa comprar 13, não 10.
    /// </summary>
    public static decimal QuantidadeSugerida(decimal saldo, decimal estoqueMinimo) =>
        saldo >= estoqueMinimo ? 0m : estoqueMinimo - saldo;
}
