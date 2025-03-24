# SlimEndpoints.AOT
C# Implementation of SlimEndpoints. Compatible 100% with AOT compilation.
This library create a wrapper for minimal apis but implementing REPR (Resource, Endpoint, Processor, Response) pattern.

SlimEndpoints.AOT [![NuGet Badge](https://buildstats.info/nuget/SlimEndpoints.AOT)](https://www.nuget.org/packages/SlimEndpoints.AOT/)

SlimEndpoints.AOT.Generator [![NuGet Badge](https://buildstats.info/nuget/SlimEndpoints.AOT.Generator)](https://www.nuget.org/packages/SlimEndpoints.AOT.Generator/)

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

In order to create a REPR pattern compatible API, you can use SlimEndpoints.AOT library. 

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

## Parameters declaration

As you can see in the example, the request is a class with the Route parameter as a property. The source generator rewrite each property into a delegate compatible with minimal apis included all of his decorators.
In the case of POST, PUT, PATCH and if route no declare parameters like "/UpdateProduct/{id}" asume whole request as a [FromBody] parameter.
You can override parameter behaviour following minimal apis parameter binding, please see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-9.0
If you need a service parameter inject directly in the handler, DO NOT put the service as a property into the request with [FromService]
See the following examples:

```csharp
// GET request
public class GetProductsRequest
{
    public int Id { get; set; }
    public string Filter { get; set; }
}
// Source generated delegate parameters
int Id, string Filter
// Minimal apis automatically infer Id parameter is a route parameter if the route is "/byid/{id}" and Filter is a query parameter because is not in the route
// this behaviour is part of Minimal apis, not SlimEndpoints.AOT


// GET request
public class GetProductsRequest
{
    [FromRoute]
    public int Id { get; set; }
    [FromQuery]
    public string Filter { get; set; }
}
// Source generated delegate parameters
[FromRoute] int Id, [FromQuery] string Filter



// POST request with a route parameter
public class UpdateWeatherForecastRequest
{
    [FromRoute]
    public int Id { get; set; }
    [FromBody]
    [Required]
    public UpdateWeatherForecastsRequest? Values { get; set; }
}
// This request will source generate the following delegate parameters
[Microsoft.AspNetCore.Mvc.FromRouteAttribute]int Id, 
[Microsoft.AspNetCore.Mvc.FromBodyAttribute] [System.ComponentModel.DataAnnotations.RequiredAttribute]Endpoints.UpdateWeatherForecastsRequest? Values



// POST request without route parameters
public class UpdateWeatherForecastsRequest
{
    [Required]
    public string? Name { get; set; }
}
// Source generated delegate parameter
[FromBody] UpdateWeatherForecastsRequest request

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

## Dependency Injection

SlimEndpoints.AOT is compatible with dependency injection. You can inject services directly into the handler.

```csharp
[SlimEndpoint("/byid/{id}", HttpMehotds.Get, "Products")]
public class GetProductByIdHandler(IRepositoryProducts repositoryProducts) : SlimEndpoint<GetProductsRequest, Product>
{
    private readonly IRepositoryProducts _repositoryProducts = repositoryProducts;

    public override async Task<Product> HandleAsync(HttpContext httpContext, GetProductsRequest request, CancellationToken cancellationToken)
    {
        var product = await _repositoryProducts.GetProductByIdAsync(request.Id);
        return product;
    }
}
```

## Validation

Activate validation in Program.cs
```csharp
app.MapGroup("/products")
    .AllowAnonymous()
    // ValidateRequestEndpointFilter is a filter that validates the request, by default each handler is Validate = OK, you must override the method ValidateRequest in the handler
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
            ? Results.Problem(title: "An error ocurred", statusCode: 400, 
                extensions: new Dictionary<string, object?>()
                {
                    ["Id"] = "id must be greater than 0"
                })
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
            return Results.Ok();
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
builder.Services.AddSlimEndpoints();
...
// Inject types into SerializationContext
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SlimJsonContext.Default);
});
...
var app = builder.Build();

app.MapGroup("/products")
    .AllowAnonymous()
    .AddEndpointFilter<ValidateRequestEndpointFilter>()
    .UseSlimEndpointsProducts();
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

## Implementing additional logic

You can implement additional logic in the handler, for example, logging, metrics, etc., using filters as the same way you use Minimal apis.
```csharp
// Exceptions filter
app.UseExceptionHandler(exceptionapp =>
    exceptionapp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>();
        await Results.Problem(title: ex?.Error?.Message ?? "Un error ha ocurrido")
            .ExecuteAsync(context);
    })
);



// Logging filter with stopwatcher
internal class LogginFilter(ILoggerFactory loggerFactory) : IEndpointFilter
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        Type implementation = context.Arguments[0]!.GetType();
        var logger = _loggerFactory.CreateLogger(implementation.Name);
        logger.LogInformation("Executing {implementation}", implementation.Name);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var result = await next(context);

        logger.LogInformation("Executed {implementation}, time ellapsed in seconds {seconds}", implementation.Name, stopwatch.ElapsedMilliseconds / 1000);
        stopwatch.Stop();
        return result;
    }
}
// Implementation Program.cs
app.MapGroup("/products")
    .AllowAnonymous()
    // Order matters
    .AddEndpointFilter<LogginFilter>()
    .AddEndpointFilter<ValidateRequestEndpointFilter>()
    .UseSlimEndpointsProducts();


```