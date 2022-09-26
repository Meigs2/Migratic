using System.Collections.Generic;
using System.Threading.Tasks;
using Functional.Core;
using Migratic.Core.Models;

namespace Migratic.Core.Abstractions;

public interface IMigraticDatabaseProvider
{
    IEnumerable<MigraticHistory> GetHistory();
    
    public bool HistoryTableExists();
    public bool HistoryTableSchemaExists();
    Task<Result> CreateHistoryTableSchema();
    Task<Result> CreateHistoryTable();
    Task<Result> InsertHistoryEntry(Migration migration);
    Task<Result> InsertHistoryEntries(IEnumerable<Migration> migrations);
}