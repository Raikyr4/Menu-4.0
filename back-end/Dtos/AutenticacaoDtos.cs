using System.ComponentModel.DataAnnotations;

namespace MenuRestaurante.Api.Dtos;

public record LoginRequisicao(
    [Required(ErrorMessage = "Informe o usuário")] string NomeUsuario,
    [Required(ErrorMessage = "Informe a senha")] string Senha);

public record CadastroRequisicao(
    [Required(ErrorMessage = "Informe o usuário")] string NomeUsuario,
    [Required(ErrorMessage = "Informe a senha"), MinLength(4, ErrorMessage = "Senha precisa de ao menos 4 caracteres")] string Senha,
    [Required(ErrorMessage = "Informe o nome")] string Nome);

public record LoginResposta(string Token, string Nome, DateTimeOffset ExpiraEm);
