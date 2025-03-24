using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SlimEndpoints.AOT;

public interface ISlimEndpoint
{
    void Configure(RouteHandlerBuilder builder);
}

public interface ISlimEndpoint<TRequest, TResponse> : ISlimEndpoint where TRequest : class
{
    IResult Validate(TRequest request);
}

public interface ISlimEndpointWithoutRequest<TResponse> : ISlimEndpoint<Unit, TResponse>;

public interface ISlimEndpointWithoutResponse<TRequest> : ISlimEndpoint<TRequest, Unit> where TRequest : class;

public abstract class SlimEndpoint<TRequest, TResponse> : ISlimEndpoint<TRequest, TResponse> where TRequest : class
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

public abstract class SlimEndpointWithoutResponse<TRequest> : ISlimEndpointWithoutResponse<TRequest> where TRequest : class
{
    public virtual void Configure(RouteHandlerBuilder builder) { }
    public abstract Task HandleAsync(HttpContext httpContext, TRequest request, CancellationToken cancellationToken);

    public virtual IResult Validate(TRequest request) => Results.Ok();
}

public record Unit { }