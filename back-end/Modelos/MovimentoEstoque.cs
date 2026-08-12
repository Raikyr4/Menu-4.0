namespace MenuRestaurante.Api.Modelos;

/// <summary>
/// Tipos de lançamento do livro de estoque (RF-20). O sinal da quantidade acompanha o tipo e
/// é conferido pelo banco: entrada positiva, saída negativa, ajuste e inventário para os dois
/// lados. Assim o saldo é uma soma simples.
/// </summary>
public static class TipoMovimento
{
    /// <summary>Compra recebida. Único tipo que recalcula o custo médio.</summary>
    public const string Entrada = "ENTRADA";

    /// <summary>Baixa gerada pelo fechamento da comanda (AD-04).</summary>
    public const string SaidaVenda = "SAIDA_VENDA";

    /// <summary>Correção manual para mais ou para menos. Exige motivo.</summary>
    public const string Ajuste = "AJUSTE";

    /// <summary>Quebra, vencimento, sobra descartada. Exige motivo.</summary>
    public const string Perda = "PERDA";

    /// <summary>Devolução ao fornecedor.</summary>
    public const string Devolucao = "DEVOLUCAO";

    /// <summary>Acerto vindo de contagem física. Exige motivo.</summary>
    public const string Inventario = "INVENTARIO";

    public static readonly string[] Validos =
        [Entrada, SaidaVenda, Ajuste, Perda, Devolucao, Inventario];

    /// <summary>Tipos que só um lançamento humano cria, e por isso pedem justificativa.</summary>
    public static readonly string[] ExigemMotivo = [Ajuste, Perda, Inventario];

    /// <summary>Tipos lançados manualmente pela tela de estoque.</summary>
    public static readonly string[] Manuais = [Ajuste, Perda, Devolucao];
}

public class MovimentoEstoque
{
    public long Id { get; set; }
    public int InsumoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal? CustoMedioApos { get; set; }
    public int? ComandaId { get; set; }
    public int? CompraId { get; set; }
    public int? UsuarioId { get; set; }
    public string? Motivo { get; set; }
    public DateTimeOffset CriadoEm { get; set; }

    // Preenchidos via JOIN
    public string InsumoNome { get; set; } = string.Empty;
    public string Unidade { get; set; } = string.Empty;
    public string? UsuarioNome { get; set; }
}
