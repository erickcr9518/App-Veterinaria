using System.Net;
using System.Text.Json;
using VetPlatform.Application.Common.Exceptions;
using ValidationException = VetPlatform.Application.Common.Exceptions.ValidationException;

namespace VetPlatform.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(exception, "Solicitud cancelada por el cliente: {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException =>
                (HttpStatusCode.BadRequest, "Se encontraron errores de validación.", (object?)validationException.Errors),
            NotFoundException notFoundException =>
                (HttpStatusCode.NotFound, notFoundException.Message, null),
            AuthenticationException authenticationException =>
                (HttpStatusCode.Unauthorized, authenticationException.Message, null),
            ForbiddenAccessException forbiddenException =>
                (HttpStatusCode.Forbidden, forbiddenException.Message, null),
            _ => (HttpStatusCode.InternalServerError, "Ocurrió un error inesperado en el servidor.", null),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Error no controlado procesando {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var payload = new
        {
            status = (int)statusCode,
            title,
            errors,
            traceId = context.TraceIdentifier,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
