using MenuRestaurante.Api.Servicos;

namespace MenuRestaurante.Testes;

/// <summary>
/// A regra de senha existe no servidor justamente porque a do formulário é opcional para
/// quem chama a API direto. Estes testes são sobre a do servidor.
/// </summary>
public class PoliticaDeSenhaTestes
{
    [Theory]
    [InlineData("caixa2026")]
    [InlineData("Terradois1")]
    [InlineData("esfiha123456")]
    public void Senha_com_letra_numero_e_tamanho_e_aceita(string senha) =>
        Assert.Null(PoliticaDeSenha.Recusar(senha));

    [Fact]
    public void Senha_vazia_e_recusada() =>
        Assert.Equal("Informe uma senha.", PoliticaDeSenha.Recusar("   "));

    [Theory]
    [InlineData("abc12")]
    [InlineData("cx2026")]
    public void Senha_curta_e_recusada(string senha) =>
        Assert.Equal("A senha precisa ter pelo menos 8 caracteres.", PoliticaDeSenha.Recusar(senha));

    [Theory]
    [InlineData("somenteletras")]
    [InlineData("12345678")]
    public void Senha_sem_mistura_de_letra_e_numero_e_recusada(string senha) =>
        Assert.Equal("A senha precisa misturar letras e números.", PoliticaDeSenha.Recusar(senha));

    [Fact]
    public void Senha_que_contem_o_nome_de_usuario_e_recusada() =>
        Assert.Equal("A senha não pode conter o nome de usuário.",
            PoliticaDeSenha.Recusar("Terradois2026", "terradois"));

    [Fact]
    public void Exigir_lanca_regra_de_negocio_com_a_mesma_mensagem()
    {
        var excecao = Assert.Throws<RegraDeNegocioException>(() => PoliticaDeSenha.Exigir("123"));
        Assert.Equal("A senha precisa ter pelo menos 8 caracteres.", excecao.Message);
    }
}
