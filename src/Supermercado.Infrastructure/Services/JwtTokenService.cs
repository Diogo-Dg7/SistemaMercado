using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Supermercado.Application.DTOs;
using Supermercado.Application.Interfaces;
using Supermercado.Application.Security;
using Supermercado.Domain.Entities;

namespace Supermercado.Infrastructure.Services;

/// <summary>Emite JWT assinado (HS256) com as claims de Id, Nome, E-mail e Perfil (role).</summary>
public class JwtTokenService : ITokenService
{
    /// <summary>Um turno de trabalho.</summary>
    public static readonly TimeSpan Validade = TimeSpan.FromHours(8);

    private readonly IOptions<JwtOptions> _opcoes;
    private readonly TimeProvider _relogio;

    public JwtTokenService(IOptions<JwtOptions> opcoes, TimeProvider relogio)
    {
        _opcoes = opcoes;
        _relogio = relogio;
    }

    public TokenAcesso GerarToken(Usuario usuario)
    {
        var jwt = _opcoes.Value; // já validado (tamanho mínimo da chave) na inicialização da API

        var agora = _relogio.GetUtcNow().UtcDateTime;
        var expiraEm = agora.Add(Validade);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expiraEm,
            Subject = new ClaimsIdentity(
            [
                new Claim(NomesDeClaim.Id, usuario.Id.ToString()),
                new Claim(NomesDeClaim.Nome, usuario.Nome),
                new Claim(NomesDeClaim.Email, usuario.Email.Valor),
                new Claim(NomesDeClaim.Perfil, usuario.Perfil.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave)), SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descritor);

        return new TokenAcesso(token, expiraEm);
    }
}
