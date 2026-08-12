using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Repositorios;

namespace MenuRestaurante.Api.Servicos;

public class UsuarioServico(UsuarioRepositorio usuarios, TokenServico tokens)
{
    /// <summary>
    /// Banco sem nenhum usuário é instalação nova: alguém precisa poder criar o primeiro
    /// dono, e não existe token para autorizar isso. Fora esse caso, criar conta exige DONO.
    /// </summary>
    public async Task<bool> PrecisaDoPrimeiroUsuario() => await usuarios.Contar() == 0;

    /// <summary><c>null</c> quando o usuário não existe ou a senha não confere.</summary>
    public async Task<LoginResposta?> Autenticar(LoginRequisicao requisicao)
    {
        // Sem normalizar maiúsculas: as contas que já existem foram gravadas como digitadas,
        // e mudar isso agora tirava o acesso de quem já usa o sistema.
        var usuario = await usuarios.BuscarPorNomeUsuario(requisicao.NomeUsuario.Trim());

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(requisicao.Senha, usuario.SenhaHash))
            return null;

        var (token, expiraEm) = tokens.GerarToken(usuario);
        return new LoginResposta(token, usuario.Nome, usuario.Papel, expiraEm);
    }

    public async Task<IEnumerable<UsuarioResposta>> Listar() =>
        (await usuarios.Listar()).Select(usuario =>
            new UsuarioResposta(usuario.Id, usuario.NomeUsuario, usuario.Nome,
                usuario.Papel, usuario.CriadoEm));

    /// <summary>
    /// Cria conta. <paramref name="criadoPorDono"/> é falso quando ninguém está autenticado —
    /// aí só passa se for a primeira conta do sistema, e ela nasce DONO.
    /// </summary>
    public async Task<UsuarioResposta> Cadastrar(CadastroRequisicao requisicao, bool criadoPorDono)
    {
        var primeiro = await PrecisaDoPrimeiroUsuario();
        if (!criadoPorDono && !primeiro)
            throw new RegraDeNegocioException("Só o dono pode criar contas de acesso.");

        var nomeUsuario = requisicao.NomeUsuario.Trim();
        if (nomeUsuario.Length < 3)
            throw new RegraDeNegocioException("O nome de usuário precisa ter pelo menos 3 caracteres.");

        PoliticaDeSenha.Exigir(requisicao.Senha, nomeUsuario);

        var papel = primeiro ? PapelUsuario.Dono : NormalizarPapel(requisicao.Papel);

        if (await usuarios.BuscarPorNomeUsuario(nomeUsuario) is not null)
            throw new RegraDeNegocioException("Este nome de usuário já está em uso.");

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(requisicao.Senha);
        var id = await usuarios.Inserir(nomeUsuario, senhaHash, requisicao.Nome.Trim(), papel);

        return new UsuarioResposta(id, nomeUsuario, requisicao.Nome.Trim(), papel, DateTimeOffset.Now);
    }

    private static string NormalizarPapel(string? papel)
    {
        // Sem papel informado, a conta nasce operadora: o acesso a faturamento é concedido
        // de propósito, nunca por omissão.
        if (string.IsNullOrWhiteSpace(papel)) return PapelUsuario.Operador;

        var normalizado = papel.Trim().ToUpperInvariant();
        if (!PapelUsuario.Validos.Contains(normalizado))
            throw new RegraDeNegocioException("Papel inválido. Use: DONO ou OPERADOR.");
        return normalizado;
    }
}
