using System.ComponentModel.DataAnnotations;

namespace MenuRestaurante.Api.Dtos;

public record LoginRequisicao(
    [Required(ErrorMessage = "Informe o usuário")] string NomeUsuario,
    [Required(ErrorMessage = "Informe a senha")] string Senha);

/// <summary>
/// A regra de senha de verdade está em <c>PoliticaDeSenha</c>, no servidor. Aqui só fica o
/// que o formulário precisa para avisar cedo — quem chama a API direto passa pela outra.
/// </summary>
public record CadastroRequisicao(
    [Required(ErrorMessage = "Informe o usuário")] string NomeUsuario,
    [Required(ErrorMessage = "Informe a senha")] string Senha,
    [Required(ErrorMessage = "Informe o nome")] string Nome,
    string? Papel = null);

public record LoginResposta(string Token, string Nome, string Papel, DateTimeOffset ExpiraEm);

public record UsuarioResposta(int Id, string NomeUsuario, string Nome, string Papel, DateTimeOffset CriadoEm);
