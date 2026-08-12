namespace MenuRestaurante.Api.Servicos;

/// <summary>
/// Regra de senha, sem banco e sem HTTP — a validação que vale é esta, no servidor.
/// O <c>minLength</c> do formulário some com um F12; este não.
///
/// O alvo é o dono do restaurante e os atendentes, não um time de segurança: exigir
/// símbolo e maiúscula faria a senha virar bilhete colado no monitor. Comprimento e a
/// mistura de letra com número é o que sobra de útil.
/// </summary>
public static class PoliticaDeSenha
{
    public const int ComprimentoMinimo = 8;

    /// <summary>Devolve o motivo da recusa, ou <c>null</c> quando a senha serve.</summary>
    public static string? Recusar(string? senha, string? nomeUsuario = null)
    {
        if (string.IsNullOrWhiteSpace(senha))
            return "Informe uma senha.";

        if (senha.Length < ComprimentoMinimo)
            return $"A senha precisa ter pelo menos {ComprimentoMinimo} caracteres.";

        if (!senha.Any(char.IsLetter) || !senha.Any(char.IsDigit))
            return "A senha precisa misturar letras e números.";

        if (!string.IsNullOrWhiteSpace(nomeUsuario)
            && senha.Contains(nomeUsuario, StringComparison.OrdinalIgnoreCase))
            return "A senha não pode conter o nome de usuário.";

        return null;
    }

    /// <summary>Mesma regra, no formato que o serviço usa.</summary>
    public static void Exigir(string? senha, string? nomeUsuario = null)
    {
        var motivo = Recusar(senha, nomeUsuario);
        if (motivo is not null) throw new RegraDeNegocioException(motivo);
    }
}
