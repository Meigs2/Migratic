using FluentAssertions;
using Functional.Core;
using MediatR;
using Migratic.Core.Abstractions;
using Moq;
using Migratic.Core.Models;
using NUnit.Framework.Constraints;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Migratic.Core.Tests.Unit.Migratic;

[Category("Migrate Command")]
public class With_Empty_Database
{
    private readonly ILogger _logger = new ConsoleLogger();
    private readonly Mock<IMediator> _mediator = new();
    private static readonly Result Failure = Result.Failure("");

    [Test] public void Fails_When_Schema_Creation_Fails()
    {
        var mockDatabaseProvider = new Mock<MockProvider>();
        mockDatabaseProvider.Setup(x => x.CreateHistoryTableSchema()).Returns(Failure.ToTask);
        mockDatabaseProvider.Setup(x => x.HistoryTableSchemaExists()).Returns(false);
        mockDatabaseProvider.CallBase = true;
        var config = new MigraticConfiguration();
        var migratic = new Core.Migratic(config, _logger, _mediator.Object, mockDatabaseProvider.Object);
        migratic.Migrate().Result.IsFailure.Should().BeTrue();
    }

    [Test] public void Fails_When_Schema_Table_Creation_Fails()
    {
        var mockDatabaseProvider = new Mock<MockProvider>();
        mockDatabaseProvider.Setup(x => x.HistoryTableExists()).Returns(false);
        mockDatabaseProvider.Setup(x => x.CreateHistoryTable()).Returns(Failure.ToTask);
        mockDatabaseProvider.CallBase = true;
        var config = new MigraticConfiguration();
        var migratic = new Core.Migratic(config, _logger, _mediator.Object, mockDatabaseProvider.Object);
        migratic.Migrate().Result.IsFailure.Should().BeTrue();
    }
    
    [Test] public void Succeeds_When_No_Migrations_Returned()
    {
        var mockDatabaseProvider = new Mock<MockProvider>();
        var mockMigrationProvider = new Mock<MockMigrationProvider>();
        var config = new MigraticConfiguration();
        config.MigrationScriptProviders = config.MigrationScriptProviders.Append(mockMigrationProvider.Object);
        var migratic = new Core.Migratic(config, _logger, _mediator.Object, mockDatabaseProvider.Object);
        migratic.Migrate().Result.IsSuccess.Should().BeTrue();
    }
}

public class MockProvider : IMigraticDatabaseProvider
{
    public virtual Task<Result> CreateHistoryTable() => Result.Success.ToTask();
    public virtual IEnumerable<MigraticHistory> GetHistory() => new List<MigraticHistory>();
    public virtual bool HistoryTableExists() => true;
    public virtual bool HistoryTableSchemaExists() => true;
    public virtual Task<Result> CreateHistoryTableSchema() => Result.Success.ToTask();
    public virtual Task<Result> InsertHistoryEntry(Migration migration) => Result.Success.ToTask();
    public virtual Task<Result> InsertHistoryEntries(IEnumerable<Migration> migrations) => Result.Success.ToTask();
}

public class MockMigrationProvider : IMigrationProvider
{
    public string ProviderName => "Mock Provider";

    public Result<IEnumerable<Migration>> GetMigrations() => Result<IEnumerable<Migration>>.Success(Enumerable.Empty<Migration>());
}