using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Repositorios;
using MenuRestaurante.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuRestaurante.Api.Controllers;

[ApiController]
[Route("api/autenticacao")]
public class AutenticacaoController(UsuarioRepositorio usuarios, TokenServico tokens) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequisicao requisicao)
    {
        var usuario = await usuarios.BuscarPorNomeUsuario(requisicao.NomeUsuario);

        // Mensagem única para usuário inexistente e senha errada (não vaza quais logins existem)
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(requisicao.Senha, usuario.SenhaHash))
            return Unauthorized(new { mensagem = "Usuário ou senha incorretos." });

        var (token, expiraEm) = tokens.GerarToken(usuario);
        return Ok(new LoginResposta(token, usuario.Nome, expiraEm));
    }

    [HttpPost("cadastro")]
    [AllowAnonymous]
    public async Task<IActionResult> Cadastrar(CadastroRequisicao requisicao)
    {
        var existente = await usuarios.BuscarPorNomeUsuario(requisicao.NomeUsuario);
        if (existente is not null)
            return Conflict(new { mensagem = "Este nome de usuário já está em uso." });

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(requisicao.Senha);
        await usuarios.Inserir(requisicao.NomeUsuario, senhaHash, requisicao.Nome);

        return Ok(new { mensagem = "Usuário cadastrado com sucesso." });
    }
}
