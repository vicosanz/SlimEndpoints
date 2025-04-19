using System.Linq;

namespace SlimEndpoints.AOT.Generator
{
    internal class UseSlimEndpointsWriter(IEnumerable<IGrouping<string, Metadata>> groups) : AbstractWriter
    {
        public string GetCode()
        {
            WriteFile();
            return GeneratedText();
        }

        private void WriteFile()
        {
            List<string> usings = ["System", "System.Diagnostics.CodeAnalysis", "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Routing", "SlimEndpoints.AOT"];
            usings.AddRange(
                groups.SelectMany(group => group.Select(endpoint => endpoint.Namespace)));

            foreach (var @using in usings.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                WriteLine($"using {@using};");
            }
            WriteLine();
            WriteLine("#nullable enable");
            WriteLine();

            WriteLine($"namespace Microsoft.Extensions.DependencyInjection;");
            WriteLine();
            WriteAddSlimEndpoints();
        }

        private void WriteAddSlimEndpoints()
        {
            WriteBrace($"public static class UseSlimEndpointsExtensions", () =>
            {
                WriteBrace($"public static IEndpointRouteBuilder UseSlimEndpoints(this IEndpointRouteBuilder app, string groupName, [StringSyntax(\"Route\")] string prefix, string? tag = null)", () =>
                {
                    WriteLine($"var group = app.MapGroup(prefix);");
                    WriteBrace("if (tag != null)", () =>
                    {
                        WriteLine($"group.WithTags(tag);");
                    });
                    WriteBrace("using (var scope = app.ServiceProvider.CreateScope())", () =>
                    {
                        WriteBrace($"foreach (var item in scope.ServiceProvider.GetKeyedServices<ISlimEndpointImplementation>(groupName))", ()=>
                        {
                            WriteLine($"item.UseSlimEndpoint(group);");
                        });
                    });
                    WriteLine("return app;");
                });
                WriteLine();

                foreach(var group in groups)
                {
                    WriteBrace($"public static IEndpointRouteBuilder UseSlimEndpoints{group.Key}(this IEndpointRouteBuilder app, [StringSyntax(\"Route\")] string prefix, string? tag = null)", () =>
                    {
                        WriteLine($"app.UseSlimEndpoints(\"{group.Key}\", prefix, tag);");
                        WriteLine("return app;");
                    });

                    WriteLine();
                }
            });
        }
    }
}