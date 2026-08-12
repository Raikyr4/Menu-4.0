namespace MenuRestaurante.Api.Modelos;

public static class UnidadeInsumo
{
    public const string Quilo = "KG";
    public const string Grama = "G";
    public const string Litro = "L";
    public const string Mililitro = "ML";
    public const string Unidade = "UN";

    public static readonly string[] Validas = [Quilo, Grama, Litro, Mililitro, Unidade];
}

public static class TipoInsumo
{
    /// <summary>Vendido como está: refrigerante em lata, cerveja, charuto.</summary>
    public const string Revenda = "REVENDA";

    /// <summary>Vira outra coisa antes de ser vendido: farinha, carne moída.</summary>
    public const string MateriaPrima = "MATERIA_PRIMA";

    public static readonly string[] Validos = [Revenda, MateriaPrima];
}

public class Insumo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Unidade { get; set; } = UnidadeInsumo.Unidade;
    public string Tipo { get; set; } = TipoInsumo.Revenda;
    public string Categoria { get; set; } = "Geral";
    public decimal EstoqueMinimo { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }
}
