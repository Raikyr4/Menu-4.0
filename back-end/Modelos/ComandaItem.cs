namespace MenuRestaurante.Api.Modelos;

public class ComandaItem
{
    public int Id { get; set; }
    public int ComandaId { get; set; }
    public int ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public string Unidade { get; set; } = "UN";
    public decimal PrecoUnitario { get; set; }
    public List<ComandaItemOpcao> Opcoes { get; set; } = [];

    // Preenchido via JOIN com produto
    public string ProdutoNome { get; set; } = string.Empty;
}
