
using System.Diagnostics;

internal class LogginFilter(ILoggerFactory loggerFactory) : IEndpointFilter
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        Type implementation = context.Arguments[0]!.GetType();
        var logger = _loggerFactory.CreateLogger(implementation.Name);
        logger.LogInformation("Executing {implementation}", implementation.Name);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var result = await next(context);

        logger.LogInformation("Executed {implementation}, time ellapsed in seconds {seconds}", implementation.Name, stopwatch.ElapsedMilliseconds / 1000);
        stopwatch.Stop();
        return result;
    }
}