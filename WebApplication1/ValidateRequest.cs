using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;
using System;
using WebApplication1.Endpoints;
using WebApplication1.Endpoints.Products;
using WebApplication1.Endpoints.Products.GetById;
using WebApplication1.Endpoints.Products.GetProducts;

namespace WebApplication1;

[SlimEndpointPipeline(2)]
public class ValidateRequest<TSlimEndpoint, TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : SlimEndpointPipeline<TSlimEndpoint, TRequest, TResponse>
{
    public override async Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var result = await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken)));
            var errors = result.SelectMany(r => r.Errors);
            if (errors.Any())
            {
                if (typeof(TResponse).IsAssignableTo(typeof(IResult)))
                {
                    return (TResponse)errors.ToValidationProblem();
                }
                throw new ValidationException(errors);
            }
        }
        return await next(cancellationToken);
    }
}

[SlimEndpointPipeline(order: 1)]
public class LogRequest<TSlimEndpoint, TRequest, TResponse>(ILogger<LogRequest<TSlimEndpoint, TRequest, TResponse>> logger) 
    : SlimEndpointPipeline<TSlimEndpoint, TRequest, TResponse>
{
    public override async Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request: {Request}", request);
        var response = await next(cancellationToken);
        logger.LogInformation("Response: {Response}", response);
        return response;
    }
}

//public class LogRequest_GetProductsHandler(ILogger<LogRequest_GetProductsHandler> logger)
//: LogRequest<GetProductsHandler, Unit, Product[]>(logger)
//{
//}
