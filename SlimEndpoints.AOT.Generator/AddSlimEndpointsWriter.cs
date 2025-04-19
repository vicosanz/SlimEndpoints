using Microsoft.CodeAnalysis;

namespace SlimEndpoints.AOT.Generator
{
    internal class AddSlimEndpointsWriter(List<Metadata> metadata, List<Pipeline> slimPipelines) : AbstractWriter
    {
        public string GetCode()
        {
            WriteFile();
            return GeneratedText();
        }

        private void WriteFile()
        {
            List<string> usings = ["System", "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Routing", "SlimEndpoints.AOT"];
            foreach (var data in metadata)
            {
                usings.AddRange(data.Usings);
                usings.Add(data.Namespace);
            }
            foreach (Pipeline pipeline in slimPipelines)
            {
                usings.AddRange(pipeline.Usings);
                usings.Add(pipeline.Namespace);
            }

            foreach (var @using in usings.Where(x=> !string.IsNullOrWhiteSpace(x)).Distinct())
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
                WriteBrace($"public static IServiceCollection AddSlimEndpoints(this IServiceCollection services, Action<SlimEndpointsConfiguration>? configuration = null)", () =>
                {
                    foreach (var data in metadata)
                    {
                        WriteLine($"services.AddScoped<{data.Name}>();");
                        WriteLine($"services.AddScoped<{data.Name}Implementation>();");
                        WriteLine($"services.AddScoped<SlimEndpointImplementation<{data.NameTyped}, {data.RequestType}, {data.ResponseType}>, {data.Name}Implementation>();");
                        WriteLine($"services.AddKeyedScoped<ISlimEndpointImplementation, {data.Name}Implementation>(\"{data.Group}\");");
                        foreach (var pipeline in slimPipelines)
                        {
                            WriteLine($"services.AddScoped<SlimEndpointPipeline<{data.NameTyped}, {data.RequestType}, {data.ResponseType}>, {pipeline.Name}_{data.Name}>();");
                        }
                        WriteLine();
                    }

                    WriteLine("var serviceConfig = new SlimEndpointsConfiguration();");
                    WriteLine("configuration?.Invoke(serviceConfig);");

                    WriteLine("return services;");
                });
            });
            WriteLine();
        }
    }
}