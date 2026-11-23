using Supermercado.Domain.Enums;
using Supermercado.Domain.Exceptions;
using Supermercado.Domain.ValueObjects;

namespace Supermercado.Domain.Entities;

public class Usuario : Entity
{
    public const int NomeTamanhoMaximo = 100;
    public const int SenhaHashTamanhoMaximo = 512;

    /// <param name="senhaHash">Hash da senha, gerado fora do domínio. Nunca a senha em texto puro.</param>
    public Usuario(string nome, Email email, string senhaHash, PerfilUsuario perfil)
    {
        ArgumentNullException.ThrowIfNull(email);
        ValidarPerfil(perfil);

        Nome = ValidarNome(nome);
        Email = email;
        SenhaHash = ValidarSenhaHash(senhaHash);
        Perfil = perfil;
        Ativo = true;
    }

    public string Nome { get; private set; }

    public Email Email { get; private set; }

    public string SenhaHash { get; private set; }

    public PerfilUsuario Perfil { get; private set; }

    public bool Ativo { get; private set; }

    /// <summary>Trocar para o perfil que o usuário já tem não faz nada.</summary>
    public void AlterarPerfil(PerfilUsuario novoPerfil)
    {
        ValidarPerfil(novoPerfil);

        Perfil = novoPerfil;
    }

    public void Desativar()
    {
        if (!Ativo)
            throw new DomainException("O usuário já está inativo.");

        Ativo = false;
    }

    public void Ativar()
    {
        if (Ativo)
            throw new DomainException("O usuário já está ativo.");

        Ativo = true;
    }

    private static void ValidarPerfil(PerfilUsuario perfil)
    {
        if (!Enum.IsDefined(perfil))
            throw new DomainException("O perfil do usuário é inválido.");
    }

    private static string ValidarNome(string? nome)
    {
        var normalizado = nome?.Trim();

        if (string.IsNullOrEmpty(normalizado))
            throw new DomainException("O nome do usuário é obrigatório.");

        if (normalizado.Length > NomeTamanhoMaximo)
            throw new DomainException($"O nome do usuário deve ter no máximo {NomeTamanhoMaximo} caracteres.");

        return normalizado;
    }

    private static string ValidarSenhaHash(string? senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("O hash da senha é obrigatório.");

        if (senhaHash.Length > SenhaHashTamanhoMaximo)
            throw new DomainException("O hash da senha é inválido.");

        return senhaHash;
    }
}
