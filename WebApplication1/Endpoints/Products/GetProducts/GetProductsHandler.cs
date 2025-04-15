using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;

namespace WebApplication1.Endpoints.Products.GetProducts;

[SlimEndpoint("/all", [HttpMehotds.Get], "Products")]
public class GetProductsHandler : SlimEndpointWithoutRequest<Results<Ok<Product[]>, BadRequest<ProblemDetails>>>
{
    public override async Task<Results<Ok<Product[]>, BadRequest<ProblemDetails>>> HandleAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken);
        return TypedResults.Ok( new[]
        {
            new Product { Name = "Product 1", Price = 100 },
            new Product { Name = "Product 2", Price = 200 },
            new Product { Name = "Product 3", Price = 300 },
        });
    }
}