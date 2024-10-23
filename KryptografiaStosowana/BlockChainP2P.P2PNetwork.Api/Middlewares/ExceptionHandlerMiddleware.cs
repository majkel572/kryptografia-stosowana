using Serilog;
using System.Net;

namespace BlockChainP2P.P2PNetwork.Api.Middlewares;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    // This method applies global try catch block for every endpoint in controllers
    // after injecting it in Program.cs file with
    // app.UseMiddleware<ExceptionHandlerMiddleware>();
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid();

            Log.Error(ex, $"DateTime: {DateTime.Now}, ErrorId:{errorId}, ErrorMessage: {ex.Message}");
            Log.Information(ex, $"DateTime: {DateTime.Now}, ErrorId:{errorId}, ErrorMessage: {ex.Message}");

            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            httpContext.Response.ContentType = "application/json";

            var error = new
            {
                Id = errorId,
                ErrorMessage = "Something went wrong",
                DebugErrorMessage = ex.Message
            };

            await httpContext.Response.WriteAsJsonAsync(error);
        }
    }
}
