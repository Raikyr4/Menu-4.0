using System.ComponentModel.DataAnnotations;

namespace MenuRestaurante.Api.Dtos;

public record CategoriaRequisicao(
    [Required(ErrorMessage = "Informe o nome da categoria"), MaxLength(255)] string Nome);

public record ProdutoRequisicao(
    [Required(ErrorMessage = "Informe o nome do produto"), MaxLength(255)] string Nome,
    [Range(1, int.MaxValue, ErrorMessage = "Escolha uma categoria")] int CategoriaId,
    [Range(0, 9_999_999, ErrorMessage = "Preço não pode ser negativo")] decimal Preco,
    [Range(0, 9_999_999, ErrorMessage = "Valor por kg não pode ser negativo")] decimal ValorKg,
    bool Especial);
