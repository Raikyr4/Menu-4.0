namespace MenuRestaurante.Api.Modelos;

public class ComandaItemOpcao
{
    public int Id { get; set; }
    public int ComandaItemId { get; set; }
    public string NomeGrupo { get; set; } = string.Empty;
    public string NomeOpcao { get; set; } = string.Empty;
    public decimal PrecoAdicional { get; set; }
}
