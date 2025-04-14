
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace SlimEndpoints.AOT.Generator
{
    public class Pipeline
    {
        public string Namespace { get; internal set; } = null!;
        public IReadOnlyList<string> Usings { get; internal set; } = null!;
        public string Name { get; internal set; } = null!;
        public string NameTyped { get; internal set; } = null!;
        public string Type { get; internal set; } = null!;
        public string Modifiers { get; internal set; } = null!;
        public INamedTypeSymbol TypeSymbol { get; internal set; } = null!;
        public int Order { get; internal set; }
        public ImmutableArray<IParameterSymbol>? ConstructorParameters { get; internal set; }
        public string ArgumentRequest { get; internal set; } = null!;
        public string ArgumentResponse { get; internal set; } = null!;
    }
}