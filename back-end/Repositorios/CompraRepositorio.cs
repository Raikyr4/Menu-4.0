using Dapper;
using MenuRestaurante.Api.Modelos;

namespace MenuRestaurante.Api.Repositorios;

public class CompraRepositorio(FabricaConexao fabrica) : RepositorioBase(fabrica)
{
    // ---------- Fornecedor ----------

    public Task<IEnumerable<Fornecedor>> ListarFornecedores() =>
        Executar(null, (conexao, transacao) =>
            conexao.QueryAsync<Fornecedor>(
                "SELECT * FROM fornecedor WHERE ativo ORDER BY nome", transaction: transacao));

    public Task<Fornecedor?> BuscarFornecedor(int id, EscopoTransacao? escopo = null) =>
        Executar(escopo, (conexao, transacao) =>
            conexao.QuerySingleOrDefaultAsync<Fornecedor>(
                "SELECT * FROM fornecedor WHERE id = @id", new { id }, transacao));

    public Task<int> InserirFornecedor(Fornecedor fornecedor) =>
        Executar(null, (conexao, transacao) =>
            conexao.ExecuteScalarAsync<int>(
                @"INSERT INTO fornecedor (nome, documento, telefone)
                  VALUES (@Nome, @Documento, @Telefone)
                  RETURNING id", fornecedor, transacao));

    public Task<int> DesativarFornecedor(int id) =>
        Executar(null, (conexao, transacao) =>
            conexao.ExecuteAsync(
                "UPDATE fornecedor SET ativo = FALSE WHERE id = @id AND ativo",
                new { id }, transacao));

    // ---------- Compra ----------

    public Task<int> InserirCompra(Compra compra, EscopoTransacao escopo) =>
        Executar(escopo, (conexao, transacao) =>
            conexao.ExecuteScalarAsync<int>(
                @"INSERT INTO compra (fornecedor_id, documento, data_compra, usuario_id)
                  VALUES (@FornecedorId, @Documento, @DataCompra, @UsuarioId)
                  RETURNING id", compra, transacao));

    public Task<int> InserirItem(CompraItem item, EscopoTransacao escopo) =>
        Executar(escopo, (conexao, transacao) =>
            conexao.ExecuteScalarAsync<int>(
                @"INSERT INTO compra_item (compra_id, insumo_id, quantidade, custo_unitario)
                  VALUES (@CompraId, @InsumoId, @Quantidade, @CustoUnitario)
                  RETURNING id", item, transacao));

    public Task<IEnumerable<Compra>> ListarCompras(int limite) =>
        Executar(null, (conexao, transacao) =>
            conexao.QueryAsync<Compra>(
                @"SELECT c.*, f.nome AS fornecedor_nome,
                         COALESCE(SUM(ci.quantidade * ci.custo_unitario), 0) AS valor_total
                  FROM compra c
                  JOIN fornecedor f ON f.id = c.fornecedor_id
                  LEFT JOIN compra_item ci ON ci.compra_id = c.id
                  GROUP BY c.id, f.nome
                  ORDER BY c.data_compra DESC, c.id DESC
                  LIMIT @limite",
                new { limite }, transacao));

    public Task<IEnumerable<CompraItem>> ListarItens(int compraId) =>
        Executar(null, (conexao, transacao) =>
            conexao.QueryAsync<CompraItem>(
                @"SELECT ci.*, i.nome AS insumo_nome, i.unidade
                  FROM compra_item ci
                  JOIN insumo i ON i.id = ci.insumo_id
                  WHERE ci.compra_id = @compraId
                  ORDER BY ci.id",
                new { compraId }, transacao));
}
