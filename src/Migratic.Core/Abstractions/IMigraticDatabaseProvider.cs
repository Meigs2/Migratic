using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Functional.Core;
using Migratic.Core.Models;

namespace Migratic.Core.Abstractions;

public interface IMigraticDatabaseProvider
{
    Task<Result<IEnumerable<MigraticHistory>>> GetHistory();
    Task<Result<MigraticStatus>> GetStatus();
    
    public Task<bool> MigraticTableExists();
    public Task<bool> MigraticSchemaExists();
    Task<Result> CreateMigraticSchema();
    Task<Result> CreateHistoryTable();
    Task<Result> InsertHistoryEntry(Migration migration);
    Task<Result> InsertHistoryEntries(IEnumerable<Migration> migrations);
}

public record MigraticStatus
{
    public IEnumerable<MigraticHistory> History { get; init; }
    public IEnumerable<Migration> AppliedMigrations { get; init; }
    public IEnumerable<Migration> PendingMigrations { get; init; }
    public IEnumerable<Migration> MissingMigrations { get; init; }
    public IEnumerable<Migration> FailedMigrations { get; init; }
}