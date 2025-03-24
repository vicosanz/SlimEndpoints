using Microsoft.CodeAnalysis;

namespace SlimEndpoints.AOT.Generator
{
    public record TypeProperty(ITypeSymbol Type, string Name, string Annotations)
    {
        public ITypeSymbol Type { get; internal set; } = Type;
        public string Name { get; internal set; } = Name;
        public string Annotations { get; internal set; } = Annotations;
    }
}