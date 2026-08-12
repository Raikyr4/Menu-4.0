using System.IdentityModel.Tokens.Jwt;
using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Repositorios;
using MenuRestaurante.Api.Servicos;
using Microsoft.Extensions.Configuration;

namespace MenuRestaurante.Testes.Integracao;

/// <summary>
/// E2-01 e E2-02. O ponto do épico é que o acesso ao faturamento passe a ser concedido, nunca
/// herdado: conta nova nasce operadora e o cadastro deixa de ser porta aberta.
/// </summary>
public class PapelDoUsuarioTestes
{
    [FatoDeBanco]
    public async Task Quem_ja_tinha_conta_antes_do_papel_vira_dono()
    {
        await using var banco = await BancoDeTeste.Criar("papel_legado");

        // Instalação que já rodava antes da migração 003 existir.
        await banco.Executar(
            @"CREATE TABLE usuario (
                  id           SERIAL PRIMARY KEY,
                  nome_usuario VARCHAR(50)  NOT NULL UNIQUE,
                  senha_hash   VARCHAR(300) NOT NULL,
                  nome         VARCHAR(100) NOT NULL,
                  criado_em    TIMESTAMPTZ  NOT NULL DEFAULT now()
              );
              INSERT INTO usuario (nome_usuario, senha_hash, nome)
              VALUES ('dono.antigo', 'hash', 'Dono Antigo');");

        banco.AplicarMigracoes();

        var papel = await banco.Escalar<string>(
            "SELECT papel FROM usuario WHERE nome_usuario = 'dono.antigo'");
        Assert.Equal(PapelUsuario.Dono, papel);
    }

    [FatoDeBanco]
    public async Task Conta_criada_depois_nasce_operadora_por_padrao()
    {
        await using var banco = await BancoDeTeste.Criar("papel_padrao");
        banco.AplicarMigracoes();
        await banco.Executar(
            @"INSERT INTO usuario (nome_usuario, senha_hash, nome)
              VALUES ('atendente', 'hash', 'Atendente')");

        var papel = await banco.Escalar<string>(
            "SELECT papel FROM usuario WHERE nome_usuario = 'atendente'");
        Assert.Equal(PapelUsuario.Operador, papel);
    }

    [FatoDeBanco]
    public async Task Primeiro_usuario_do_sistema_nasce_dono_mesmo_sem_ninguem_autenticado()
    {
        await using var banco = await BancoDeTeste.Criar("primeiro_usuario");
        banco.AplicarMigracoes();
        var servico = CriarServico(banco);

        Assert.True(await servico.PrecisaDoPrimeiroUsuario());

        var criado = await servico.Cadastrar(
            new CadastroRequisicao("dono", "restaurante2026", "Dono"), criadoPorDono: false);

        Assert.Equal(PapelUsuario.Dono, criado.Papel);
        Assert.False(await servico.PrecisaDoPrimeiroUsuario());
    }

    [FatoDeBanco]
    public async Task Depois_do_primeiro_cadastro_anonimo_e_recusado()
    {
        await using var banco = await BancoDeTeste.Criar("cadastro_fechado");
        banco.AplicarMigracoes();
        var servico = CriarServico(banco);
        await servico.Cadastrar(new CadastroRequisicao("dono", "restaurante2026", "Dono"), criadoPorDono: false);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            servico.Cadastrar(new CadastroRequisicao("intruso", "qualquer2026", "Intruso"),
                criadoPorDono: false));

        Assert.Equal("Só o dono pode criar contas de acesso.", excecao.Message);
    }

    [FatoDeBanco]
    public async Task Conta_criada_pelo_dono_sem_papel_informado_nasce_operadora()
    {
        await using var banco = await BancoDeTeste.Criar("cadastro_pelo_dono");
        banco.AplicarMigracoes();
        var servico = CriarServico(banco);
        await servico.Cadastrar(new CadastroRequisicao("dono", "restaurante2026", "Dono"), criadoPorDono: false);

        var atendente = await servico.Cadastrar(
            new CadastroRequisicao("garcom", "salao2026ok", "Garçom"), criadoPorDono: true);

        Assert.Equal(PapelUsuario.Operador, atendente.Papel);
    }

    [FatoDeBanco]
    public async Task Senha_fora_da_politica_e_recusada_no_cadastro()
    {
        await using var banco = await BancoDeTeste.Criar("senha_fraca");
        banco.AplicarMigracoes();
        var servico = CriarServico(banco);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            servico.Cadastrar(new CadastroRequisicao("dono", "1234", "Dono"), criadoPorDono: false));

        Assert.Equal("A senha precisa ter pelo menos 8 caracteres.", excecao.Message);
        Assert.Equal(0, await banco.Escalar<int>("SELECT COUNT(*) FROM usuario"));
    }

    [FatoDeBanco]
    public async Task Login_devolve_o_papel_e_o_token_carrega_a_claim()
    {
        await using var banco = await BancoDeTeste.Criar("token_com_papel");
        banco.AplicarMigracoes();
        var servico = CriarServico(banco);
        await servico.Cadastrar(new CadastroRequisicao("dono", "restaurante2026", "Dono"), criadoPorDono: false);

        var sessao = await servico.Autenticar(new LoginRequisicao("dono", "restaurante2026"));

        Assert.NotNull(sessao);
        Assert.Equal(PapelUsuario.Dono, sessao.Papel);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(sessao.Token);
        var claim = Assert.Single(token.Claims, c => c.Type == PapelUsuario.TipoDeClaim);
        Assert.Equal(PapelUsuario.Dono, claim.Value);
    }

    [FatoDeBanco]
    public async Task Senha_errada_nao_autentica()
    {
        await using var banco = await BancoDeTeste.Criar("senha_errada");
        banco.AplicarMigracoes();
        var servico = CriarServico(banco);
        await servico.Cadastrar(new CadastroRequisicao("dono", "restaurante2026", "Dono"), criadoPorDono: false);

        Assert.Null(await servico.Autenticar(new LoginRequisicao("dono", "restaurante2027")));
    }

    private static UsuarioServico CriarServico(BancoDeTeste banco)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MenuRestaurante"] = banco.ConnectionString,
                ["Jwt:Chave"] = "chave-de-teste-com-mais-de-32-caracteres-ok",
                ["Jwt:Emissor"] = "MenuRestaurante",
                ["Jwt:Publico"] = "MenuRestauranteApp"
            })
            .Build();

        var fabrica = new FabricaConexao(configuracao);
        return new UsuarioServico(new UsuarioRepositorio(fabrica), new TokenServico(configuracao));
    }
}
