using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Repositorios;
using Npgsql;

namespace MenuRestaurante.Api.Servicos;

public class EstoqueServico(EstoqueRepositorio estoque)
{
    /// <summary>
    /// Lista para a tela de estoque (RF-30). Saldo e custo médio vêm do livro; o resto é
    /// aritmética, e por isso sai de <see cref="CalculadoraEstoque"/>, não de SQL.
    /// </summary>
    public async Task<IEnumerable<InsumoComSaldoResposta>> Listar(bool incluirInativos = false)
    {
        var insumos = await estoque.ListarComSaldo(incluirInativos);
        foreach (var insumo in insumos)
        {
            insumo.ValorImobilizado = CalculadoraEstoque.ValorImobilizado(insumo.Saldo, insumo.CustoMedio);
            // Só sinaliza. Falta de estoque nunca impede venda (AD-06).
            insumo.AbaixoDoMinimo = insumo.EstoqueMinimo > 0 && insumo.Saldo < insumo.EstoqueMinimo;
            insumo.QuantidadeSugerida =
                CalculadoraEstoque.QuantidadeSugerida(insumo.Saldo, insumo.EstoqueMinimo);
        }
        return insumos;
    }

    public async Task<Insumo> Criar(InsumoRequisicao requisicao)
    {
        var insumo = Validar(requisicao);
        int id;
        try
        {
            id = await estoque.InserirInsumo(insumo);
        }
        catch (PostgresException excecao) when (excecao.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new RegraDeNegocioException($"Já existe um insumo chamado '{insumo.Nome}'.");
        }

        // Relê para devolver o que o banco preencheu — senão a resposta sai com data zerada.
        return await BuscarOuFalhar(id);
    }

    public async Task<Insumo> Atualizar(int id, InsumoRequisicao requisicao)
    {
        _ = await BuscarOuFalhar(id);
        var insumo = Validar(requisicao);
        insumo.Id = id;
        try
        {
            await estoque.AtualizarInsumo(insumo);
        }
        catch (PostgresException excecao) when (excecao.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new RegraDeNegocioException($"Já existe um insumo chamado '{insumo.Nome}'.");
        }
        return await BuscarOuFalhar(id);
    }

    /// <summary>
    /// Insumo que já se moveu não é apagado — ele explica os lançamentos antigos. Some do
    /// cadastro por exclusão lógica. Mesmo critério que produto já usa.
    /// </summary>
    public async Task Excluir(int id)
    {
        _ = await BuscarOuFalhar(id);

        if (await estoque.InsumoVinculadoAProduto(id))
            throw new RegraDeNegocioException(
                "Este insumo está vinculado a um produto do cardápio. Desfaça o vínculo primeiro.");

        if (await estoque.InsumoPossuiMovimento(id))
            await estoque.DesativarInsumo(id);
        else
            await estoque.ExcluirInsumo(id);
    }

    /// <summary>
    /// Ajuste, perda e devolução: os lançamentos que uma pessoa cria à mão. Todos exigem
    /// motivo (RF-26) e entram ao custo médio vigente, para que o relatório de perdas some
    /// dinheiro e não só quantidade.
    /// </summary>
    public async Task<MovimentoEstoque> LancarManual(LancamentoManualRequisicao requisicao, int? usuarioId)
    {
        var tipo = requisicao.Tipo.Trim().ToUpperInvariant();
        if (!TipoMovimento.Manuais.Contains(tipo))
            throw new RegraDeNegocioException(
                "Tipo de lançamento inválido. Use: AJUSTE, PERDA ou DEVOLUCAO.");

        if (string.IsNullOrWhiteSpace(requisicao.Motivo))
            throw new RegraDeNegocioException("Informe o motivo do lançamento.");

        var insumo = await BuscarOuFalhar(requisicao.InsumoId);

        // Perda e devolução saem do estoque: a tela pede um número positivo e o sinal é
        // decidido aqui, para o operador não precisar digitar menos.
        var quantidade = tipo switch
        {
            TipoMovimento.Perda or TipoMovimento.Devolucao when requisicao.Quantidade <= 0 =>
                throw new RegraDeNegocioException("Informe uma quantidade maior que zero."),
            TipoMovimento.Perda or TipoMovimento.Devolucao => -requisicao.Quantidade,
            _ when requisicao.Quantidade == 0 =>
                throw new RegraDeNegocioException(
                    "Ajuste precisa de quantidade diferente de zero. Use negativo para tirar do estoque."),
            _ => requisicao.Quantidade
        };

        var movimento = new MovimentoEstoque
        {
            InsumoId = insumo.Id,
            Tipo = tipo,
            Quantidade = quantidade,
            CustoUnitario = await estoque.CustoMedioVigente(insumo.Id),
            UsuarioId = usuarioId,
            Motivo = requisicao.Motivo.Trim()
        };

        var id = await estoque.InserirMovimento(movimento);
        return await estoque.BuscarMovimento(id);
    }

    public async Task<IEnumerable<MovimentoEstoque>> Extrato(int insumoId, int limite)
    {
        _ = await BuscarOuFalhar(insumoId);
        return await estoque.ListarMovimentos(insumoId, Math.Clamp(limite, 1, 500));
    }

    private async Task<Insumo> BuscarOuFalhar(int id) =>
        await estoque.BuscarInsumo(id)
        ?? throw new RegraDeNegocioException("Insumo não encontrado.");

    private static Insumo Validar(InsumoRequisicao requisicao)
    {
        var nome = requisicao.Nome.Trim();
        if (nome.Length < 2)
            throw new RegraDeNegocioException("O nome do insumo precisa ter pelo menos 2 caracteres.");

        var unidade = requisicao.Unidade.Trim().ToUpperInvariant();
        if (!UnidadeInsumo.Validas.Contains(unidade))
            throw new RegraDeNegocioException("Unidade inválida. Use: KG, G, L, ML ou UN.");

        var tipo = requisicao.Tipo.Trim().ToUpperInvariant();
        if (!TipoInsumo.Validos.Contains(tipo))
            throw new RegraDeNegocioException("Tipo inválido. Use: REVENDA ou MATERIA_PRIMA.");

        if (requisicao.EstoqueMinimo < 0)
            throw new RegraDeNegocioException("O estoque mínimo não pode ser negativo.");

        return new Insumo
        {
            Nome = nome,
            Unidade = unidade,
            Tipo = tipo,
            Categoria = string.IsNullOrWhiteSpace(requisicao.Categoria) ? "Geral" : requisicao.Categoria.Trim(),
            EstoqueMinimo = requisicao.EstoqueMinimo
        };
    }
}
