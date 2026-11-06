using Banking.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.ExceptionHandling;

/// <summary>
/// Único punto donde las excepciones se traducen a HTTP. Los controladores
/// no necesitan ningún try-catch: lanzan y este manejador responde.
/// Las respuestas siguen el estándar RFC 7807 (ProblemDetails).
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            NotFoundException e => Build(StatusCodes.Status404NotFound, "Recurso no encontrado", e.Message, e.Code),
            InsufficientFundsException e => Build(StatusCodes.Status400BadRequest, "Fondos insuficientes", e.Message, e.Code),
            DomainValidationException e => Build(StatusCodes.Status400BadRequest, "Solicitud inválida", e.Message, e.Code),
            ConflictException e => Build(StatusCodes.Status409Conflict, "Conflicto", e.Message, e.Code),
            _ => null
        };

        if (problem is null)
        {
            // Error inesperado: se registra completo en el log, pero al cliente NO se le filtran detalles.
            logger.LogError(exception, "Error no controlado procesando {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            problem = Build(
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                "Ocurrió un error inesperado. Intente nuevamente más tarde.",
                "internal_error");
        }
        else
        {
            logger.LogWarning("Error de negocio ({Code}): {Message}",
                problem.Extensions["code"], exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Build(int status, string title, string detail, string code) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Extensions = { ["code"] = code }
    };
}
