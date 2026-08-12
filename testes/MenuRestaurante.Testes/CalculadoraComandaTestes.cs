using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Servicos;

namespace MenuRestaurante.Testes;

/// <summary>
/// Toda conta de dinheiro do sistema passa por <see cref="CalculadoraComanda"/>.
/// Se algum destes testes quebrar, o caixa do restaurante fecha errado.
/// </summary>
public class CalculadoraComandaTestes
{
    private const decimal Taxa10 = 0.10m;

    private static ComandaItem Item(decimal quantidade, decimal precoUnitario, string unidade = "UN") =>
        new() { Quantidade = quantidade, PrecoUnitario = precoUnitario, Unidade = unidade };

    private static Pagamento Pago(decimal valor, string forma = "DINHEIRO") =>
        new() { Valor = valor, Forma = forma };

    private static ComandaAjuste Desconto(decimal valor) =>
        new() { Tipo = TipoAjuste.Desconto, Valor = valor };

    private static ComandaAjuste Sangria(decimal valor) =>
        new() { Tipo = TipoAjuste.Sangria, Valor = valor };

    private static CalculadoraComanda.Totais Calcular(
        IReadOnlyCollection<ComandaItem>? itens = null,
        IReadOnlyCollection<Pagamento>? pagamentos = null,
        IReadOnlyCollection<ComandaAjuste>? ajustes = null,
        bool taxaAplicada = false,
        decimal percentualTaxa = Taxa10) =>
        CalculadoraComanda.Calcular(
            itens ?? [], pagamentos ?? [], ajustes ?? [], taxaAplicada, percentualTaxa);

    // ---------- Total ----------

    [Fact]
    public void Comanda_vazia_zera_tudo()
    {
        var totais = Calcular();

        Assert.Equal(0m, totais.Total);
        Assert.Equal(0m, totais.TaxaServico);
        Assert.Equal(0m, totais.Restante);
    }

    [Fact]
    public void Total_soma_quantidade_vezes_preco_congelado()
    {
        var totais = Calcular([Item(3, 14.90m), Item(2, 5.00m)]);

        Assert.Equal(54.70m, totais.Total);
    }

    [Fact]
    public void Produto_por_peso_usa_quantidade_fracionada()
    {
        // 350 g de um produto a R$ 89,90/kg
        var totais = Calcular([Item(0.350m, 89.90m, "KG")]);

        Assert.Equal(31.465m, totais.Total);
    }

    // ---------- Taxa de serviço ----------

    [Fact]
    public void Sem_taxa_aplicada_a_taxa_e_zero()
    {
        var totais = Calcular([Item(1, 100.00m)], taxaAplicada: false);

        Assert.Equal(0m, totais.TaxaServico);
        Assert.Equal(100.00m, totais.TotalComTaxa);
    }

    [Fact]
    public void Com_taxa_aplicada_soma_dez_por_cento()
    {
        var totais = Calcular([Item(1, 100.00m)], taxaAplicada: true);

        Assert.Equal(10.00m, totais.TaxaServico);
        Assert.Equal(110.00m, totais.TotalComTaxa);
    }

    [Fact]
    public void Percentual_de_taxa_vem_da_configuracao()
    {
        var totais = Calcular([Item(1, 200.00m)], taxaAplicada: true, percentualTaxa: 0.13m);

        Assert.Equal(26.00m, totais.TaxaServico);
    }

    [Fact]
    public void Taxa_arredonda_meio_centavo_para_cima_igual_ao_Postgres()
    {
        // Consumo de R$ 10,05 dá taxa bruta de 1,005.
        // O arredondamento bancário do .NET devolveria 1,00 e o ROUND do Postgres
        // (usado na listagem de mesas) devolve 1,01 — a tela da mesa e a da comanda
        // divergiam em um centavo. Ver CalculadoraComanda.ArredondarDinheiro.
        var totais = Calcular([Item(1, 10.05m)], taxaAplicada: true);

        Assert.Equal(1.01m, totais.TaxaServico);
        Assert.Equal(11.06m, totais.TotalComTaxa);
    }

