using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing.Constraints;
using WebApplication1;

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
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SlimJsonContext.Default);
        });
        builder.Services.Configure<RouteOptions>(options => 
        {
            options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.MapGroup("/weatherforecast")
            .AddEndpointFilter<LogginFilter>()
            .AddEndpointFilter<ValidateRequestEndpointFilter>()
            .UseSlimEndpointsweatherforecast();

        app.MapGroup("/products")
            .AllowAnonymous()
            .AddEndpointFilter<LogginFilter>()
            .AddEndpointFilter<ValidateRequestEndpointFilter>()
            .UseSlimEndpointsProducts();
        
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
