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
            List<string> usings = ["System"];
            foreach (var data in metadata)
            {
                usings.Add(data.Namespace);
            }

            foreach (var @using in usings.Distinct())
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
            });
            WriteLine();
        }
    }
}