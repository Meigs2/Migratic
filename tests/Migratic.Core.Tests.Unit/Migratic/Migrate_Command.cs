using MediatR;
using Migratic.Core.Abstractions;
using Migratic.Core;
using Moq;
using NUnit.Framework.Internal;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Migratic.Core.Models;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Migratic.Core.Tests.Unit.Migratic;

public class Migrate_Command : MigraticTestsFixtueBase
{
    // use moq to createa a mock of a basic IMigraticDatabaseProvider
    private readonly Mock<IMigraticDatabaseProvider> _mockDatabaseProvider = new Mock<IMigraticDatabaseProvider>();
    private readonly ILogger _logger = new ConsoleLogger();
    private readonly Mock<IMediator> _mediator = new Mock<IMediator>();

    [Test]
    public void Should_Migrate_To_The_Last_Version()
    {
        var mockDatabaseProvider = new Mock<IMigraticDatabaseProvider>();
        mockDatabaseProvider.Setup(x => x.GetHistory()).Returns(new List<MigraticHistory>());
        var config = new MigraticConfiguration();
        var migratic = new Core.Migratic(config, _logger, _mediator.Object, mockDatabaseProvider.Object);
        migratic.Migrate();
    }
}