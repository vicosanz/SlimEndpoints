using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SlimEndpoints.AOT;

public interface ISlimEndpoint
{
    void Configure(RouteHandlerBuilder builder);
}

public interface ISlimEndpoint<in TRequest, TResponse> : ISlimEndpoint 
{
    Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, CancellationToken cancellationToken);
    IResult Validate(TRequest request);
}

public interface ISlimEndpointWithoutRequest<TResponse> : ISlimEndpoint<Unit, TResponse>;

public interface ISlimEndpointWithoutResponse<TRequest> : ISlimEndpoint<TRequest, Unit> ;

public abstract class SlimEndpoint<TRequest, TResponse> : ISlimEndpoint<TRequest, TResponse> 
{
    public virtual void Configure(RouteHandlerBuilder builder) { }

    public abstract Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, CancellationToken cancellationToken);

    public virtual IResult Validate(TRequest request) => Results.Ok();
}

public abstract class SlimEndpointWithoutRequest<TResponse> : ISlimEndpointWithoutRequest<TResponse>
{
    public virtual void Configure(RouteHandlerBuilder builder) { }

    public Task<TResponse> HandleAsync(HttpContext httpContext, Unit request, CancellationToken cancellationToken) => HandleAsync(httpContext, cancellationToken);
    public abstract Task<TResponse> HandleAsync(HttpContext httpContext, CancellationToken cancellationToken);

    public virtual IResult Validate(Unit request) => Results.Ok();
}

public abstract class SlimEndpointWithoutResponse<TRequest> : ISlimEndpointWithoutResponse<TRequest> 
{
    public virtual void Configure(RouteHandlerBuilder builder) { }
    public abstract Task HandleAsync(HttpContext httpContext, TRequest request, CancellationToken cancellationToken);

    public virtual IResult Validate(TRequest request) => Results.Ok();

    async Task<Unit> ISlimEndpoint<TRequest, Unit>.HandleAsync(HttpContext httpContext, TRequest request, CancellationToken cancellationToken)
    {
        await HandleAsync(httpContext, request, cancellationToken);
        return new Unit();
    }
}




public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

public interface ISlimEndpointPipeline<in ISlimEndpoint>;

public abstract class SlimEndpointPipeline<TSlimEndpoint, TRequest, TResponse> : ISlimEndpointPipeline<ISlimEndpoint<TRequest, TResponse>> 
{
    public abstract Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}




public interface ISlimEndpointImplementation
{
    void UseSlimEndpoint(IEndpointRouteBuilder app);
}

public interface ISlimEndpointImplementation<TSlimEndpoint, TRequest, TResponse> : ISlimEndpointImplementation where TSlimEndpoint : ISlimEndpoint<TRequest, TResponse>
{
    TRequest ParseRequestFromFilterContext(EndpointFilterInvocationContext context);
    IResult ValidateRequest(TRequest request);
    IResult ValidateFromFilterContext(EndpointFilterInvocationContext context);
}

public abstract class SlimEndpointImplementation<TSlimEndpoint, TRequest, TResponse>(TSlimEndpoint slimEndpoint) :
    ISlimEndpointImplementation<TSlimEndpoint, TRequest, TResponse> where TSlimEndpoint: ISlimEndpoint<TRequest, TResponse>
{
    protected readonly TSlimEndpoint slimEndpoint = slimEndpoint;

    public abstract void UseSlimEndpoint(IEndpointRouteBuilder app);

    public async Task<TResponse> HandleAsync(HttpContext httpContext,         
        TRequest request,
        IEnumerable<SlimEndpointPipeline<TSlimEndpoint, TRequest, TResponse>> pipelines, 
        CancellationToken cancellationToken)
    {
        Task<TResponse> Handler(CancellationToken cancellationToken = default) => slimEndpoint.HandleAsync(httpContext, request, cancellationToken);
        return await pipelines.Reverse()
            .Aggregate
            (
                (RequestHandlerDelegate<TResponse>)Handler, 
                (next, pipeline) => async (t) =>
                {
                    var result = await pipeline.HandleAsync(httpContext, request, next, cancellationToken);
                    return result;
                }).Invoke(cancellationToken);
    }

    public abstract TRequest ParseRequestFromFilterContext(EndpointFilterInvocationContext context);

    public IResult ValidateRequest(TRequest request) => slimEndpoint.Validate(request);

    public IResult ValidateFromFilterContext(EndpointFilterInvocationContext context) => ValidateRequest(ParseRequestFromFilterContext(context));
}

public record Unit;