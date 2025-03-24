# SlimEndpoints
C# Implementation of SlimEndpoints. Compatible 100% with AOT compilation.
This library create a wrapper for minimal apis but implementing REPR (Resource, Endpoint, Processor, Response) pattern.

SlimEndpoints [![NuGet Badge](https://buildstats.info/nuget/SlimEndpoints)](https://www.nuget.org/packages/SlimEndpoints/)

SlimEndpoints.Generator [![NuGet Badge](https://buildstats.info/nuget/SlimEndpoints.Generator)](https://www.nuget.org/packages/SlimEndpoints.Generator/)

[![publish to nuget](https://github.com/vicosanz/SlimEndpoints/actions/workflows/main.yml/badge.svg)](https://github.com/vicosanz/SlimEndpoints/actions/workflows/main.yml)


## Buy me a coffee
If you want to reward my effort, :coffee: https://www.paypal.com/paypalme/vicosanzdev?locale.x=es_XC


Minimal apis generally looks like this:
```csharp
// GET endpoint: Retrieve all products
app.MapGet("/products", () =>
{
    return Results.Ok(products);
});

// POST endpoint: Add a new product
app.MapPost("/products", (Product newProduct) =>
{
    products.Add(newProduct);
    return Results.Created($"/products/{newProduct.Id}", newProduct);
});
```

In order to create a REPR pattern compatible API, you can use SlimEndpoints library. 

```csharp
// Endpoints/Products/Product.cs
public record Product
{
    public string Name { get; init; }
    public decimal Price { get; init; }
}

// Endpoints/Products/GetProducts/GetProductsHandler.cs
// "Products" is the group name, this create a "UseSlimEndpointsProducts" that must be used in Program.cs
[SlimEndpoint("/all", HttpMehotds.Get, "Products")]
public class GetProducts : SlimEndpointWithoutRequest<Product[]>
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

// Endpoints/Products/GetById/GetByIdHandler.cs
[SlimEndpoint("/byid/{id}", HttpMehotds.Get, "Products")]
public class GetProductByIdHandler : SlimEndpoint<GetProductsRequest, Product>
{
    public override Task<Product> HandleAsync(HttpContext httpContext, GetProductsRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Product { Name = "Product 1", Price = 100 });
    }
}
// Endpoints/Products/GetById/GetProductsRequest
public class GetProductsRequest
{
    //Id is injected into the route, [FromRoute] is optional here
    public int Id { get; set; }
}

// Program.cs
var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSlimEndpoints();

var app = builder.Build();

// USeSlimEndpointsProducts is generated based in group name
// group+handler /products/all
// group+handler /products/byid/{id}
app.MapGroup("/products")
    .UseSlimEndpointsProducts();

```

## Configure route

Routes can be configured from registration or from the handler itself.

```csharp
// From registration in Program.cs
[SlimEndpoint("/byid/{id}", HttpMehotds.Get, "ProductsAnonymous")]
public class GetProductByIdHandler : SlimEndpoint<GetProductsRequest, Product>
//...

app.MapGroup("/products")
    .AllowAnonymous()
    .UseSlimEndpointsProductsAnonymous();



// From handler
[SlimEndpoint("/byid/{id}", HttpMehotds.Get, "Products")]
public class GetProductByIdHandler : SlimEndpoint<GetProductsRequest, Product>
{
    public override void Configure(RouteHandlerBuilder builder)
    {
        // Allow anonymous access
        builder.AllowAnonymous();
    }
    public override Task<Product> HandleAsync(HttpContext httpContext, GetProductsRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Product { Name = "Product 1", Price = 100 });
    }
}
```

## Validation

Activate validation in Program.cs
```csharp
app.MapGroup("/products")
    .AllowAnonymous()
    // ValidateRequestEndpointFilter is a filter that validates the request, by default each handler is validated = true, you must override the method ValidateRequest in the handler
    .AddEndpointFilter<ValidateRequestEndpointFilter>()
    .UseSlimEndpointsProducts();


// Handler
[SlimEndpoint("/byid/{id}", HttpMehotds.Get, "Products")]
public class GetProductByIdHandler : SlimEndpoint<GetProductsRequest, Product>
{
    public override IResult Validate(GetProductsRequest request)
    {
        // Basic request validation
        return request.Id == 0 
            ? Results.BadRequest(TypedResults.Problem("id must be greater than 0", statusCode: 400).ProblemDetails) 
            : Results.Ok();
    }

    public override Task<Product> HandleAsync(HttpContext httpContext, GetProductsRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Product { Name = "Product 1", Price = 100 });
    }
}
```

You can use FluentValidation for more complex validations
```csharp
// Handler
[SlimEndpoint("/byid/{id}", HttpMehotds.Get, "Products")]
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

// Validator
public class GetProductsRequestValidator : AbstractValidator<GetProductsRequest>
{
    public GetProductsRequestValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greather than zero");
    }
}

// Extension
public static class FluentValidationResultExtensions
{
    public static IResult OkOrBadRequest(this FluentValidation.Results.ValidationResult result)
    {
        if (result.IsValid)
        {
            Results.Ok();
        }
        return Results.Problem(title: "An error ocurred", statusCode: 400, extensions: result.Errors.Select(x => new KeyValuePair<string, object?>(x.PropertyName, x.ErrorMessage)));
    }
}
```

## AOT Compilation

SlimEndpoints is compatible with AOT compilation

```csharp
// CSProj
  <PropertyGroup>
    <PublishAot>true</PublishAot>
  </PropertyGroup>

// Program.cs
// Use CreateSlimBuilder instead of CreateBuilder
var builder = WebApplication.CreateSlimBuilder(args);

...
// Inject types into SerializationContext
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SlimJsonContext.Default);
});
...
var app = builder.Build();
...

// SlimJsonContext.cs
using System.Text.Json.Serialization;
using WebApplication1.Endpoints.Products;

[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ValidationProblemDetails))]
[JsonSerializable(typeof(Endpoints.GetProductsRequest))]
[JsonSerializable(typeof(Product))]
public partial class SlimJsonContext : JsonSerializerContext
{
}
```