using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace RestaurantApi.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<ErrorHandlingMiddleware> logger)
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
            catch (Exception ex)
            {
                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

                // Registrar la excepción completa (mensaje, tipo y stack trace) en los logs.
                _logger.LogError(
                    ex,
                    "Error no controlado en {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    traceId
                );

                // Si la respuesta ya comenzó, no es seguro reemplazarla.
                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                // Mantener una respuesta genérica para no exponer detalles internos al cliente.
                await context.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Ocurrió un error interno en el servidor.",
                    traceId
                });
            }
        }
    }
}