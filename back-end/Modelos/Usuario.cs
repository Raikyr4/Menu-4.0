namespace MenuRestaurante.Api.Modelos;

/// <summary>
/// Papéis de acesso. <c>DONO</c> vê faturamento e altera cadastro; <c>OPERADOR</c> atende
/// mesa e balcão. O papel viaja no token como a claim <see cref="TipoDeClaim"/>.
/// </summary>
public static class PapelUsuario
{
    public const string Dono = "DONO";
    public const string Operador = "OPERADOR";

    /// <summary>Nome da claim de papel no JWT — registrado como RoleClaimType no Program.cs.</summary>
    public const string TipoDeClaim = "papel";

    public static readonly string[] Validos = [Dono, Operador];
}

public class Usuario
{
    public int Id { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Papel { get; set; } = PapelUsuario.Operador;
    public DateTimeOffset CriadoEm { get; set; }
}
