namespace MenuRestaurante.Api.Modelos;

public class ComandaItem
{
    public int Id { get; set; }
    public int ComandaId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }

    // Preenchido via JOIN com produto
    public string ProdutoNome { get; set; } = string.Empty;
}
