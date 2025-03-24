using System;
using System.Diagnostics.CodeAnalysis;

namespace SlimEndpoints;

[AttributeUsage(AttributeTargets.Class)]
public class SlimEndpointAttribute([StringSyntax("Route")] string route, string verb = HttpMehotds.Get, string group = "") : Attribute
{
    private readonly string route = route;
    private readonly string verb = verb;
    private readonly string group = group;
}