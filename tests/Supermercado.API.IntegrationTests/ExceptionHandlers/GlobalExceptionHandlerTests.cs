using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Supermercado.API.ExceptionHandlers;
using Supermercado.Application.Exceptions;
using Supermercado.Domain.Exceptions;

namespace Supermercado.API.IntegrationTests.ExceptionHandlers;

public class GlobalExceptionHandlerTests
{
    private sealed class LoggerEmMemoria : ILogger<GlobalExceptionHandler>
    {
        public List<(LogLevel Nivel, Exception? Excecao)> Registros { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Registros.Add((logLevel, exception));
    }

    private sealed record Resultado(
        bool Tratou, int Status, string ContentType, string CorpoTexto, JsonElement Corpo, LoggerEmMemoria Log);

    private static async Task<Resultado> ExecutarAsync(Exception excecao, bool requisicaoAbortada = false)
    {
        using var servicos = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var log = new LoggerEmMemoria();
        var handler = new GlobalExceptionHandler(servicos.GetRequiredService<IProblemDetailsService>(), log);

        var contexto = new DefaultHttpContext { RequestServices = servicos };
        contexto.Response.Body = new MemoryStream();
        if (requisicaoAbortada)
            contexto.RequestAborted = new CancellationToken(canceled: true);

        var tratou = await handler.TryHandleAsync(contexto, excecao, CancellationToken.None);

        contexto.Response.Body.Position = 0;
        var texto = await new StreamReader(contexto.Response.Body).ReadToEndAsync();
        var corpo = texto.Length > 0 ? JsonDocument.Parse(texto).RootElement.Clone() : default;

        return new Resultado(tratou, contexto.Response.StatusCode, contexto.Response.ContentType ?? "", texto, corpo, log);
    }

    public static TheoryData<Exception, int> Mapeamentos => new()
    {
        { new ValidationException([new ValidationFailure("Nome", "obrigatório")]), 400 },
        { new RecursoNaoEncontradoException("Produto", Guid.NewGuid()), 404 },
        { new EstoqueInsuficienteException(1m, 2m), 409 },
        { new ConflitoDeConcorrenciaException(), 409 },
        { new DomainException("regra violada"), 422 },
        { new BadHttpRequestException("corpo inválido"), 400 },
        { new InvalidOperationException("falha qualquer"), 500 },
    };

    [Theory]
    [MemberData(nameof(Mapeamentos))]
    public async Task Deve_mapear_a_excecao_para_o_status_http_em_ProblemDetails(Exception excecao, int statusEsperado)
    {
        var resultado = await ExecutarAsync(excecao);

        resultado.Tratou.Should().BeTrue();
        resultado.Status.Should().Be(statusEsperado);
        resultado.ContentType.Should().StartWith("application/problem+json");
        resultado.Corpo.GetProperty("status").GetInt32().Should().Be(statusEsperado);
        resultado.Corpo.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        resultado.Corpo.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ValidationException_deve_listar_os_erros_por_campo_em_camelCase()
    {
        var excecao = new ValidationException(
        [
            new ValidationFailure("PrecoCusto", "não pode ser negativo"),
            new ValidationFailure("PrecoCusto", "deve ser um número"),
            new ValidationFailure("Nome", "é obrigatório"),
        ]);

        var resultado = await ExecutarAsync(excecao);

        var erros = resultado.Corpo.GetProperty("errors");
        erros.GetProperty("precoCusto").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("não pode ser negativo", "deve ser um número");
        erros.GetProperty("nome").EnumerateArray().Select(e => e.GetString()).Should().Equal("é obrigatório");
    }

    [Fact]
    public async Task Erros_de_negocio_devem_repassar_a_mensagem_no_detail()
    {
        (await ExecutarAsync(new DomainException("O preço de venda não pode ser menor que o preço de custo.")))
            .Corpo.GetProperty("detail").GetString()
            .Should().Be("O preço de venda não pode ser menor que o preço de custo.");

        (await ExecutarAsync(new EstoqueInsuficienteException(3m, 4m)))
            .Corpo.GetProperty("detail").GetString().Should().Contain("3").And.Contain("4");
    }

    [Fact]
    public async Task Erro_inesperado_deve_retornar_500_sem_expor_detalhes_internos_e_logar_a_excecao()
    {
        var excecao = new InvalidOperationException("Server=SEGREDO;Password=123 falhou");

        var resultado = await ExecutarAsync(excecao);

        resultado.Status.Should().Be(500);
        resultado.CorpoTexto.Should().NotContain("SEGREDO")
            .And.NotContain("Password")
            .And.NotContain(nameof(InvalidOperationException))
            .And.NotContain("StackTrace", because: "nenhum rastreamento de pilha deve vazar");
        resultado.Log.Registros.Should().ContainSingle(r => r.Nivel == LogLevel.Error && ReferenceEquals(r.Excecao, excecao));
    }

    [Fact]
    public async Task Erros_esperados_nao_devem_ser_logados_como_erro()
    {
        var resultado = await ExecutarAsync(new DomainException("regra violada"));

        resultado.Log.Registros.Should().NotContain(r => r.Nivel >= LogLevel.Error);
    }

    [Fact]
    public async Task Requisicao_abandonada_pelo_cliente_nao_deve_gerar_500_nem_log_de_erro()
    {
        var resultado = await ExecutarAsync(new OperationCanceledException(), requisicaoAbortada: true);

        resultado.Tratou.Should().BeTrue();
        resultado.Status.Should().Be(499);
        resultado.CorpoTexto.Should().BeEmpty();
        resultado.Log.Registros.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancelamento_sem_abortar_a_requisicao_continua_sendo_erro_inesperado()
    {
        var resultado = await ExecutarAsync(new OperationCanceledException("timeout interno"));

        resultado.Status.Should().Be(500);
    }
}
