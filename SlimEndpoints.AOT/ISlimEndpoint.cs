using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SlimEndpoints.AOT;

public interface ISlimEndpoint
{
    void Configure(RouteHandlerBuilder builder);
}

public interface ISlimEndpoint<in TRequest, out TResponse> : ISlimEndpoint 
{
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
    public abstract Task<TResponse> HandleAsync(HttpContext httpContext, CancellationToken cancellationToken);

    public virtual IResult Validate(Unit request) => Results.Ok();
}

public abstract class SlimEndpointWithoutResponse<TRequest> : ISlimEndpointWithoutResponse<TRequest> 
{
    public virtual void Configure(RouteHandlerBuilder builder) { }
    public abstract Task HandleAsync(HttpContext httpContext, TRequest request, CancellationToken cancellationToken);

    public virtual IResult Validate(TRequest request) => Results.Ok();
}

public delegate Task RequestHandlerDelegate(CancellationToken cancellationToken = default);
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

public delegate Task<TResponse> PipelineHandlerDelegate<TRequest, TResponse>(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);

public interface ISlimEndpointPipeline<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

public abstract class SlimEndpointPipeline<TRequest, TResponse> : ISlimEndpointPipeline<TRequest, TResponse>
{
    public abstract Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

public record Unit { }