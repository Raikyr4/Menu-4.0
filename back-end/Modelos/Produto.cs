namespace MenuRestaurante.Api.Modelos;

public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public bool Especial { get; set; }
    public decimal Preco { get; set; }
    public decimal ValorKg { get; set; }
    public bool Ativo { get; set; }
    public List<ProdutoOpcaoGrupo> GruposOpcoes { get; set; } = [];
}
