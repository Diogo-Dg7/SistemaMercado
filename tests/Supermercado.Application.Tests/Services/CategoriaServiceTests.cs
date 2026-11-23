using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Application.Exceptions;
using Supermercado.Application.Services;
using Supermercado.Application.Validators;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Interfaces;

namespace Supermercado.Application.Tests.Services;

public class CategoriaServiceTests
{
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CategoriaService _service;

    public CategoriaServiceTests()
    {
        _service = new CategoriaService(_categorias, _unitOfWork, new CriarCategoriaRequestValidator());
    }

    [Fact]
    public async Task Criar_deve_adicionar_salvar_e_retornar_dto()
    {
        var dto = await _service.CriarAsync(new CriarCategoriaRequest("Bebidas"));

        dto.Nome.Should().Be("Bebidas");
        dto.Id.Should().NotBeEmpty();
        await _categorias.Received(1).AdicionarAsync(
            Arg.Is<Categoria>(c => c.Nome == "Bebidas" && c.Id == dto.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criar_com_request_invalido_deve_lancar_ValidationException_sem_persistir()
    {
        var act = () => _service.CriarAsync(new CriarCategoriaRequest(""));

        await act.Should().ThrowAsync<ValidationException>();
        await _categorias.DidNotReceiveWithAnyArgs().AdicionarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(default);
    }

    [Fact]
    public async Task Listar_deve_retornar_dtos()
    {
        _categorias.ListarAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Categoria> { new("Bebidas"), new("Limpeza") });

        var dtos = await _service.ListarAsync();

        dtos.Select(d => d.Nome).Should().Equal("Bebidas", "Limpeza");
    }

    [Fact]
    public async Task ObterPorId_deve_retornar_dto()
    {
        var categoria = new Categoria("Bebidas");
        _categorias.ObterPorIdAsync(categoria.Id, Arg.Any<CancellationToken>()).Returns(categoria);

        var dto = await _service.ObterPorIdAsync(categoria.Id);

        dto.Should().Be(new CategoriaDto(categoria.Id, "Bebidas"));
    }

    [Fact]
    public async Task ObterPorId_inexistente_deve_lancar_RecursoNaoEncontradoException()
    {
        var id = Guid.NewGuid();
        _categorias.ObterPorIdAsync(id, Arg.Any<CancellationToken>()).Returns((Categoria?)null);

        var act = () => _service.ObterPorIdAsync(id);

        (await act.Should().ThrowAsync<RecursoNaoEncontradoException>()).Which.Id.Should().Be(id);
    }
}
