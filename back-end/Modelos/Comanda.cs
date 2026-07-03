namespace MenuRestaurante.Api.Modelos;

public static class TipoComanda
{
    public const string Mesa = "MESA";
    public const string Balcao = "BALCAO";
}

public static class StatusComanda
{
    public const string Aberta = "ABERTA";
    public const string Fechada = "FECHADA";
}

public class Comanda
{
    public int Id { get; set; }
    public string Tipo { get; set; } = TipoComanda.Mesa;
    public int? MesaNumero { get; set; }
    public string Status { get; set; } = StatusComanda.Aberta;
    public bool TaxaServicoAplicada { get; set; }
    public DateTimeOffset AbertaEm { get; set; }
    public DateTimeOffset? FechadaEm { get; set; }
}
