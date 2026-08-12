using Dapper;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Servicos;
using Npgsql;

namespace MenuRestaurante.Testes.Integracao;

/// <summary>
/// Prova o que os critérios de aceite de E1-04 e E1-05 pedem, contra Postgres de verdade:
/// migração roda em banco vazio, roda duas vezes sem quebrar, roda num banco que já existia
/// antes do versionamento, e deixa os índices dos relatórios de pé.
/// </summary>
public class MigracoesTestes
{
    private static readonly string[] TabelasEsperadas =
    [
        "usuario", "categoria", "produto", "produto_opcao_grupo", "produto_opcao",
        "mesa", "comanda", "comanda_item", "comanda_item_opcao", "pagamento", "comanda_ajuste"
    ];

    [FatoDeBanco]
    public async Task Banco_vazio_recebe_o_esquema_completo()
    {
        await using var banco = await BancoDeTeste.Criar("migracao_vazio");
        banco.AplicarMigracoes();

        foreach (var tabela in TabelasEsperadas)
        {
            var existe = await banco.Escalar<bool>(
                "SELECT EXISTS (SELECT 1 FROM information_schema.tables " +
                "WHERE table_schema = 'public' AND table_name = @tabela)",
                new { tabela });
            Assert.True(existe, $"A migração não criou a tabela '{tabela}'.");
        }

        // Conferido contra os scripts embarcados, não contra um número fixo: migração nova
        // não deve exigir editar este teste.
        var embarcados = typeof(MigradorBanco).Assembly.GetManifestResourceNames()
            .Count(nome => nome.EndsWith(".sql", StringComparison.OrdinalIgnoreCase));
        var aplicados = await banco.Escalar<int>("SELECT COUNT(*) FROM schemaversions");
        Assert.Equal(embarcados, aplicados);
    }

    [FatoDeBanco]
    public async Task Aplicar_duas_vezes_seguidas_nao_quebra_e_nao_reaplica()
    {
        await using var banco = await BancoDeTeste.Criar("migracao_duas_vezes");
        banco.AplicarMigracoes();
        var primeiraLeva = await banco.Escalar<int>("SELECT COUNT(*) FROM schemaversions");

        // É este o ponto do DbUp: a segunda passada é inócua, sem depender de IF NOT EXISTS.
        banco.AplicarMigracoes();
        var segundaLeva = await banco.Escalar<int>("SELECT COUNT(*) FROM schemaversions");

        Assert.Equal(primeiraLeva, segundaLeva);
    }

