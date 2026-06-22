using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Core;

public static class CodeSpiritErrorHandler
{
    public static async Task HandleAsync(HttpContext context, Exception ex, ILogger? logger = null)
    {
        logger?.LogError(ex, "Unhandled exception in CodeSpirit module");

        context.Response.StatusCode = ex switch
        {
            ArgumentException => 400,
            UnauthorizedAccessException => 401,
            InvalidOperationException => 403,
            NotImplementedException => 501,
            _ => 500
        };

        context.Response.ContentType = CodeSpiritDefaults.ContentTypeJson;
        await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, new
        {
            error = ex.GetType().Name,
            message = ex.Message,
            statusCode = context.Response.StatusCode
        });
    }

    public static async Task<T> SafeExecuteAsync<T>(Func<Task<T>> action, T defaultValue, ILogger? logger = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "SafeExecuteAsync caught exception, returning default");
            return defaultValue;
        }
    }

    public static bool IsCritical(Exception ex)
    {
        return ex is OutOfMemoryException or StackOverflowException or AccessViolationException;
    }
}
