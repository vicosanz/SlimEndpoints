using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Scalar.AspNetCore;
using SlimEndpoints.AOT;
using System.Diagnostics.CodeAnalysis;
using WebApplication1;
using WebApplication1.Endpoints.Products;
using WebApplication1.Endpoints.Products.GetById;
using WebApplication1.Endpoints.Products.GetProducts;
using WebApplication1.Endpoints.Products.Upload;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddMemoryCache();

        builder.Services.AddSlimEndpoints();

        builder.Services.AddAntiforgery();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SlimJsonContext.Default);
        });
        builder.Services.AddSingleton(SlimJsonContext.Default);
        builder.Services.Configure<RouteOptions>(options =>
        {
            options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
        });

        builder.Services.AddTransient<IValidator<GetProductsRequest>, GetProductsRequestValidator>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        //Set defaults globally
        var rootGroup = app.MapGroup("")
            .AddEndpointFilter<LogginFilter>()
            .AddEndpointFilter<ValidateRequestEndpointFilter>()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesValidationProblem();

        rootGroup
            .UseSlimEndpointsweatherforecast("/weatherforecast", "Weather Forecast")
            .UseSlimEndpointsProducts("/products", "Products");

        rootGroup.MapGet("/generate-antiforgery-token", (IAntiforgery antiforgery, HttpContext httpContext) =>
        {
            // Generate the antiforgery tokens (cookie and request token)
            var tokens = antiforgery.GetAndStoreTokens(httpContext);

            var xsrfToken = tokens.RequestToken!;
            return TypedResults.Content($"{tokens.FormFieldName} {xsrfToken}", "text/plain");
        });

        app.UseExceptionHandler(exceptionapp =>
            exceptionapp.Run(async context =>
            {
                var ex = context.Features.Get<IExceptionHandlerFeature>();
                if (ex?.Error is ValidationException validation)
                {
                    IResult result = validation.Errors.ToValidationProblem();
                    await result.ExecuteAsync(context);
                    return;
                }
                await Results.Problem(title: ex?.Error?.Message ?? "Error ocurred")
                                .ExecuteAsync(context);
            })
        );

        app.Run();
    }
}
