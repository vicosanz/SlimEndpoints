namespace SlimEndpoints.AOT.Generator
{
    internal class ValidatorWriter(List<Metadata> metadata) : AbstractWriter
    {
        public string GetCode()
        {
            WriteFile();
            return GeneratedText();
        }

        private void WriteFile()
        {
            List<string> usings = ["using System;", "using Microsoft.AspNetCore.Http.HttpResults;"];
            foreach (var data in metadata)
            {
                usings.Add($"using {data.Namespace};");
            }

            foreach (var @using in usings.Distinct())
            {
                WriteLine(@using);
            }
            WriteLine();
            WriteLine("#nullable enable");
            WriteLine();

            if (!string.IsNullOrEmpty(metadata[0].Namespace))
            {
                WriteLine($"namespace Microsoft.Extensions.DependencyInjection;");
            }
            WriteLine();
            WriteValidator();
        }

        private void WriteValidator()
        {
            WriteBrace("public class ValidateRequestEndpointFilter : IEndpointFilter", () =>
            {
                WriteBrace("public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)", () =>
                {
                    WriteLine("var result = context.ValidateRequest();");
                    WriteLine("return result is Ok ? await next(context) : result;");
                });
            });
            WriteLine();
            WriteBrace("public static class SlimEndpointsConverterExtensions", () =>
            {
                WriteBrace("public static IResult? ValidateRequest(this EndpointFilterInvocationContext context)", () =>
                {
                    WriteBrace("return context.Arguments[0] switch", () =>
                    {
                        foreach (var metadata in metadata)
                        {
                            WriteLine($"{metadata.NameTyped}Implementation impl => impl.ValidateFromFilterContext(context),");
                        }
                        WriteLine("_ => Results.Ok()");
                    });
                    WriteLine(";");
                });
            });
        }
    }
}