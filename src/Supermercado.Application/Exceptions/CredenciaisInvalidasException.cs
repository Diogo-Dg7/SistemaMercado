namespace Supermercado.Application.Exceptions;

/// <summary>
/// Falha de login. É SEMPRE a mesma exceção e a mesma mensagem, qualquer que seja o motivo
/// (e-mail inexistente, senha errada ou usuário inativo), para não revelar quais e-mails existem.
/// Será traduzida para HTTP 401.
/// </summary>
public class CredenciaisInvalidasException : Exception
{
    public const string MensagemPadrao = "E-mail ou senha inválidos";

    public CredenciaisInvalidasException() : base(MensagemPadrao)
    {
    }
}
