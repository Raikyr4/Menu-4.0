namespace MenuRestaurante.Api.Modelos;

public static class FormaPagamento
{
    public static readonly string[] Validas = ["CREDITO", "DEBITO", "DINHEIRO", "PIX"];
}

public class Pagamento
{
    public int Id { get; set; }
    public int ComandaId { get; set; }
    public string Forma { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTimeOffset PagoEm { get; set; }
}
