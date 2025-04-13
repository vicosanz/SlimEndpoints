namespace SlimEndpoints.AOT.Generator
{
    public class Parameter
    {
        public Parameter(string type, string name)
        {
            Type_ = type;
            Name = name;
        }

        public string Type_ { get; }
        public string Name { get; }
    }
}