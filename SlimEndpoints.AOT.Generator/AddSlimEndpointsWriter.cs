
using Microsoft.CodeAnalysis;

namespace SlimEndpoints.AOT.Generator
{
    internal class AddSlimEndpointsWriter(List<Metadata> metadata) : AbstractWriter
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
            WriteBrace($"public static class AddSlimEndpointsExtensions", () =>
            {
                WriteBrace($"public static IServiceCollection AddSlimEndpoints(this IServiceCollection services)", () =>
                {
                    foreach (var data in metadata)
                    {
                        WriteLine($"services.AddScoped<{data.Name}>();");
                        WriteLine($"services.AddScoped<{data.Name}Implementation>();");
                        WriteLine();
                    }
                    WriteLine("return services;");
                });
            });
            WriteLine();
        }
    }
}