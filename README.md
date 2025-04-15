# SlimEndpoints.AOT
C# Implementation of SlimEndpoints. Compatible 100% with AOT compilation.
This library create a wrapper for minimal apis but implementing REPR (Resource, Endpoint, Processor, Response) pattern.

SlimEndpoints.AOT [![NuGet Badge](https://buildstats.info/nuget/SlimEndpoints.AOT)](https://www.nuget.org/packages/SlimEndpoints.AOT/)

SlimEndpoints.AOT.Generator [![NuGet Badge](https://buildstats.info/nuget/SlimEndpoints.AOT.Generator)](https://www.nuget.org/packages/SlimEndpoints.AOT.Generator/)

[![publish to nuget](https://github.com/vicosanz/SlimEndpoints/actions/workflows/main.yml/badge.svg)](https://github.com/vicosanz/SlimEndpoints/actions/workflows/main.yml)


## Buy me a coffee
If you want to reward my effort, :coffee: https://www.paypal.com/paypalme/vicosanzdev?locale.x=es_XC


## Instalation

Add SlimEndpoints.AOT and SlimEndpoints.AOT.Generator to your project.
```bash
dotnet add package SlimEndpoints.AOT
dotnet add package SlimEndpoints.AOT.Generator
``` 

## Usage

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

As you can see in the example, the request is a class with the Route parameter as a property. 
The source generator rewrite each property into a delegate compatible with minimal apis included all of his decorators.
In the case of POST, PUT, PATCH and if route no declare parameters like "/UpdateProduct/{id}" 
asume whole request as a [FromBody] parameter.
You can override parameter behaviour following minimal apis parameter binding, 
please see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-9.0
If you need a service parameter inject directly in the handler, 
TRY TO NOT put the service as a property into the request with [FromService]
If you need custom bindings, follow the guide directly from Minimal apis
https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-9.0#custom-binding
An example code of this can be found in the repository.
https://github.com/vicosanz/SlimEndpoints/blob/master/WebApplication1/Endpoints/Products/Upload/PostUploadMultipartFormDataNonStandard.cs

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

## FromBody

If you declare multiple [FromBody] parameters, the source generator will create a Record with all the properties and the request will be injected as a single parameter.
This new Record source generated has not support for AOT compilation because is not possible to source generate json serialization context of a Record not defined by user,
the solution is inherit from a class with all properties with [FromBody] attribute or decorate the auxiliar class with [AsBody] attribute,
all properties must be decorated with [FromBody] attribute, if you use a property with another [From...] attribute, 
the source generator will generate a Record only with the properties with [FromBody] attribute.

```csharp
public class PostSlug2Request : BodyAuxiliar
{
    public string Slug { get; set; } 
}

[AsBody] // You can put one AsBody here or decorate each parameter with [FromBody]
public class BodyAuxiliar  
{
    [FromBody] // If the class is decorated with [AsBody] you can remove this attribute
    public int Id { get; set; }
    [FromBody] // If the class is decorated with [AsBody] you can remove this attribute
    public string Name { get; set; }
}

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

## Custom Bindings

Use BindAsync when you need additional bindings.
Please see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-9.0#custom-binding

Example

```csharp
//Binder to get data from Json Section of a multipart form-data
public record UploadData(string name, string description)
{
    public static async ValueTask<UploadData?> BindAsync(HttpContext httpContext,
        ParameterInfo parameter) => await httpContext.GetMultipartFormJsonSection<UploadData>(parameter);
}


//Usage of custom binder
public class PostUploadBindingRequest
{
    public int Id { get; set; }
    public UploadData Data { get; set; }
    public IFormFile File { get; set; }
}

```


Example using ComplexType library:

```bash
dotnet add package ComplexType
``` 

```csharp
[ComplexType]
public readonly partial record struct UserIdClaim
{
    public static ValueTask<UserIdClaim?> BindAsync(HttpContext httpContext)
    {
        var claimValue = httpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimNames.UserIdClaimType)?.Value;
        UserIdClaim? result = null;
        if (!string.IsNullOrWhiteSpace(claimValue))
        {
            result = claimValue;
        }
        return ValueTask.FromResult(result);
    }
}

//usage
//Automatically UserId will be populated with UserId Claim of the logged in user
public record Request(int Id, UserIdClaim UserId, string Description)
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
        await Results.Problem(title: ex?.Error?.Message ?? "Error ocurred")
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

## Pipelines

Implementing pipelines is a easy way to implement cross cutting concerns like logging, metrics, etc.

```csharp
[SlimEndpointPipeline(order: 1)] //Use order to define the order of execution
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

[SlimEndpointPipeline(2)] //Second pipeline in execution
public class ValidateRequest<TSlimEndpoint, TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) 
    : SlimEndpointPipeline<TSlimEndpoint, TRequest, TResponse>
{
    public override async Task<TResponse> HandleAsync(HttpContext httpContext, TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var result = await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken)));
            var errors = result.SelectMany(r => r.Errors);
            if (errors.Any())
            {
                if (typeof(TResponse).IsAssignableTo(typeof(IResult))) // if TResponse admit IResult short circuit gracefully
                {
                    return (TResponse)errors.ToValidationProblem();
                }
                throw new ValidationException(errors); // Short circuit with exception
            }
        }
        return await next(cancellationToken);
    }
}

```