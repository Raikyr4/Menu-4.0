using System.ComponentModel.DataAnnotations;

namespace MenuRestaurante.Api.Dtos;

public record CategoriaRequisicao(
    [Required(ErrorMessage = "Informe o nome da categoria"), MaxLength(255)] string Nome);

public record ProdutoRequisicao(
    [Required(ErrorMessage = "Informe o nome do produto"), MaxLength(255)] string Nome,
    [Range(1, int.MaxValue, ErrorMessage = "Escolha uma categoria")] int CategoriaId,
    [Range(0, 9_999_999, ErrorMessage = "Preço não pode ser negativo")] decimal Preco,
    [Range(0, 9_999_999, ErrorMessage = "Valor por kg não pode ser negativo")] decimal ValorKg,
    bool Especial,
    List<ProdutoGrupoOpcaoRequisicao> GruposOpcoes);

public record ProdutoGrupoOpcaoRequisicao(
    [Required(ErrorMessage = "Informe o nome do grupo"), MaxLength(100)] string Nome,
    bool Obrigatorio,
    bool SelecaoMultipla,
    List<ProdutoOpcaoRequisicao> Opcoes);

public record ProdutoOpcaoRequisicao(
    [Required(ErrorMessage = "Informe o nome da opção"), MaxLength(150)] string Nome,
    [Range(0, 9_999_999, ErrorMessage = "Valor da opção não pode ser negativo")] decimal PrecoAdicional);
