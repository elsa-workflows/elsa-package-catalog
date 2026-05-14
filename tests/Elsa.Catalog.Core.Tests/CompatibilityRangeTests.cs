using Elsa.Catalog.Core.Compatibility;
using FluentAssertions;

namespace Elsa.Catalog.Core.Tests;

public sealed class CompatibilityRangeTests
{
    private readonly VersionRangeEvaluator _ranges = new();

    [Fact]
    public void Evaluates_inclusive_and_exclusive_ranges()
    {
        _ranges.Includes("[1.0.0,2.0.0)", "1.5.0").Should().BeTrue();
        _ranges.Includes("[1.0.0,2.0.0)", "2.0.0").Should().BeFalse();
    }
}
