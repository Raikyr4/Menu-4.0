using Menu.Api.Contracts;
using Menu.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Menu.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class AuthController(IAuthRepository authRepository) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await authRepository.GetUserByUsernameAsync(request.Username, ct);
        if (user is null) return BadRequest("Usuário ou Senha incorreta");

        var ok = BCrypt.Net.BCrypt.Verify(request.Password, (string)user.password);
        if (!ok) return BadRequest("Senha incorreta");

        return Ok(new { message = "Login bem sucedido", nome = (string)user.nome });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (await authRepository.UsernameExistsAsync(request.Username, ct)) return BadRequest("Usuário já existe");

        var hashed = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await authRepository.CreateUserAsync(request.Username, hashed, request.Nome, ct);
        return Ok("Usuário registrado com sucesso");
    }
}
