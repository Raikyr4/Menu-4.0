using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Repositorios;

namespace MenuRestaurante.Api.Servicos;

/// <summary>Exceção de regra de negócio — vira resposta 400/404 amigável no controller.</summary>
public class RegraDeNegocioException(string mensagem) : Exception(mensagem);

public class ComandaServico(
    ComandaRepositorio comandas,
    CatalogoRepositorio catalogo,
    IConfiguration configuracao)
{
    private decimal PercentualTaxa => configuracao.GetValue<decimal>("Negocio:PercentualTaxaServico", 0.10m);

    public Task<IEnumerable<MesaResposta>> ListarMesas() => comandas.ListarMesas(PercentualTaxa);

    public Task<IEnumerable<PedidoBalcaoResposta>> ListarPedidosBalcao() => comandas.ListarPedidosBalcaoDoDia();

    public Task<ResumoFinanceiroResposta> ResumoFinanceiro() => comandas.ResumoFinanceiro();

    /// <summary>Retorna a comanda aberta da mesa; se não existir, abre uma nova.</summary>
    public async Task<ComandaDetalheResposta> AbrirOuObterComandaMesa(int mesaNumero)
    {
        if (!await comandas.MesaExiste(mesaNumero))
            throw new RegraDeNegocioException($"Mesa {mesaNumero} não existe.");

        var comanda = await comandas.BuscarAbertaDaMesa(mesaNumero);
        var id = comanda?.Id ?? await comandas.AbrirComandaMesa(mesaNumero);
        return await MontarDetalhe(id);
    }

    public async Task<ComandaDetalheResposta> AbrirComandaBalcao()
    {
        var id = await comandas.AbrirComandaBalcao();
        return await MontarDetalhe(id);
    }

    public async Task<ComandaDetalheResposta> ObterDetalhe(int comandaId)
    {
        _ = await BuscarOuFalhar(comandaId);
        return await MontarDetalhe(comandaId);
    }

    public async Task<ComandaDetalheResposta> AdicionarItem(int comandaId, NovoItemRequisicao requisicao)
    {
        var comanda = await BuscarAbertaOuFalhar(comandaId);

        if (requisicao.Quantidade < 0.001m || requisicao.Quantidade > 999.999m)
            throw new RegraDeNegocioException("Quantidade deve ser entre 0,001 e 999,999.");

        var produto = await catalogo.BuscarProduto(requisicao.ProdutoId)
            ?? throw new RegraDeNegocioException("Produto não encontrado.");

        var idsSelecionados = (requisicao.OpcaoIds ?? []).Distinct().ToHashSet();
        var idsConhecidos = produto.GruposOpcoes
            .SelectMany(g => g.Opcoes)
            .Select(o => o.Id)
            .ToHashSet();
        if (idsSelecionados.Any(id => !idsConhecidos.Contains(id)))
            throw new RegraDeNegocioException("Uma das opções selecionadas não pertence ao produto.");

        var opcoes = new List<ComandaItemOpcao>();
        foreach (var grupo in produto.GruposOpcoes)
        {
            var selecionadas = grupo.Opcoes.Where(o => idsSelecionados.Contains(o.Id)).ToList();
            if (grupo.Obrigatorio && selecionadas.Count == 0)
                throw new RegraDeNegocioException($"Escolha uma opção em '{grupo.Nome}'.");
            if (!grupo.SelecaoMultipla && selecionadas.Count > 1)
                throw new RegraDeNegocioException($"Escolha apenas uma opção em '{grupo.Nome}'.");

            opcoes.AddRange(selecionadas.Select(opcao => new ComandaItemOpcao
            {
                NomeGrupo = grupo.Nome,
                NomeOpcao = opcao.Nome,
                PrecoAdicional = opcao.PrecoAdicional
            }));
        }

        var vendidoPorPeso = produto.ValorKg > 0;
        if (!vendidoPorPeso && requisicao.Quantidade != decimal.Truncate(requisicao.Quantidade))
            throw new RegraDeNegocioException("Produtos por unidade precisam de quantidade inteira.");

        var precoUnitario = vendidoPorPeso
            ? produto.ValorKg
            : produto.Preco + opcoes.Sum(o => o.PrecoAdicional);
        if (precoUnitario <= 0)
            throw new RegraDeNegocioException("A configuração escolhida precisa ter valor maior que zero.");

        // Preço, unidade e nomes das opções ficam congelados no momento do lançamento.
        await comandas.InserirItem(
            comanda.Id, produto.Id, requisicao.Quantidade,
            vendidoPorPeso ? "KG" : "UN", precoUnitario, opcoes);
        return await MontarDetalhe(comandaId);
    }

    public async Task<ComandaDetalheResposta> RemoverItem(int comandaId, int itemId)
    {
        await BuscarAbertaOuFalhar(comandaId);
        var removidos = await comandas.RemoverItem(comandaId, itemId);
        if (removidos == 0)
            throw new RegraDeNegocioException("Item não encontrado na comanda.");
        return await MontarDetalhe(comandaId);
    }

    public async Task<ComandaDetalheResposta> AdicionarPagamento(int comandaId, NovoPagamentoRequisicao requisicao)
    {
        await BuscarAbertaOuFalhar(comandaId);

        var forma = requisicao.Forma.Trim().ToUpperInvariant();
        if (!FormaPagamento.Validas.Contains(forma))
            throw new RegraDeNegocioException("Forma de pagamento inválida. Use: CREDITO, DEBITO, DINHEIRO ou PIX.");

        var detalhe = await MontarDetalhe(comandaId);
        if (detalhe.Itens.Count == 0)
            throw new RegraDeNegocioException("Não é possível registrar pagamento em comanda sem produtos.");
        if (requisicao.Valor > detalhe.Restante)
            throw new RegraDeNegocioException("Pagamento maior que o valor restante da comanda.");

        await comandas.InserirPagamento(comandaId, forma, requisicao.Valor);
        return await MontarDetalhe(comandaId);
    }

    public async Task<ComandaDetalheResposta> RemoverPagamento(int comandaId, int pagamentoId)
    {
        await BuscarAbertaOuFalhar(comandaId);
        var removidos = await comandas.RemoverPagamento(comandaId, pagamentoId);
        if (removidos == 0)
            throw new RegraDeNegocioException("Pagamento não encontrado na comanda.");
        return await MontarDetalhe(comandaId);
    }

    /// <summary>
    /// Registra desconto ou sangria: a parte do valor devido que não será paga.
    /// Só é permitido em comanda aberta, com produtos, e nunca maior que o restante.
    /// </summary>
    public async Task<ComandaDetalheResposta> AdicionarAjuste(int comandaId, NovoAjusteRequisicao requisicao)
    {
        await BuscarAbertaOuFalhar(comandaId);

        var tipo = requisicao.Tipo.Trim().ToUpperInvariant();
        if (!TipoAjuste.Validos.Contains(tipo))
            throw new RegraDeNegocioException("Tipo de ajuste inválido. Use: DESCONTO ou SANGRIA.");

        var detalhe = await MontarDetalhe(comandaId);
        if (detalhe.Itens.Count == 0)
            throw new RegraDeNegocioException("Não é possível registrar ajuste em comanda sem produtos.");
        if (requisicao.Valor > detalhe.Restante)
            throw new RegraDeNegocioException("Ajuste maior que o valor restante da comanda.");

        await comandas.InserirAjuste(comandaId, tipo, requisicao.Valor);
        return await MontarDetalhe(comandaId);
    }

    public async Task<ComandaDetalheResposta> RemoverAjuste(int comandaId, int ajusteId)
    {
        await BuscarAbertaOuFalhar(comandaId);
        var removidos = await comandas.RemoverAjuste(comandaId, ajusteId);
        if (removidos == 0)
            throw new RegraDeNegocioException("Ajuste não encontrado na comanda.");
        return await MontarDetalhe(comandaId);
    }

    public Task<int> CriarMesa() => comandas.CriarMesa();

    public async Task ExcluirMesa(int numero)
    {
        var resultado = await comandas.ExcluirMesa(numero);
        switch (resultado)
        {
            case ResultadoExclusaoMesa.NaoEncontrada:
                throw new RegraDeNegocioException($"Mesa {numero} não existe.");
            case ResultadoExclusaoMesa.MinimoAtingido:
                throw new RegraDeNegocioException("O salão deve manter no mínimo 2 mesas.");
            case ResultadoExclusaoMesa.ComandaAberta:
                throw new RegraDeNegocioException("Não é possível excluir uma mesa com comanda aberta.");
        }
    }

    public async Task<ComandaDetalheResposta> AlterarTaxaServico(int comandaId, bool aplicada)
    {
        var comanda = await BuscarAbertaOuFalhar(comandaId);
        if (comanda.Tipo == TipoComanda.Balcao && aplicada)
            throw new RegraDeNegocioException("Pedido de balcão não tem taxa de serviço.");

        await comandas.AtualizarTaxaServico(comandaId, aplicada);
        return await MontarDetalhe(comandaId);
    }

    /// <summary>
    /// Fecha a comanda. Regras:
    /// - comanda vazia (sem itens e sem pagamentos) de mesa: fecha direto (libera a mesa);
    /// - com itens: só fecha se o valor pago cobrir o total devido.
    /// </summary>
    public async Task<ComandaDetalheResposta> Fechar(int comandaId)
    {
        await BuscarAbertaOuFalhar(comandaId);
        var detalhe = await MontarDetalhe(comandaId);

        if (detalhe.Itens.Count > 0 && detalhe.Restante > 0)
            throw new RegraDeNegocioException(
                $"Ainda faltam R$ {detalhe.Restante:F2} a pagar para fechar a comanda.");

        if (detalhe.Itens.Count == 0 && detalhe.Pagamentos.Count > 0)
            throw new RegraDeNegocioException("Comanda sem produtos não pode ter pagamentos. Remova os pagamentos.");

        await comandas.Fechar(comandaId);
        return await MontarDetalhe(comandaId);
    }

    /// <summary>Exclui pedido de balcão aberto (engano do operador).</summary>
    public async Task ExcluirPedidoBalcao(int comandaId)
    {
        var comanda = await BuscarOuFalhar(comandaId);
        if (comanda.Tipo != TipoComanda.Balcao)
            throw new RegraDeNegocioException("Só pedidos de balcão podem ser excluídos.");
        if (comanda.Status != StatusComanda.Aberta)
            throw new RegraDeNegocioException("Pedido fechado é registro de faturamento e não pode ser excluído.");

        await comandas.Excluir(comandaId);
    }

    private async Task<Comanda> BuscarOuFalhar(int comandaId) =>
        await comandas.Buscar(comandaId)
        ?? throw new RegraDeNegocioException("Comanda não encontrada.");

    private async Task<Comanda> BuscarAbertaOuFalhar(int comandaId)
    {
        var comanda = await BuscarOuFalhar(comandaId);
        if (comanda.Status != StatusComanda.Aberta)
            throw new RegraDeNegocioException("Comanda já está fechada.");
        return comanda;
    }

    /// <summary>Todos os totais são calculados aqui, no servidor — nunca no navegador.</summary>
    private async Task<ComandaDetalheResposta> MontarDetalhe(int comandaId)
    {
        var comanda = await BuscarOuFalhar(comandaId);
        var itens = (await comandas.ListarItens(comandaId)).ToList();
        var pagamentos = (await comandas.ListarPagamentos(comandaId)).ToList();
        var ajustes = (await comandas.ListarAjustes(comandaId)).ToList();

        var total = itens.Sum(i => i.Quantidade * i.PrecoUnitario);
        var taxa = comanda.TaxaServicoAplicada ? Math.Round(total * PercentualTaxa, 2) : 0m;
        var totalComTaxa = total + taxa;
        var pago = pagamentos.Sum(p => p.Valor);
        var descontos = ajustes.Where(a => a.Tipo == TipoAjuste.Desconto).Sum(a => a.Valor);
        var sangrias = ajustes.Where(a => a.Tipo == TipoAjuste.Sangria).Sum(a => a.Valor);

        return new ComandaDetalheResposta
        {
            Id = comanda.Id,
            Tipo = comanda.Tipo,
            MesaNumero = comanda.MesaNumero,
            Status = comanda.Status,
            TaxaServicoAplicada = comanda.TaxaServicoAplicada,
            AbertaEm = comanda.AbertaEm,
            FechadaEm = comanda.FechadaEm,
            Itens = itens,
            Pagamentos = pagamentos,
            Ajustes = ajustes,
            Total = total,
            TaxaServico = taxa,
            TotalComTaxa = totalComTaxa,
            TotalDevido = totalComTaxa,
            Pago = pago,
            TotalDescontos = descontos,
            TotalSangrias = sangrias,
            // Descontos e sangrias abatem o que falta pagar
            Restante = Math.Max(totalComTaxa - pago - descontos - sangrias, 0)
        };
    }
}
