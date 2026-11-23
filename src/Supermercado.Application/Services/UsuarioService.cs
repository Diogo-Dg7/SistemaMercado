using FluentValidation;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Mappings;
using Supermercado.Domain.Entities;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.Interfaces;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CriarUsuarioRequest> _criarValidator;

    public UsuarioService(
        IUsuarioRepository usuarios,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IValidator<CriarUsuarioRequest> criarValidator)
    {
        _usuarios = usuarios;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _criarValidator = criarValidator;
    }

    public async Task<UsuarioDto> CriarAsync(CriarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        await _criarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = new Email(request.Email);

        if (await _usuarios.ObterPorEmailAsync(email, cancellationToken) is not null)
            throw new DomainException("Já existe um usuário com este e-mail.");

        // Só o hash é guardado; a senha em texto puro não sai deste método.
        var usuario = new Usuario(request.Nome, email, _passwordHasher.Gerar(request.Senha), request.Perfil);

        await _usuarios.AdicionarAsync(usuario, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return usuario.ToDto();
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await _usuarios.ListarAsync(cancellationToken);

        return usuarios.Select(u => u.ToDto()).ToList();
    }
}
