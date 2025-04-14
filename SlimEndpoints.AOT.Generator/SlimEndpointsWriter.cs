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
            List<string> usings = ["System", "Microsoft.AspNetCore.Mvc"];
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
                            .Replace(pipeline.ArgumentRequest, metadata.RequestType)
                            .Replace(pipeline.ArgumentResponse, metadata.ResponseType)} {x.Name}"))})";
                    constructorparams = $"({string.Join(", ", pipeline.ConstructorParameters.Value.Select(x => $"{x.Name}"))})";
                }
                WriteLine($"public class {pipeline.Name}_{metadata.Name}{constructor} : {pipeline.Name}<{metadata.RequestType}, {metadata.ResponseType}>{constructorparams};");
                WriteLine();
            }

            WriteBrace($"{metadata.Modifiers} class {metadata.NameTyped}Implementation({metadata.NameTyped} endpoint)", () =>
            {
                WriteLine($"private readonly {metadata.NameTyped} endpoint = endpoint;");
                WriteLine();
                WriteBrace($"public void UseSlimEndpoint(IEndpointRouteBuilder app)", () =>
                {
                    WriteIdented($"var route = app.MapMethods(\"{metadata.Route}\", [\"{string.Join(", ", metadata.Verbs)}\"],", () =>
                    {
                        WriteBracedArrowFunction($"static ([{GeneratorHelpers.FromServicesAttribute}] {metadata.NameTyped}Implementation implementation, {metadata.PropertiesWithTypeAndAnnotations}HttpContext httpContext, [Microsoft.AspNetCore.Mvc.FromServicesAttribute] IEnumerable<ISlimEndpointPipeline<{metadata.RequestType}, {metadata.ResponseType}>> pipelines, CancellationToken cancellationToken) =>", () =>
                        {
                            WriteLine($"return implementation.HandleAsync({metadata.PropertiesNames}httpContext, pipelines, cancellationToken);");
                        });
                    });
                    WriteLine($"endpoint.Configure(route);");
                });
                string handle;
                if (metadata.ResponseType == "SlimEndpoints.AOT.Unit")
                {
                    handle = $"public async Task HandleAsync({metadata.PropertiesWithType}HttpContext httpContext, IEnumerable<ISlimEndpointPipeline<{metadata.RequestType}, {metadata.ResponseType}>> pipelines, CancellationToken cancellationToken)";
                }
                else
                {
                    handle = $"public async Task<{metadata.ResponseType}> HandleAsync({metadata.PropertiesWithType}HttpContext httpContext, IEnumerable<ISlimEndpointPipeline<{metadata.RequestType}, {metadata.ResponseType}>> pipelines, CancellationToken cancellationToken)";
                }
                WriteBrace(handle, () =>
                {
                    if (!(metadata.RequestType == "SlimEndpoints.AOT.Unit"))
                    {
                        if (!string.IsNullOrWhiteSpace(metadata.PropertiesParse))
                        {
                            if (metadata.IsRequestTypePositionRecord)
                            {
                                WriteLine($"var request = new {metadata.RequestType}({metadata.PropertiesParse});");
                            }
                            else
                            {
                                WriteLine($"var request = new {metadata.RequestType} {{{metadata.PropertiesParse}}};");
                            }
                        }
                    }
                    var returnString = metadata.ResponseType == "SlimEndpoints.AOT.Unit" ? "" : "return ";
                    var requestString = metadata.RequestType == "SlimEndpoints.AOT.Unit" ? "" : "request, ";
                    //WriteLine($"{returnString}await endpoint.HandleAsync(httpContext, {requestString}cancellationToken);");

                    if (metadata.RequestType == "SlimEndpoints.AOT.Unit")
                    {
                        WriteLine("var request = new SlimEndpoints.AOT.Unit();");
                    }
                    if (metadata.ResponseType == "SlimEndpoints.AOT.Unit")
                    {
                        WriteBrace($"async Task<{metadata.ResponseType}> Handler(CancellationToken cancellationToken = default)", () =>
                        {
                            WriteLine($"await endpoint.HandleAsync(httpContext, {requestString}cancellationToken);");
                            WriteLine("return new SlimEndpoints.AOT.Unit();");
                        });
                    }
                    else
                    {
                        WriteLine($"Task<{metadata.ResponseType}> Handler(CancellationToken cancellationToken = default) => endpoint.HandleAsync(httpContext, {requestString}cancellationToken);");
                    }

                    WriteLine($"{returnString}await pipelines.Reverse().Aggregate((RequestHandlerDelegate<{metadata.ResponseType}>)Handler, (next, pipeline) => (t) => pipeline.HandleAsync(httpContext, request, next, cancellationToken)).Invoke(cancellationToken);");
                });
                WriteBrace($"public {metadata.RequestType} ParseRequestFromFilterContext(Microsoft.AspNetCore.Http.EndpointFilterInvocationContext context)", () =>
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

                WriteLine();
                WriteLine($"public IResult ValidateRequest({metadata.RequestType} request) => endpoint.Validate(request);");

                WriteLine();
                WriteLine("public IResult ValidateFromFilterContext(Microsoft.AspNetCore.Http.EndpointFilterInvocationContext context) => ValidateRequest(ParseRequestFromFilterContext(context));");
            });
            WriteLine();
        }
    }
}