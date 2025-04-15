using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;
using System.Text.RegularExpressions;

namespace WebApplication1.Endpoints.Products.GetById
{
    [SlimEndpoint("/byid/{id}", [HttpMehotds.Get], group: "Products")]
    public class GetProductByIdHandler : SlimEndpoint<GetProductsRequest, IResult>
    {
        //public override IResult Validate(GetProductsRequest request)
        //{
        //    var validator = new GetProductsRequestValidator();
        //    var result = validator.Validate(request);
        //    return result.OkOrBadRequest();
        //}
        public override void Configure(RouteHandlerBuilder builder)
        {
            builder.Produces<Product>(StatusCodes.Status200OK);
        }

        public override async Task<IResult> HandleAsync(HttpContext httpContext, GetProductsRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(0, cancellationToken);
            httpContext.TryGetRequestQueryValues<int>("groups", out var groups);
            return TypedResults.Ok(new Product { Name = "Product 1", Price = 100 });
        }
    }
}
