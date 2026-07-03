namespace MenuRestaurante.Api.Modelos;

public class ProdutoOpcaoGrupo
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Obrigatorio { get; set; }
    public bool SelecaoMultipla { get; set; }
    public int Ordem { get; set; }
    public List<ProdutoOpcao> Opcoes { get; set; } = [];
}

public class ProdutoOpcao
{
    public int Id { get; set; }
    public int GrupoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal PrecoAdicional { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; }
}
