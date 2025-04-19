using Microsoft.CodeAnalysis;

namespace SlimEndpoints.AOT.Generator
{
    internal class SlimEndpointsWriter(Metadata metadata, List<Pipeline> slimPipelines) : AbstractWriter
    {
        public string GetCode()
        {
            WriteFile();
            return GeneratedText();
        }

        private void WriteFile()
        {
            List<string> usings = ["System", "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Routing", "Microsoft.AspNetCore.Mvc"];
            usings = [.. usings, .. metadata.Usings];
            foreach (Pipeline pipeline in slimPipelines)
            {
                usings.AddRange(pipeline.Usings);
                usings.Add(pipeline.Namespace);
            }
            foreach (var @using in usings.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                WriteLine($"using {@using};");
            }
            WriteLine();
            WriteLine("#nullable enable");
            WriteLine();

            if (!string.IsNullOrEmpty(metadata.Namespace))
            {
                WriteLine($"namespace {metadata.Namespace};");
            }
            WriteLine();
            WriteSlimEndpoints();
        }

        private void WriteSlimEndpoints()
        {
            if (metadata.CreateAuxiliarBodyRequestClass)
            {
                WriteNested($"public record {metadata.AuxiliarBodyRequestClassName}", "(", ");", () =>
                {
                    WriteLine($"{metadata.RecordParametersBodyRequest}");
                    //foreach(var property in metadata.RequestTypeProperties.Where(x => x.HasFromBoyAnnotations()))
                    //{
                    //    WriteLine($"public {property.Type} {property.Name} {{ get; set; }}");
                    //}
                });
                WriteLine();
            }

            foreach (var pipeline in slimPipelines)
            {
                var constructor = "";
                var constructorparams = "";
                if ((pipeline.ConstructorParameters?.Count() ?? 0) > 0)
                {//.Replace(typeSymbol.ToString(), $"{typeSymbol.Name}")
                    constructor = $"({string.Join(", ", pipeline.ConstructorParameters!.Value
                        .Select(x => $"{x.Type.ToString()
                            .Replace(pipeline.Type, $"{pipeline.Name}_{metadata.Name}")
                            .Replace(pipeline.ArgumentSlimEndpoint, metadata.NameTyped)
                            .Replace(pipeline.ArgumentRequest, metadata.RequestType)
                            .Replace(pipeline.ArgumentResponse, metadata.ResponseType)} {x.Name}"))})";
                    constructorparams = $"({string.Join(", ", pipeline.ConstructorParameters.Value.Select(x => $"{x.Name}"))})";
                }
                WriteLine($"public class {pipeline.Name}_{metadata.Name}{constructor} : {pipeline.Name}<{metadata.NameTyped}, {metadata.RequestType}, {metadata.ResponseType}>{constructorparams};");
                WriteLine();
            }

            WriteBrace($"{metadata.Modifiers} class {metadata.NameTyped}Implementation({metadata.NameTyped} endpoint, IEnumerable<SlimEndpointPipeline<{metadata.NameTyped}, {metadata.RequestType}, {metadata.ResponseType}>> pipelines) : SlimEndpointImplementation<{metadata.NameTyped}, {metadata.RequestType}, {metadata.ResponseType}>(endpoint, pipelines)", () =>
            {
                WriteLine($"private readonly {metadata.NameTyped} endpoint = endpoint;");
                WriteLine();
                WriteBrace($"public override void UseSlimEndpoint(IEndpointRouteBuilder app)", () =>
                {
                    WriteIdented($"var route = app.MapMethods(\"{metadata.Route}\", [\"{string.Join(", ", metadata.Verbs)}\"],", () =>
                    {
                        WriteBracedArrowFunction($"static async ([{GeneratorHelpers.FromServicesAttribute}] SlimEndpointImplementation<{metadata.NameTyped}, {metadata.RequestType}, {metadata.ResponseType}> implementation, {metadata.PropertiesWithTypeAndAnnotations}HttpContext httpContext, CancellationToken cancellationToken) =>", () =>
                        {
                            if (metadata.RequestType == "SlimEndpoints.AOT.Unit")
                            {
                                WriteLine("var __request__ = new SlimEndpoints.AOT.Unit();");
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(metadata.PropertiesParse))
                                {
                                    WriteLine("var __request__ = request;");
                                }
                                else 
                                { 
                                    if (metadata.IsRequestTypePositionRecord)
                                    {
                                        WriteLine($"var __request__ = new {metadata.RequestType}({metadata.PropertiesParse});");
                                    }
                                    else
                                    {
                                        WriteLine($"var __request__ = new {metadata.RequestType} {{{metadata.PropertiesParse}}};");
                                    }
                                }
                            }

                            var returnString = metadata.ResponseType == "SlimEndpoints.AOT.Unit" ? "" : "return ";
                            WriteLine($"{returnString}await implementation.HandleAsync(httpContext, __request__, cancellationToken);");
                        });
                    });
                    if (metadata.ProduceType != null)
                    {
                        WriteLine($"route.Produces<{metadata.ProduceType}>(StatusCodes.Status200OK);");
                    }
                    else if (metadata.ResponseType.Contains(".IResult"))
                    {
                        WriteLine($"route.Produces(StatusCodes.Status200OK);");
                    }
                    WriteLine($"endpoint.Configure(route);");
                });
                WriteBrace($"public override {metadata.RequestType} ParseRequestFromFilterContext(Microsoft.AspNetCore.Http.EndpointFilterInvocationContext context)", () =>
                {
                    if (metadata.RequestType == "SlimEndpoints.AOT.Unit")
                    {
                        WriteLine("var request = new SlimEndpoints.AOT.Unit();");
                    }
                    else if (metadata.IsRequestFromBody || metadata.IsRequestAsParameter)
                    {
                        WriteLine($"var request = {metadata.PropertiesFromContext};");
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(metadata.ParseinnerBodyRequest))
                        {
                            WriteLine(metadata.ParseinnerBodyRequest);
                        }
                        if (metadata.IsRequestTypePositionRecord)
                        {
                            WriteLine($"var request = new {metadata.RequestType}({metadata.PropertiesFromContext});");
                        }
                        else
                        {
                            WriteLine($"var request = new {metadata.RequestType}(){{ {metadata.PropertiesFromContext} }};");
                        }
                    }
                    WriteLine("return request;");
                });
            });
            WriteLine();
        }
    }
}