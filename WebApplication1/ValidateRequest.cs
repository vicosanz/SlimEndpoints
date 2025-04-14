using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;
using WebApplication1.Endpoints;

namespace WebApplication1;

[SlimEndpointPipeline(2)]
public class ValidateRequest<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : SlimEndpointPipeline<TRequest, TResponse>
{
    public override async Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Count()> 0)
        {
            var result = await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken)));
            var errors = result.SelectMany(r => r.Errors).ToList();
            if (errors.Count > 0)
            {
                throw new ValidationException(errors);
            }
        }
        if (request is UpdateWeatherForecastsRequest req)
        {
            req.Base.Name = "Test";
            return await next(cancellationToken);
        }
        return await next(cancellationToken);
    }
}

[SlimEndpointPipeline(order: 1)]
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