    [Theory]
    [InlineData(0.005, 0.01)]
    [InlineData(0.015, 0.02)]
    [InlineData(0.025, 0.03)]
    [InlineData(1.004, 1.00)]
    [InlineData(1.006, 1.01)]
    public void Arredondamento_de_dinheiro_e_sempre_meio_para_cima(decimal valor, decimal esperado)
    {
        Assert.Equal(esperado, CalculadoraComanda.ArredondarDinheiro(valor));
    }

    // ---------- Pagamentos ----------

    [Fact]
    public void Pagamento_parcial_deixa_restante()
    {
        var totais = Calcular([Item(1, 100.00m)], [Pago(30.00m)], taxaAplicada: true);

        Assert.Equal(30.00m, totais.Pago);
        Assert.Equal(80.00m, totais.Restante);
    }

    [Fact]
    public void Pagamentos_de_formas_diferentes_somam()
    {
        var totais = Calcular(
            [Item(1, 100.00m)],
            [Pago(40.00m, "PIX"), Pago(35.00m, "CREDITO"), Pago(25.00m, "DINHEIRO")]);

        Assert.Equal(100.00m, totais.Pago);
        Assert.Equal(0m, totais.Restante);
    }

    [Fact]
    public void Restante_nunca_fica_negativo()
    {
        var totais = Calcular([Item(1, 50.00m)], [Pago(80.00m)]);

        Assert.Equal(0m, totais.Restante);
    }

    // ---------- Ajustes ----------

    [Fact]
    public void Desconto_abate_o_restante_mas_nao_conta_como_pago()
    {
        var totais = Calcular([Item(1, 100.00m)], [Pago(60.00m)], [Desconto(40.00m)]);

        Assert.Equal(60.00m, totais.Pago);
        Assert.Equal(40.00m, totais.TotalDescontos);
        Assert.Equal(0m, totais.Restante);
    }

    [Fact]
    public void Sangria_abate_o_restante_mas_nao_conta_como_pago()
    {
        var totais = Calcular([Item(1, 100.00m)], [Pago(70.00m)], [Sangria(30.00m)]);

        Assert.Equal(70.00m, totais.Pago);
        Assert.Equal(30.00m, totais.TotalSangrias);
        Assert.Equal(0m, totais.Restante);
    }

    [Fact]
    public void Desconto_e_sangria_sao_somados_separadamente()
    {
        var totais = Calcular(
            [Item(1, 100.00m)],
            ajustes: [Desconto(10.00m), Desconto(5.00m), Sangria(20.00m)]);

        Assert.Equal(15.00m, totais.TotalDescontos);
        Assert.Equal(20.00m, totais.TotalSangrias);
        Assert.Equal(65.00m, totais.Restante);
    }

    // ---------- Cenário completo ----------

    [Fact]
    public void Mesa_com_taxa_pagamento_dividido_e_desconto_fecha_zerada()
    {
        var totais = Calcular(
            itens: [Item(4, 12.50m), Item(2, 8.00m), Item(0.250m, 120.00m, "KG")],
            pagamentos: [Pago(50.00m, "PIX"), Pago(51.00m, "DEBITO")],
            ajustes: [Desconto(5.60m)],
            taxaAplicada: true);

        Assert.Equal(96.00m, totais.Total);          // 50,00 + 16,00 + 30,00
        Assert.Equal(9.60m, totais.TaxaServico);     // 10% de 96,00
        Assert.Equal(105.60m, totais.TotalComTaxa);
        Assert.Equal(101.00m, totais.Pago);
        Assert.Equal(0m, totais.Restante);           // 105,60 - 101,00 - 5,60
    }

    [Fact]
    public void Balcao_nunca_tem_taxa_mesmo_com_valor_alto()
    {
        // A regra de que balcão não aplica taxa mora em ComandaServico.AlterarTaxaServico;
        // aqui garantimos que, não aplicada, a taxa é exatamente zero e não arredonda nada.
        var totais = Calcular([Item(10, 99.99m)], taxaAplicada: false);

        Assert.Equal(0m, totais.TaxaServico);
        Assert.Equal(999.90m, totais.Total);
        Assert.Equal(999.90m, totais.TotalComTaxa);
    }
}
