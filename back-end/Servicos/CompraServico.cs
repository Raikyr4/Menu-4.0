using MenuRestaurante.Api.Dtos;
using MenuRestaurante.Api.Modelos;
using MenuRestaurante.Api.Repositorios;
using Npgsql;

namespace MenuRestaurante.Api.Servicos;

public class CompraServico(CompraRepositorio compras, EstoqueRepositorio estoque)
{
    public Task<IEnumerable<Fornecedor>> ListarFornecedores() => compras.ListarFornecedores();

    public async Task<Fornecedor> CriarFornecedor(FornecedorRequisicao requisicao)
    {
        var nome = requisicao.Nome.Trim();
        if (nome.Length < 2)
            throw new RegraDeNegocioException("O nome do fornecedor precisa ter pelo menos 2 caracteres.");

        var fornecedor = new Fornecedor
        {
            Nome = nome,
            Documento = string.IsNullOrWhiteSpace(requisicao.Documento) ? null : requisicao.Documento.Trim(),
            Telefone = string.IsNullOrWhiteSpace(requisicao.Telefone) ? null : requisicao.Telefone.Trim()
        };

        try
        {
            fornecedor.Id = await compras.InserirFornecedor(fornecedor);
        }
        catch (PostgresException excecao) when (excecao.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new RegraDeNegocioException("Já existe um fornecedor com esse nome.");
        }

        return fornecedor;
    }

    public async Task ExcluirFornecedor(int id)
    {
        _ = await compras.BuscarFornecedor(id)
            ?? throw new RegraDeNegocioException("Fornecedor não encontrado.");
        await compras.DesativarFornecedor(id);
    }

    /// <summary>
    /// Registra a entrada de compra (RF-23). A compra e as ENTRADAs que ela gera são uma
    /// transação só: meia compra gravada vira saldo errado sem ninguém perceber.
    ///
    /// Cada item recalcula o custo médio ponderado móvel do seu insumo (RF-24) e congela o
    /// resultado no lançamento. O insumo é travado antes da leitura do saldo — duas compras
    /// simultâneas do mesmo insumo leriam a mesma média e a segunda sairia errada.
    /// </summary>
    public async Task<CompraResposta> Registrar(CompraRequisicao requisicao, int? usuarioId)
    {
        if (requisicao.Itens is null || requisicao.Itens.Count == 0)
            throw new RegraDeNegocioException("Informe pelo menos um item na compra.");

        foreach (var item in requisicao.Itens)
        {
            if (item.Quantidade <= 0)
                throw new RegraDeNegocioException("A quantidade de cada item precisa ser maior que zero.");
            if (item.CustoUnitario < 0)
                throw new RegraDeNegocioException("O custo unitário não pode ser negativo.");
        }

        var fornecedor = await compras.BuscarFornecedor(requisicao.FornecedorId)
            ?? throw new RegraDeNegocioException("Fornecedor não encontrado.");

        await using var escopo = await compras.AbrirTransacao();

        var compra = new Compra
        {
            FornecedorId = fornecedor.Id,
            Documento = string.IsNullOrWhiteSpace(requisicao.Documento) ? null : requisicao.Documento.Trim(),
            DataCompra = requisicao.DataCompra ?? DiaDoNegocio.Hoje(),
            UsuarioId = usuarioId
        };
        compra.Id = await compras.InserirCompra(compra, escopo);

        var itens = new List<CompraItemResposta>();

        foreach (var requisitado in requisicao.Itens)
        {
            var insumo = await estoque.BuscarInsumoParaAtualizar(requisitado.InsumoId, escopo)
                ?? throw new RegraDeNegocioException(
                    $"Insumo {requisitado.InsumoId} não encontrado.");
            if (!insumo.Ativo)
                throw new RegraDeNegocioException($"O insumo '{insumo.Nome}' está inativo.");

            var saldoAnterior = await estoque.Saldo(insumo.Id, escopo);
            var medioAnterior = await estoque.CustoMedioVigente(insumo.Id, escopo);
            var medioNovo = CalculadoraEstoque.CustoMedioPonderado(
                saldoAnterior, medioAnterior, requisitado.Quantidade, requisitado.CustoUnitario);

            await compras.InserirItem(new CompraItem
            {
                CompraId = compra.Id,
                InsumoId = insumo.Id,
                Quantidade = requisitado.Quantidade,
                CustoUnitario = requisitado.CustoUnitario
            }, escopo);

            await estoque.InserirMovimento(new MovimentoEstoque
            {
                InsumoId = insumo.Id,
                Tipo = TipoMovimento.Entrada,
                Quantidade = requisitado.Quantidade,
                CustoUnitario = requisitado.CustoUnitario,
                CustoMedioApos = medioNovo,
                CompraId = compra.Id,
                UsuarioId = usuarioId
            }, escopo);

            itens.Add(new CompraItemResposta(
                insumo.Id, insumo.Nome, insumo.Unidade,
                requisitado.Quantidade, requisitado.CustoUnitario,
                CalculadoraComanda.ArredondarDinheiro(requisitado.Quantidade * requisitado.CustoUnitario)));
        }

        await escopo.Confirmar();

        return new CompraResposta(
            compra.Id, fornecedor.Id, fornecedor.Nome, compra.Documento, compra.DataCompra,
            CalculadoraComanda.ArredondarDinheiro(itens.Sum(i => i.Subtotal)), itens);
    }

    public async Task<IEnumerable<Compra>> Listar(int limite) =>
        await compras.ListarCompras(Math.Clamp(limite, 1, 200));

    public Task<IEnumerable<CompraItem>> ItensDa(int compraId) => compras.ListarItens(compraId);
}
