using System;
using System.Diagnostics.CodeAnalysis;

namespace SlimEndpoints.AOT;

[AttributeUsage(AttributeTargets.Class)]
public class SlimEndpointAttribute([StringSyntax("Route")] string route, string[] verbs, string group = "") : Attribute
{
    private readonly string route = route;
    private readonly string[] verbs = verbs.Length == 0 ? [HttpMehotds.Get] : verbs;
    private readonly string group = group;
}