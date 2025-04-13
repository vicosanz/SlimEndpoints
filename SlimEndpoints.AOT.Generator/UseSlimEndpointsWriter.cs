namespace SlimEndpoints.AOT.Generator
{
    internal class UseSlimEndpointsWriter(string group, List<Metadata> metadata) : AbstractWriter
    {
        public string GetCode()
        {
            WriteFile();
            return GeneratedText();
        }

        private void WriteFile()
        {
            List<string> usings = ["System", "System.Diagnostics.CodeAnalysis"];
            foreach (var data in metadata)
            {
                usings.Add(data.Namespace);
            }

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
            WriteBrace($"public static class UseSlimEndpoints{group}Extensions", () =>
            {
                WriteBrace($"public static void UseSlimEndpoints{group}(this IEndpointRouteBuilder app)", () =>
                {
                    WriteBrace("using (var scope = app.ServiceProvider.CreateScope())", () =>
                    {
                        foreach (var data in metadata)
                        {
                            WriteLine($"scope.ServiceProvider.GetRequiredService<{data.Name}Implementation>().UseSlimEndpoint(app);");
                            WriteLine();
                        }
                    });
                });
                WriteLine();
                WriteBrace($"public static IEndpointRouteBuilder UseSlimEndpoints{group}(this IEndpointRouteBuilder app, [StringSyntax(\"Route\")] string prefix)", () =>
                {
                    WriteLine($"var group = app.MapGroup(prefix);");
                    WriteBrace("using (var scope = app.ServiceProvider.CreateScope())", () =>
                    {
                        foreach (var data in metadata)
                        {
                            WriteLine($"scope.ServiceProvider.GetRequiredService<{data.Name}Implementation>().UseSlimEndpoint(group);");
                            WriteLine();
                        }
                    });
                    WriteLine("return app;");
                });
            });
            WriteLine();
        }
    }
}