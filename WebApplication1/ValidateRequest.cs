using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;
using WebApplication1.Endpoints;

namespace WebApplication1;

[SlimEndpointPipeline(1)]
public class ValidateRequest<TRequest, TResponse> : SlimEndpointPipeline<TRequest, TResponse>
{
    public override async Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is UpdateWeatherForecastsRequest req)
        {
            req.Base.Name = "Test";
            return await next(cancellationToken);
        }
        return await next(cancellationToken);
    }
}

[SlimEndpointPipeline(order: 2)]
public class LogRequest<TRequest, TResponse>(ILogger<LogRequest<TRequest, TResponse>> logger) : SlimEndpointPipeline<TRequest, TResponse>
{
    public override async Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request: {Request}", request);
        var response = await next(cancellationToken);
        logger.LogInformation("Response: {Response}", response);
        return response;
    }
}

//public class LogRequest_GetStream1(Microsoft.Extensions.Logging.ILogger<WebApplication1.LogRequest<TRequest, TResponse>> logger) : 
//    LogRequest<global::WebApplication1.Endpoints.GetStreamRequest, SlimEndpoints.AOT.Unit>(logger);
