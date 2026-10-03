using System.Reflection;
using FsCheck.Xunit;

namespace Forzion.Simulation.Tests;

public class FoundationTests
{
    private const string CoreAssemblyName = "Forzion.Simulation";

    // ADR 0001: the simulation core is a pure C# library with no engine dependency.
    [Fact]
    public void Simulation_core_does_not_reference_Godot()
    {
        var core = Assembly.Load(CoreAssemblyName);

        var godotReferences = core.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Godot", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(godotReferences);
    }

    // Smoke test proving FsCheck properties are discovered and run by the xUnit runner.
    [Property]
    public bool Integer_addition_is_commutative(int a, int b) => a + b == b + a;
}
