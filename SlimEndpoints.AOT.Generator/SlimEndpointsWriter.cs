using Microsoft.CodeAnalysis;

namespace SlimEndpoints.AOT.Generator
{
    internal class SlimEndpointsWriter(Metadata metadata) : AbstractWriter
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
            foreach (var @using in usings.Distinct())
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
            WriteBrace($"{metadata.Modifiers} class {metadata.NameTyped}Implementation({metadata.NameTyped} endpoint)", () =>
            {
                WriteLine($"private readonly {metadata.NameTyped} endpoint = endpoint;");
                WriteLine();
                WriteBrace($"public void UseSlimEndpoint(IEndpointRouteBuilder app)", () =>
                {
                    WriteIdented($"var route = app.MapMethods(\"{metadata.Route}\", [\"{string.Join(", ", metadata.Verbs)}\"],", () =>
                    {
                        WriteBracedArrowFunction($"static ([FromServices] {metadata.NameTyped}Implementation implementation, {metadata.PropertiesWithTypeAndAnnotations}HttpContext httpContext, CancellationToken cancellationToken) =>", () =>
                        {
                            WriteLine($"return implementation.HandleAsync({metadata.PropertiesNames}httpContext, cancellationToken);");
                        });
                    });
                    WriteLine($"endpoint.Configure(route);");
                });
                string handle;
                if (metadata.ResponseType == "SlimEndpoints.AOT.Unit")
                {
                    handle = $"public async Task HandleAsync({metadata.PropertiesWithType}HttpContext httpContext, CancellationToken cancellationToken)";
                }
                else
                {
                    handle = $"public async Task<{metadata.ResponseType}> HandleAsync({metadata.PropertiesWithType}HttpContext httpContext, CancellationToken cancellationToken)";
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
                    WriteLine($"{returnString}await endpoint.HandleAsync(httpContext, {requestString}cancellationToken);");
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