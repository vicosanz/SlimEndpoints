
using SlimEndpoints;

namespace WebApplication1.Endpoints.Products.GetProducts
{
    [SlimEndpoint("/all", HttpMehotds.Get, "Products")]
    public class GetProductsHandler : SlimEndpointWithoutRequest<Product[]>
    {
        public override Task<Product[]> HandleAsync(HttpContext httpContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(new[]
            {
                new Product { Name = "Product 1", Price = 100 },
                new Product { Name = "Product 2", Price = 200 },
                new Product { Name = "Product 3", Price = 300 },
            });
        }
    }
}
