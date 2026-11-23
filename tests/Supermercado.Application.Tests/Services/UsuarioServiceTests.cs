using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Services;
using Supermercado.Application.Validators;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Enums;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Application.Tests.Services;

public class UsuarioServiceTests
{
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly UsuarioService _service;

    public UsuarioServiceTests()
    {
        _hasher.Gerar("senha-segura-1").Returns("hash-gerado");
        _usuarios.ObterPorEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((Usuario?)null);
        _service = new UsuarioService(_usuarios, _unitOfWork, _hasher, new CriarUsuarioRequestValidator());
    }

    private static CriarUsuarioRequest RequestValido() =>
        new("Ana Silva", "Ana@Mercado.com", "senha-segura-1", PerfilUsuario.Gerente);

    [Fact]
    public async Task Criar_deve_gravar_somente_o_hash_da_senha_salvar_e_retornar_dto_sem_hash()
    {
        var dto = await _service.CriarAsync(RequestValido());

        dto.Nome.Should().Be("Ana Silva");
        dto.Email.Should().Be("ana@mercado.com");
        dto.Perfil.Should().Be(PerfilUsuario.Gerente);
        dto.Ativo.Should().BeTrue();
        typeof(UsuarioDto).GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Senha"));

        await _usuarios.Received(1).AdicionarAsync(
            Arg.Is<Usuario>(u => u.Id == dto.Id && u.SenhaHash == "hash-gerado" && u.Email == new Email("ana@mercado.com")),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criar_com_email_ja_cadastrado_deve_lancar_DomainException_sem_persistir()
    {
        var existente = new Usuario("Outra", new Email("ana@mercado.com"), "hash", PerfilUsuario.Operador);
        _usuarios.ObterPorEmailAsync(new Email("ana@mercado.com"), Arg.Any<CancellationToken>()).Returns(existente);

        var act = () => _service.CriarAsync(RequestValido());

        await act.Should().ThrowAsync<DomainException>().WithMessage("*e-mail*");
        await _usuarios.DidNotReceiveWithAnyArgs().AdicionarAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("curta")]
    [InlineData("1234567")]
    public async Task Criar_com_senha_curta_deve_lancar_ValidationException_sem_gerar_hash(string senha)
    {
        var act = () => _service.CriarAsync(RequestValido() with { Senha = senha });

        await act.Should().ThrowAsync<ValidationException>();
        _hasher.DidNotReceiveWithAnyArgs().Gerar(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(default);
    }

    [Fact]
    public async Task Criar_com_email_invalido_deve_lancar_ValidationException()
    {
        var act = () => _service.CriarAsync(RequestValido() with { Email = "nao-e-email" });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Listar_deve_retornar_dtos_sem_hash()
    {
        _usuarios.ListarAsync(Arg.Any<CancellationToken>()).Returns(new List<Usuario>
        {
            new("Ana", new Email("ana@mercado.com"), "hash-1", PerfilUsuario.Gerente),
            new("Beto", new Email("beto@mercado.com"), "hash-2", PerfilUsuario.Operador),
        });

        var dtos = await _service.ListarAsync();

        dtos.Select(d => d.Email).Should().Equal("ana@mercado.com", "beto@mercado.com");
        dtos.Select(d => d.Perfil).Should().Equal(PerfilUsuario.Gerente, PerfilUsuario.Operador);
    }
}
