namespace Menu.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        const string headerName = "X-Correlation-Id";
        var correlationId = context.Request.Headers[headerName].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Response.Headers[headerName] = correlationId;
        context.Items[headerName] = correlationId;
        await next(context);
    }
}
