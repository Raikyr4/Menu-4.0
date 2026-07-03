using System.ComponentModel.DataAnnotations;
using MenuRestaurante.Api.Modelos;

namespace MenuRestaurante.Api.Dtos;

public record NovoItemRequisicao(
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido")] int ProdutoId,
    [Range(1, 999, ErrorMessage = "Quantidade deve ser entre 1 e 999")] int Quantidade);

public record NovoPagamentoRequisicao(
    [Required(ErrorMessage = "Informe a forma de pagamento")] string Forma,
    [Range(0.01, 9_999_999, ErrorMessage = "Valor deve ser maior que zero")] decimal Valor);

public record TaxaServicoRequisicao(bool Aplicada);

public record NovoAjusteRequisicao(
    [Required(ErrorMessage = "Informe o tipo do ajuste")] string Tipo,
    [Range(0.01, 9_999_999, ErrorMessage = "Valor deve ser maior que zero")] decimal Valor);

/// <summary>Comanda completa com itens, pagamentos e totais calculados no servidor.</summary>
public class ComandaDetalheResposta
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int? MesaNumero { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool TaxaServicoAplicada { get; set; }
    public DateTimeOffset AbertaEm { get; set; }
    public DateTimeOffset? FechadaEm { get; set; }
    public List<ComandaItem> Itens { get; set; } = [];
    public List<Pagamento> Pagamentos { get; set; } = [];
    public List<ComandaAjuste> Ajustes { get; set; } = [];
    public decimal Total { get; set; }
    public decimal TotalDescontos { get; set; }
    public decimal TotalSangrias { get; set; }
    public decimal TaxaServico { get; set; }
    public decimal TotalComTaxa { get; set; }
    public decimal TotalDevido { get; set; }
    public decimal Pago { get; set; }
    public decimal Restante { get; set; }
}

public class MesaResposta
{
    public int Numero { get; set; }
    public bool Ocupada { get; set; }
    public int? ComandaId { get; set; }
    public decimal TotalDevido { get; set; }
    public decimal Restante { get; set; }
}

public class PedidoBalcaoResposta
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset AbertaEm { get; set; }
    public decimal Total { get; set; }
}

public record ResumoFinanceiroResposta(decimal FaturamentoTotal, decimal FaturamentoHoje, decimal TotalEmAbertoNasMesas);
