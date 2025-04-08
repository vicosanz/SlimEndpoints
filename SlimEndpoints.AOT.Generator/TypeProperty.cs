using Microsoft.CodeAnalysis;

namespace SlimEndpoints.AOT.Generator
{
    public record TypeProperty(ITypeSymbol TypeSymbol, ITypeSymbol Type, string Name, List<string> Annotations, bool HasBindParses)
    {
        public ITypeSymbol TypeSymbol { get; internal set; } = TypeSymbol;
        public ITypeSymbol Type { get; internal set; } = Type;
        public string Name { get; internal set; } = Name;
        public List<string> Annotations { get; internal set; } = Annotations;
        public bool HasBindParses { get; internal set; } = HasBindParses;
    }
}