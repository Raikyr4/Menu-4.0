namespace MenuRestaurante.Api.Modelos;

public class Fornecedor
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefone { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }
}

public class Compra
{
    public int Id { get; set; }
    public int FornecedorId { get; set; }
    public string? Documento { get; set; }
    public DateOnly DataCompra { get; set; }
    public int? UsuarioId { get; set; }
    public DateTimeOffset CriadoEm { get; set; }

    // Preenchidos via JOIN
    public string FornecedorNome { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
}

public class CompraItem
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int InsumoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal CustoUnitario { get; set; }

    public string InsumoNome { get; set; } = string.Empty;
    public string Unidade { get; set; } = string.Empty;
}
