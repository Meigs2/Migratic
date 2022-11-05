using FluentAssertions;
using Meigs2.Functional;

namespace Migratic.Core.Tests.Unit;

public class MigrationVersionTests
{
    [Test] public void Version_Tests()
    {
        var higherVersion = MigrationVersion.From(1, 2, 3).ValueOrThrow();
        var lowestVersion = MigrationVersion.From(1, 2).ValueOrThrow();
        var equalVersion = MigrationVersion.From(1, 2, 3).ValueOrThrow();
        
        // use fluent assertions to make the tests more readable
        higherVersion.Should().BeGreaterThan(lowestVersion);
        higherVersion.Should().BeGreaterOrEqualTo(equalVersion);
        higherVersion.Should().BeGreaterOrEqualTo(lowestVersion);
        higherVersion.Should().BeGreaterOrEqualTo(equalVersion);    
    }
}
