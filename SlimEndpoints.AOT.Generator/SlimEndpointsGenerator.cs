using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SlimEndpoints.AOT.Generator
{
    [Generator]
    public class SlimEndpointsGenerator : IIncrementalGenerator
    {
        private static readonly string slimEndpointAttribute = "SlimEndpoints.AOT.SlimEndpointAttribute";
        private static readonly string iSlimEndpointType = "SlimEndpoint<";
        private static readonly string iSlimEndpointWithoutRequest = "SlimEndpointWithoutRequest<";
        private static readonly string iSlimEndpointWithoutResponse = "SlimEndpointWithoutResponse<";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached) Debugger.Launch();
//#endif
            IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s.IsSyntaxTargetForGeneration(),
                    transform: static (ctx, _) => ctx.GetSemanticTargetForGeneration(slimEndpointAttribute))
                .Where(static m => m is not null)!;

            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndEnums
                = context.CompilationProvider.Combine(typeDeclarations.Collect());
            
            context.RegisterSourceOutput(compilationAndEnums,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> type, SourceProductionContext context)
        {
            if (type.IsDefaultOrEmpty) return;

            List<Metadata> slimEndpoints = GetSlimpEndpoints(compilation, type.Distinct(), context);
            var groups = slimEndpoints.GroupBy(x => x.Group);

            if (groups.Any())
            {
                var generatorAddSlimEndpoints = new AddSlimEndpointsWriter(slimEndpoints);
                context.AddSource($"{slimEndpoints[0].Namespace}.AddSlimEndpoints.g.cs",
                    SourceText.From(generatorAddSlimEndpoints.GetCode(), Encoding.UTF8));

                foreach (var group in groups)
                {
                    var generatorGroup = new UseSlimEndpointsWriter(group.Key, [.. group]);
                    context.AddSource($"{slimEndpoints[0].Namespace}.UseSlimEndpoints.{group.Key}.g.cs",
                        SourceText.From(generatorGroup.GetCode(), Encoding.UTF8));

                    foreach (var slimEndpoint in group)
                    {
                        var generatorSlimEndpoint = new SlimEndpointsWriter(slimEndpoint);
                        context.AddSource(slimEndpoint.GetFileNameImplementationGenerated(),
                            SourceText.From(generatorSlimEndpoint.GetCode(), Encoding.UTF8));
                    }
                }

                var generatorValidator = new ValidatorWriter(slimEndpoints);
                context.AddSource($"{slimEndpoints[0].Namespace}.Validators.g.cs",
                    SourceText.From(generatorValidator.GetCode(), Encoding.UTF8));
            }
        }

        protected static List<Metadata> GetSlimpEndpoints(Compilation compilation,
            IEnumerable<TypeDeclarationSyntax> types, SourceProductionContext context)
        {
            
            var slimEndpoints = new List<Metadata>();
            foreach (var type in types)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                SemanticModel semanticModel = compilation.GetSemanticModel(type.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(type) is not INamedTypeSymbol typeSymbol)
                {
                    // report diagnostic, something went wrong
                    continue;
                }
                string modifiers = type.GetModifiers();

                TypeSyntax? requestTypeSyntax = null;
                string requestType = "SlimEndpoints.AOT.Unit";
                string responseType = "SlimEndpoints.AOT.Unit";

                if (type.BaseList != null)
                {
                    foreach (var baseType in type.BaseList!.Types)
                    {
                        if (baseType.ToFullString().Contains(iSlimEndpointType))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            requestTypeSyntax = argumentsType.TypeArgumentList.Arguments[0];
                            requestType = requestTypeSyntax.ToFullString();
                            if (argumentsType.TypeArgumentList.Arguments.Count > 1)
                            {
                                responseType = argumentsType.TypeArgumentList.Arguments[1].ToFullString();
                            }
                        }
                        else if (baseType.ToFullString().Contains(iSlimEndpointWithoutRequest))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            responseType = argumentsType.TypeArgumentList.Arguments[0].ToFullString();
                        }
                        else if (baseType.ToFullString().Contains(iSlimEndpointWithoutResponse))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            requestTypeSyntax = argumentsType.TypeArgumentList.Arguments[0];
                            requestType = requestTypeSyntax.ToFullString();
                        }
                    }
                }
                List<TypeProperty>? requestTypeSymbolProperties = null;
                if (requestType != "SlimEndpoints.AOT.Unit")
                {
                    var requestTypeSymbol = semanticModel.GetTypeInfo(requestTypeSyntax!).Type;
                    if (requestTypeSymbol is null)
                    {
                        // report diagnostic, something went wrong
                        continue;
                    }
                    requestTypeSymbolProperties = [.. requestTypeSymbol.GetMembers().OfType<IPropertySymbol>().Select(x => new TypeProperty(x.Type, x.Name, x.GetAnnotations()))];
                }

                string route = "";
                string[] verbs = [HttpMehotds.Get];
                string group = "";

                foreach (var attribute in typeSymbol.GetAttributes())
                {
                    if (attribute.AttributeClass!.ToDisplayString().Equals(slimEndpointAttribute, StringComparison.OrdinalIgnoreCase))
                    {
                        if (attribute.ConstructorArguments.Any())
                        {
                            route = attribute.ConstructorArguments.First().Value!.ToString();
                            if (attribute.ConstructorArguments.Count() > 1)
                            {
                                verbs = attribute.ConstructorArguments[1].Values.Select(x => x.Value!.ToString()).ToArray();
                            }
                            if (attribute.ConstructorArguments.Count() > 2)
                            {
                                group = attribute.ConstructorArguments[2].Value!.ToString();
                            }
                        }
                    }
                }
                slimEndpoints.Add(
                    new Metadata(type.GetNamespace(),
                                        type.GetUsings(),
                                        true,
                                        typeSymbol.Name,
                                        typeSymbol.GetNameTyped(),
                                        typeSymbol.ToString(),
                                        modifiers,
                                        requestType,
                                        responseType,
                                        requestTypeSymbolProperties,
                                        route,
                                        verbs,
                                        group));
            }
            return slimEndpoints;
        }

    }
}
