using System.Collections.Generic;
using System.Threading.Tasks;
using Functional.Core;
using Migratic.Core.Models;

namespace Migratic.Core.Abstractions;

public interface IMigraticDatabaseProvider
{
    Task<Result<IEnumerable<MigraticHistory>>> GetHistory();
    
    public bool MigraticTableExists();
    public bool MigraticSchemaExists();
    Task<Result> CreateMigraticSchema();
    Task<Result> CreateHistoryTable();
    Task<Result> InsertHistoryEntry(Migration migration);
    Task<Result> InsertHistoryEntries(IEnumerable<Migration> migrations);
}