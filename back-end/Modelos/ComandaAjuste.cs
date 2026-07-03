namespace MenuRestaurante.Api.Modelos;

public static class TipoAjuste
{
    public const string Desconto = "DESCONTO";
    public const string Sangria = "SANGRIA";
    public static readonly string[] Validos = [Desconto, Sangria];
}

/// <summary>Parte do valor devido que não foi paga: desconto concedido ou sangria.</summary>
public class ComandaAjuste
{
    public int Id { get; set; }
    public int ComandaId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
