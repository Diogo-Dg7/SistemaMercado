using Supermercado.Domain.Enums;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Entities;

public class Produto : Entity
{
    public const int NomeTamanhoMaximo = 200;
    public const int CasasDecimaisFracionados = 3;

    public Produto(
        string nome,
        CodigoBarras codigoBarras,
        Dinheiro precoCusto,
        Dinheiro precoVenda,
        Categoria categoria,
        UnidadeMedida unidadeMedida,
        decimal estoqueMinimo = 0,
        decimal estoqueInicial = 0)
    {
        ArgumentNullException.ThrowIfNull(codigoBarras);
        ArgumentNullException.ThrowIfNull(precoCusto);
        ArgumentNullException.ThrowIfNull(precoVenda);
        ArgumentNullException.ThrowIfNull(categoria);

        if (!Enum.IsDefined(unidadeMedida))
            throw new DomainException("A unidade de medida é inválida.");

        if (estoqueMinimo < 0)
            throw new DomainException("O estoque mínimo não pode ser negativo.");

        if (estoqueInicial < 0)
            throw new DomainException("O estoque inicial não pode ser negativo.");

        ValidarPrecisaoDaQuantidade(estoqueMinimo, unidadeMedida);
        ValidarPrecisaoDaQuantidade(estoqueInicial, unidadeMedida);

        // Na criação a regra é sempre estrita; a exceção do Gerente vale só em AtualizarPreco.
        ValidarPrecos(precoCusto, precoVenda, PerfilUsuario.Operador);

        Nome = ValidarNome(nome);
        CodigoBarras = codigoBarras;
        PrecoCusto = precoCusto;
        PrecoVenda = precoVenda;
        Categoria = categoria;
        CategoriaId = categoria.Id;
        UnidadeMedida = unidadeMedida;
        EstoqueMinimo = estoqueMinimo;
        Estoque = estoqueInicial;
    }

    // Exigido pelo EF Core para materializar a entidade; os valores reais vêm do banco.
    // Privado: nenhum código de negócio consegue criar um Produto sem passar pelas invariantes.
    private Produto()
    {
        Nome = null!;
        CodigoBarras = null!;
        PrecoCusto = null!;
        PrecoVenda = null!;
        Categoria = null!;
    }

    public string Nome { get; private set; }

    public CodigoBarras CodigoBarras { get; private set; }

    public Dinheiro PrecoCusto { get; private set; }

    public Dinheiro PrecoVenda { get; private set; }

    public UnidadeMedida UnidadeMedida { get; private set; }

    public decimal Estoque { get; private set; }

    public decimal EstoqueMinimo { get; private set; }

    public Guid CategoriaId { get; private set; }

    public Categoria Categoria { get; private set; }

    /// <summary>
    /// Atualiza custo e venda juntos para que a invariante (venda >= custo) seja checada sobre o par.
    /// Apenas o perfil Gerente pode vender abaixo do custo.
    /// </summary>
    public void AtualizarPreco(Dinheiro novoPrecoCusto, Dinheiro novoPrecoVenda, PerfilUsuario perfil)
    {
        ArgumentNullException.ThrowIfNull(novoPrecoCusto);
        ArgumentNullException.ThrowIfNull(novoPrecoVenda);

        ValidarPrecos(novoPrecoCusto, novoPrecoVenda, perfil);

        PrecoCusto = novoPrecoCusto;
        PrecoVenda = novoPrecoVenda;
    }

    public void AdicionarEstoque(decimal quantidade)
    {
        ValidarQuantidade(quantidade);

        if (quantidade > decimal.MaxValue - Estoque)
            throw new DomainException("A quantidade informada excede a capacidade de estoque do produto.");

        Estoque += quantidade;
    }

    public void BaixarEstoque(decimal quantidade)
    {
        ValidarQuantidade(quantidade);

        if (quantidade > Estoque)
            throw new EstoqueInsuficienteException(Estoque, quantidade);

        Estoque -= quantidade;
    }

    public bool EstaAbaixoDoEstoqueMinimo() => Estoque < EstoqueMinimo;

    private static void ValidarPrecos(Dinheiro precoCusto, Dinheiro precoVenda, PerfilUsuario perfil)
    {
        if (precoVenda < precoCusto && perfil != PerfilUsuario.Gerente)
            throw new DomainException("O preço de venda não pode ser menor que o preço de custo.");
    }

    private void ValidarQuantidade(decimal quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");

        ValidarPrecisaoDaQuantidade(quantidade, UnidadeMedida);
    }

    // Unidade: só inteiros. Quilograma e Litro: até 3 casas decimais.
    private static void ValidarPrecisaoDaQuantidade(decimal quantidade, UnidadeMedida unidadeMedida)
    {
        if (unidadeMedida == UnidadeMedida.Unidade)
        {
            if (decimal.Truncate(quantidade) != quantidade)
                throw new DomainException("Produtos vendidos por unidade só aceitam quantidades inteiras.");

            return;
        }

        if (decimal.Round(quantidade, CasasDecimaisFracionados) != quantidade)
            throw new DomainException($"A quantidade aceita no máximo {CasasDecimaisFracionados} casas decimais.");
    }

    private static string ValidarNome(string? nome)
    {
        var normalizado = nome?.Trim();

        if (string.IsNullOrEmpty(normalizado))
            throw new DomainException("O nome do produto é obrigatório.");

        if (normalizado.Length > NomeTamanhoMaximo)
            throw new DomainException($"O nome do produto deve ter no máximo {NomeTamanhoMaximo} caracteres.");

        return normalizado;
    }
}
