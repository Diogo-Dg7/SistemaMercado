using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Supermercado.Application.Exceptions;
using Supermercado.Domain.Exceptions;

namespace Supermercado.API.ExceptionHandlers;

/// <summary>
/// Traduz exceções em respostas ProblemDetails (RFC 9457):
/// ValidationException → 400 (erros por campo); RecursoNaoEncontradoException → 404;
/// EstoqueInsuficienteException e ConflitoDeConcorrenciaException → 409; DomainException → 422;
/// qualquer outra → 500 sem expor detalhes internos (o erro é registrado no log).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    // Status não padronizado (usado por proxies como nginx) para requisição abandonada pelo cliente.
    private const int ClienteEncerrouARequisicao = 499;

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // O cliente desistiu da requisição: não é erro do sistema, não há a quem responder.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClienteEncerrouARequisicao;
            return true;
        }

        var problema = Mapear(exception);

        if (problema.Status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogInformation("Requisição recusada com {Status} em {Metodo} {Caminho}: {Mensagem}",
                problema.Status, httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        httpContext.Response.StatusCode = problema.Status!.Value;

        var escrito = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception
        });

        // Se nenhum writer aceitou (ex.: cabeçalho Accept incomum), responde mesmo assim em application/problem+json.
        if (!escrito)
            await httpContext.Response.WriteAsJsonAsync(problema, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }

    private static ProblemDetails Mapear(Exception exception) => exception switch
    {
        ValidationException validacao => Criar(
            StatusCodes.Status400BadRequest, "Dados inválidos", "Um ou mais campos são inválidos.", AgruparErros(validacao)),

        RecursoNaoEncontradoException naoEncontrado => Criar(
            StatusCodes.Status404NotFound, "Recurso não encontrado", naoEncontrado.Message),

        // EstoqueInsuficienteException herda de DomainException: precisa vir antes dela.
        EstoqueInsuficienteException estoque => Criar(
            StatusCodes.Status409Conflict, "Estoque insuficiente", estoque.Message),

        ConflitoDeConcorrenciaException conflito => Criar(
            StatusCodes.Status409Conflict, "Conflito de concorrência", conflito.Message),

        DomainException dominio => Criar(
            StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", dominio.Message),

        // Corpo malformado, payload grande demais etc.: erro do cliente, sem repassar o texto interno.
        BadHttpRequestException requisicaoInvalida => Criar(
            requisicaoInvalida.StatusCode, "Requisição inválida", "A requisição não pôde ser processada."),

        _ => Criar(
            StatusCodes.Status500InternalServerError, "Erro interno do servidor",
            "Ocorreu um erro inesperado. Tente novamente mais tarde.")
    };

    private static ProblemDetails Criar(
        int status, string titulo, string detalhe, IDictionary<string, string[]>? erros = null)
    {
        var problema = new ProblemDetails { Status = status, Title = titulo, Detail = detalhe };

        if (erros is not null)
            problema.Extensions["errors"] = erros; // mesmo formato do ValidationProblemDetails do ASP.NET

        return problema;
    }

    // Nomes de campo em camelCase, como no JSON da API: "PrecoCusto" -> "precoCusto".
    private static Dictionary<string, string[]> AgruparErros(ValidationException validacao) =>
        validacao.Errors
            .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
}
