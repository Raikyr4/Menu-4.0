using System.ComponentModel.DataAnnotations;

namespace MenuRestaurante.Api.Dtos;

public record InsumoRequisicao(
    [Required(ErrorMessage = "Informe o nome do insumo")] string Nome,
    [Required(ErrorMessage = "Informe a unidade")] string Unidade,
    [Required(ErrorMessage = "Informe o tipo")] string Tipo,
    string? Categoria,
    decimal EstoqueMinimo);

/// <summary>Insumo com o que veio do livro: saldo, custo médio e valor em casa (RF-30).</summary>
public class InsumoComSaldoResposta
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Unidade { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal EstoqueMinimo { get; set; }
    public decimal Saldo { get; set; }
    public decimal CustoMedio { get; set; }
    public decimal ValorImobilizado { get; set; }
    /// <summary>Abaixo do mínimo. Só sinaliza — nunca impede venda (AD-06).</summary>
    public bool AbaixoDoMinimo { get; set; }
    public decimal QuantidadeSugerida { get; set; }
}

public record LancamentoManualRequisicao(
    [Range(1, int.MaxValue, ErrorMessage = "Insumo inválido")] int InsumoId,
    [Required(ErrorMessage = "Informe o tipo do lançamento")] string Tipo,
    decimal Quantidade,
    [Required(ErrorMessage = "Informe o motivo")] string Motivo);

public record FornecedorRequisicao(
    [Required(ErrorMessage = "Informe o nome do fornecedor")] string Nome,
    string? Documento,
    string? Telefone);

public record CompraItemRequisicao(
    [Range(1, int.MaxValue, ErrorMessage = "Insumo inválido")] int InsumoId,
    decimal Quantidade,
    decimal CustoUnitario);

public record CompraRequisicao(
    [Range(1, int.MaxValue, ErrorMessage = "Fornecedor inválido")] int FornecedorId,
    string? Documento,
    DateOnly? DataCompra,
    List<CompraItemRequisicao> Itens);

public record CompraResposta(
    int Id, int FornecedorId, string FornecedorNome, string? Documento,
    DateOnly DataCompra, decimal ValorTotal, List<CompraItemResposta> Itens);

public record CompraItemResposta(
    int InsumoId, string InsumoNome, string Unidade,
    decimal Quantidade, decimal CustoUnitario, decimal Subtotal);
