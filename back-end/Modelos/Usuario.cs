namespace MenuRestaurante.Api.Modelos;

public class Usuario
{
    public int Id { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
}
