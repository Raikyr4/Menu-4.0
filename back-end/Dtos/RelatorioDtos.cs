namespace MenuRestaurante.Api.Dtos;

/// <summary>Movimento de um dia de caixa: tudo que entrou entre 00:00 e 23:59.</summary>
public class CaixaDiarioResposta
{
    public DateOnly Data { get; set; }
    public decimal Total { get; set; }
    public int QuantidadePagamentos { get; set; }
    public int ComandasFechadas { get; set; }
}

public class VendaPorPeriodoResposta
{
    public DateOnly Periodo { get; set; }
    public decimal Total { get; set; }
}

public class ProdutoMaisVendidoResposta
{
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal Total { get; set; }
}

public class VendaPorFormaPagamentoResposta
{
    public string Forma { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
}

public class AjusteDiarioResposta
{
    public DateOnly Data { get; set; }
    public decimal Descontos { get; set; }
    public decimal Sangrias { get; set; }
}

/// <summary>Mesas fechadas sem cobrar a taxa de serviço no período.</summary>
public class TaxaNaoCobradaResposta
{
    public int QuantidadeComandas { get; set; }
    public decimal TotalConsumo { get; set; }
    public decimal TaxaNaoCobrada { get; set; }
}
