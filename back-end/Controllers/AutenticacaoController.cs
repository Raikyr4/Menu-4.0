using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MenuRestaurante.Api.Controllers;

[ApiController]
[Route("api/autenticacao")]
public class AutenticacaoController(UsuarioServico usuarios) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesDeRequisicao.Login)]
    public async Task<IActionResult> Login(LoginRequisicao requisicao)
    {
        var resposta = await usuarios.Autenticar(requisicao);
        return resposta is null
            ? Unauthorized(new { mensagem = "Usuário ou senha incorretos." })
            : Ok(resposta);
    }

    /// <summary>
    /// Instalação nova não tem ninguém para autorizar a primeira conta. A tela de login
    /// pergunta isso antes de decidir se mostra o formulário de criar o primeiro dono.
    /// </summary>
    [HttpGet("primeiro-acesso")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesDeRequisicao.Anonimo)]
    public async Task<IActionResult> PrimeiroAcesso() =>
        Ok(new { precisaDoPrimeiroUsuario = await usuarios.PrecisaDoPrimeiroUsuario() });

    [HttpGet("usuarios")]
    [Authorize(Roles = PapelUsuario.Dono)]
    public async Task<IActionResult> Listar() => Ok(await usuarios.Listar());

    /// <summary>
    /// Criar conta é do dono. A única exceção é a primeira conta do sistema, tratada em
    /// <c>UsuarioServico.Cadastrar</c> — sem ela um clone novo não teria como entrar.
    /// </summary>
    [HttpPost("cadastro")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesDeRequisicao.Login)]
    public async Task<IActionResult> Cadastrar(CadastroRequisicao requisicao)
    {
        var criadoPorDono = User.IsInRole(PapelUsuario.Dono);
        return Ok(await usuarios.Cadastrar(requisicao, criadoPorDono));
    }
}
