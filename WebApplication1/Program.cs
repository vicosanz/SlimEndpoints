using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Scalar.AspNetCore;

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
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        rootGroup.MapGroup("/weatherforecast")
            .AddEndpointFilter<LogginFilter>()
            .AddEndpointFilter<ValidateRequestEndpointFilter>()
            .UseSlimEndpointsweatherforecast();

        rootGroup.MapGroup("/products")
            .AllowAnonymous()
            .AddEndpointFilter<LogginFilter>()
            .AddEndpointFilter<ValidateRequestEndpointFilter>()
            .UseSlimEndpointsProducts();

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
                await Results.Problem(title: ex?.Error?.Message ?? "Un error ha ocurrido")
                             .ExecuteAsync(context);
            })
        );

        app.Run();
    }    
}