    [FatoDeBanco]
    public async Task Banco_anterior_ao_versionamento_e_atualizado_sem_perder_dado()
    {
        await using var banco = await BancoDeTeste.Criar("migracao_legado");

        // Estado do banco do restaurante antes das migrações existirem: quantidade inteira,
        // sem coluna de unidade, sem exclusão lógica de mesa e sem a tabela de ajustes.
        await banco.Executar(EsquemaAnteriorAoVersionamento);
        await banco.Executar(
            @"INSERT INTO categoria (id, nome) VALUES (1, 'Esfihas');
              INSERT INTO produto (id, nome, categoria_id, preco) VALUES (1, 'Esfiha de carne', 1, 5.00);
              INSERT INTO mesa (numero) VALUES (1);
              INSERT INTO comanda (id, tipo, mesa_numero) VALUES (1, 'MESA', 1);
              INSERT INTO comanda_item (comanda_id, produto_id, quantidade, preco_unitario)
              VALUES (1, 1, 3, 5.00);");

        banco.AplicarMigracoes();

        // O dado continua lá e ganhou o valor padrão da coluna nova.
        await using var conexao = banco.Conectar();
        var item = await conexao.QuerySingleAsync<(decimal Quantidade, string Unidade)>(
            "SELECT quantidade, unidade FROM comanda_item WHERE comanda_id = 1");
        Assert.Equal(3m, item.Quantidade);
        Assert.Equal("UN", item.Unidade);

        // E o que faltava foi criado.
        Assert.True(await banco.Escalar<bool>(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_name = 'comanda_ajuste')"));
        Assert.True(await banco.Escalar<bool>(
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns " +
            "WHERE table_name = 'mesa' AND column_name = 'ativa')"));

        // Rodar de novo num banco de legado também tem que ser inócuo.
        banco.AplicarMigracoes();
    }

    [FatoDeBanco]
    public async Task Indices_dos_relatorios_existem()
    {
        await using var banco = await BancoDeTeste.Criar("migracao_indices");
        banco.AplicarMigracoes();

        string[] esperados =
        [
            "ix_pagamento_pago_em", "ix_comanda_fechada_em", "ix_comanda_tipo_status",
            "ix_comanda_ajuste_criado", "ix_comanda_item_produto"
        ];

        await using var conexao = banco.Conectar();
        var existentes = (await conexao.QueryAsync<string>(
            "SELECT indexname FROM pg_indexes WHERE schemaname = 'public'")).ToHashSet();

        foreach (var indice in esperados)
            Assert.True(existentes.Contains(indice), $"Índice '{indice}' não foi criado.");
    }

    /// <summary>
    /// E1-03: o corte do dia é o do restaurante, não o do servidor de banco. Com a sessão em
    /// UTC, um pagamento às 22h30 de Brasília ainda pertence ao dia daquele dia — antes,
    /// já contava para o dia seguinte e o caixa "virava" às 21h.
    /// </summary>
    [FatoDeBanco]
    public async Task Pagamento_as_22h_de_Brasilia_pertence_ao_dia_daquele_dia_com_banco_em_UTC()
    {
        await using var banco = await BancoDeTeste.Criar("fuso_do_negocio");
        banco.AplicarMigracoes();

        await using var conexao = banco.Conectar();
        await conexao.OpenAsync();
        await conexao.ExecuteAsync("SET TIME ZONE 'UTC'");

        await conexao.ExecuteAsync(
            @"INSERT INTO categoria (id, nome) VALUES (1, 'Bebidas');
              INSERT INTO produto (id, nome, categoria_id, preco) VALUES (1, 'Refrigerante', 1, 8.00);
              INSERT INTO comanda (id, tipo) VALUES (1, 'BALCAO');
              INSERT INTO pagamento (comanda_id, forma, valor, pago_em)
              VALUES (1, 'DINHEIRO', 8.00, TIMESTAMPTZ '2026-08-11 22:30:00-03');");

        var dia = await conexao.ExecuteScalarAsync<DateOnly>(
            "SELECT (pago_em AT TIME ZONE @fuso)::date FROM pagamento WHERE comanda_id = 1",
            new { fuso = DiaDoNegocio.Fuso });

        Assert.Equal(new DateOnly(2026, 8, 11), dia);

        // O jeito errado, mantido aqui para deixar explícito o que a regra evita.
        var diaSemFuso = await conexao.ExecuteScalarAsync<DateOnly>(
            "SELECT pago_em::date FROM pagamento WHERE comanda_id = 1");
        Assert.Equal(new DateOnly(2026, 8, 12), diaSemFuso);
    }

    private const string EsquemaAnteriorAoVersionamento = """
        CREATE TABLE usuario (
            id SERIAL PRIMARY KEY,
            nome_usuario VARCHAR(50) NOT NULL UNIQUE,
            senha_hash VARCHAR(300) NOT NULL,
            nome VARCHAR(100) NOT NULL,
            criado_em TIMESTAMPTZ NOT NULL DEFAULT now()
        );
        CREATE TABLE categoria (
            id SERIAL PRIMARY KEY,
            nome VARCHAR(255) NOT NULL
        );
        CREATE TABLE produto (
            id SERIAL PRIMARY KEY,
            nome VARCHAR(255) NOT NULL,
            categoria_id INT NOT NULL REFERENCES categoria(id),
            especial BOOLEAN NOT NULL DEFAULT FALSE,
            preco NUMERIC(10,2) NOT NULL,
            valor_kg NUMERIC(10,2) NOT NULL DEFAULT 0,
            ativo BOOLEAN NOT NULL DEFAULT TRUE
        );
        CREATE TABLE produto_opcao_grupo (
            id SERIAL PRIMARY KEY,
            produto_id INT NOT NULL REFERENCES produto(id) ON DELETE CASCADE,
            nome VARCHAR(100) NOT NULL,
            obrigatorio BOOLEAN NOT NULL DEFAULT FALSE,
            selecao_multipla BOOLEAN NOT NULL DEFAULT FALSE,
            ordem INT NOT NULL DEFAULT 0
        );
        CREATE TABLE produto_opcao (
            id SERIAL PRIMARY KEY,
            grupo_id INT NOT NULL REFERENCES produto_opcao_grupo(id) ON DELETE CASCADE,
            nome VARCHAR(150) NOT NULL,
            preco_adicional NUMERIC(10,2) NOT NULL DEFAULT 0 CHECK (preco_adicional >= 0),
            ordem INT NOT NULL DEFAULT 0,
            ativo BOOLEAN NOT NULL DEFAULT TRUE
        );
        CREATE TABLE mesa (
            numero INT PRIMARY KEY
        );
        CREATE TABLE comanda (
            id SERIAL PRIMARY KEY,
            tipo VARCHAR(10) NOT NULL CHECK (tipo IN ('MESA', 'BALCAO')),
            mesa_numero INT REFERENCES mesa(numero),
            status VARCHAR(10) NOT NULL DEFAULT 'ABERTA' CHECK (status IN ('ABERTA', 'FECHADA')),
            taxa_servico_aplicada BOOLEAN NOT NULL DEFAULT TRUE,
            aberta_em TIMESTAMPTZ NOT NULL DEFAULT now(),
            fechada_em TIMESTAMPTZ,
            CONSTRAINT chk_comanda_mesa CHECK (
                (tipo = 'MESA' AND mesa_numero IS NOT NULL) OR
                (tipo = 'BALCAO' AND mesa_numero IS NULL)
            )
        );
        CREATE UNIQUE INDEX ux_comanda_mesa_aberta
            ON comanda (mesa_numero) WHERE status = 'ABERTA' AND tipo = 'MESA';
        CREATE TABLE comanda_item (
            id SERIAL PRIMARY KEY,
            comanda_id INT NOT NULL REFERENCES comanda(id) ON DELETE CASCADE,
            produto_id INT NOT NULL REFERENCES produto(id),
            quantidade INT NOT NULL DEFAULT 1 CHECK (quantidade > 0),
            preco_unitario NUMERIC(10,2) NOT NULL
        );
        CREATE TABLE comanda_item_opcao (
            id SERIAL PRIMARY KEY,
            comanda_item_id INT NOT NULL REFERENCES comanda_item(id) ON DELETE CASCADE,
            nome_grupo VARCHAR(100) NOT NULL,
            nome_opcao VARCHAR(150) NOT NULL,
            preco_adicional NUMERIC(10,2) NOT NULL DEFAULT 0
        );
        CREATE TABLE pagamento (
            id SERIAL PRIMARY KEY,
            comanda_id INT NOT NULL REFERENCES comanda(id) ON DELETE CASCADE,
            forma VARCHAR(20) NOT NULL CHECK (forma IN ('CREDITO', 'DEBITO', 'DINHEIRO', 'PIX')),
            valor NUMERIC(10,2) NOT NULL CHECK (valor > 0),
            pago_em TIMESTAMPTZ NOT NULL DEFAULT now()
        );
        """;
}
