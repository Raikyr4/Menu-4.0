using MenuRestaurante.Api.Servicos;

namespace MenuRestaurante.Testes;

/// <summary>
/// Custo médio ponderado móvel (RF-24). É a conta que sustenta o CMV e a margem — se ela
/// estiver errada, todo relatório da Fase 4 mente sem dar erro.
/// </summary>
public class CalculadoraEstoqueTestes
{
    [Fact]
    public void Primeira_entrada_define_o_custo_medio()
    {
        var medio = CalculadoraEstoque.CustoMedioPonderado(
            saldoAnterior: 0, custoMedioAnterior: 0,
            quantidadeEntrada: 10, custoDaEntrada: 4.50m);

        Assert.Equal(4.50m, medio);
    }

    [Fact]
    public void Segunda_entrada_com_preco_diferente_pondera_pelas_quantidades()
    {
        // 10 un a R$ 4,00 em casa + 10 un a R$ 6,00 que chegaram = média R$ 5,00
        var medio = CalculadoraEstoque.CustoMedioPonderado(
            saldoAnterior: 10, custoMedioAnterior: 4.00m,
            quantidadeEntrada: 10, custoDaEntrada: 6.00m);

        Assert.Equal(5.00m, medio);
    }

    [Fact]
    public void Quantidades_diferentes_puxam_a_media_para_o_lado_do_lote_maior()
    {
        // 5 kg a R$ 10,00 + 15 kg a R$ 20,00 = (50 + 300) / 20 = R$ 17,50
        var medio = CalculadoraEstoque.CustoMedioPonderado(
            saldoAnterior: 5, custoMedioAnterior: 10.00m,
            quantidadeEntrada: 15, custoDaEntrada: 20.00m);

        Assert.Equal(17.50m, medio);
    }

    /// <summary>
    /// Saldo negativo é estado válido: falta de estoque não bloqueia venda (AD-06). Ponderar
    /// por um saldo negativo daria média sem sentido — ou até negativa.
    /// </summary>
    [Fact]
    public void Entrada_com_saldo_negativo_adota_o_custo_da_entrada()
    {
        var medio = CalculadoraEstoque.CustoMedioPonderado(
            saldoAnterior: -8, custoMedioAnterior: 3.00m,
            quantidadeEntrada: 20, custoDaEntrada: 7.25m);

        Assert.Equal(7.25m, medio);
    }

    [Fact]
    public void Entrada_com_saldo_zerado_adota_o_custo_da_entrada()
    {
        var medio = CalculadoraEstoque.CustoMedioPonderado(
            saldoAnterior: 0, custoMedioAnterior: 99.00m,
            quantidadeEntrada: 3, custoDaEntrada: 1.10m);

        Assert.Equal(1.10m, medio);
    }

    /// <summary>
    /// Quatro casas, não duas. Uma esfiha leva 0,06 kg de massa: com duas casas o custo do
    /// grama arredondaria para zero e o CMV inteiro sumiria.
    /// </summary>
    [Fact]
    public void Custo_medio_mantem_quatro_casas()
    {
        // (1 * 1,00 + 2 * 2,00) / 3 = 1,6666...
        var medio = CalculadoraEstoque.CustoMedioPonderado(
            saldoAnterior: 1, custoMedioAnterior: 1.00m,
            quantidadeEntrada: 2, custoDaEntrada: 2.00m);

        Assert.Equal(1.6667m, medio);
    }

    [Fact]
    public void Custo_medio_arredonda_meio_para_cima()
    {
        // (1 * 1,00 + 1 * 1,00025) / 2 = 1,000125 -> 1,0001 é meio exato na 5a casa
        var medio = CalculadoraEstoque.CustoMedioPonderado(
            saldoAnterior: 1, custoMedioAnterior: 1.0000m,
            quantidadeEntrada: 1, custoDaEntrada: 1.0003m);

        Assert.Equal(1.0002m, medio);
    }

    [Fact]
    public void Entrada_sem_quantidade_e_erro_de_programacao_nao_de_regra()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalculadoraEstoque.CustoMedioPonderado(10, 5m, 0, 3m));
    }

    [Fact]
    public void Valor_imobilizado_e_saldo_vezes_custo_medio_em_duas_casas()
    {
        Assert.Equal(43.75m, CalculadoraEstoque.ValorImobilizado(12.5m, 3.50m));
    }

    [Fact]
    public void Valor_imobilizado_de_saldo_negativo_e_zero()
    {
        // O restaurante não tem dinheiro "negativo" parado na prateleira; a falta aparece
        // no saldo, não no valor.
        Assert.Equal(0m, CalculadoraEstoque.ValorImobilizado(-4m, 10.00m));
    }

    [Theory]
    [InlineData(10, 10, 0)]   // no mínimo exato, não sugere compra
    [InlineData(12, 10, 0)]   // acima do mínimo
    [InlineData(4, 10, 6)]    // falta 6 para o mínimo
    [InlineData(-3, 10, 13)]  // deve 3 e quer 10 de mínimo: precisa de 13
    public void Quantidade_sugerida_repoe_ate_o_minimo(decimal saldo, decimal minimo, decimal esperado) =>
        Assert.Equal(esperado, CalculadoraEstoque.QuantidadeSugerida(saldo, minimo));
}
