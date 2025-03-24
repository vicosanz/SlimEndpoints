using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;

namespace WebApplication1.Endpoints.Products.GetById
{
    [SlimEndpoint("/byid/{id}", [HttpMehotds.Get], "Products")]
    public class GetProductByIdHandler : SlimEndpoint<GetProductsRequest, Product>
    {
        public override IResult Validate(GetProductsRequest request)
        {
            var validator = new GetProductsRequestValidator();
            var result = validator.Validate(request);
            return result.OkOrBadRequest();
        }

        public override Task<Product> HandleAsync(HttpContext httpContext, GetProductsRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Product { Name = "Product 1", Price = 100 });
        }
    }
}
