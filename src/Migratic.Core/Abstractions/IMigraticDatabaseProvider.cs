using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Functional.Core;
using Migratic.Core.Models;

namespace Migratic.Core;

public interface IMigraticDatabaseProvider
{
    string ProviderType { get; }
    Result<MigraticStatus> CreateStatus(IEnumerable<MigraticHistory> history);
    Task<Result<Migration>> Execute(Migration migration);
    Task<Result<IEnumerable<MigraticHistory>>> GetHistory();
    Task<Result<MigraticStatus>> GetStatus();
    Task<bool> MigraticTableExists();
    Task<bool> MigraticSchemaExists();
    Task<Result> CreateMigraticSchema();
    Task<Result> CreateHistoryTable();
    Task<Result> InsertHistoryEntry(Migration migration);
    Task<Result> InsertHistoryEntries(IEnumerable<Migration> migrations);
    Result<string> GetCurrentUser();

    // CleanDatabase should totally reset the database to a "clean" state, as if it was just created.
    Task<Result> DropAllTables();
}
