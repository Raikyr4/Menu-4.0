using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MenuRestaurante.Api.Modelos;
using Microsoft.IdentityModel.Tokens;

namespace MenuRestaurante.Api.Servicos;

/// <summary>Gera tokens JWT para a sessão do usuário.</summary>
public class TokenServico(IConfiguration configuracao)
{
    public (string Token, DateTimeOffset ExpiraEm) GerarToken(Usuario usuario)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracao["Jwt:Chave"]!));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        var expiraEm = DateTimeOffset.UtcNow.AddHours(configuracao.GetValue<double>("Jwt:ExpiracaoHoras", 12));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.NomeUsuario),
            new Claim("nome", usuario.Nome)
        };

        var token = new JwtSecurityToken(
            issuer: configuracao["Jwt:Emissor"],
            audience: configuracao["Jwt:Publico"],
            claims: claims,
            expires: expiraEm.UtcDateTime,
            signingCredentials: credenciais);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
