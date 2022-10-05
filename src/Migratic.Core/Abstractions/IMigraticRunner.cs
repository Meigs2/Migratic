using System.Collections.Generic;
using System.Threading.Tasks;
using Functional.Core;
using Migratic.Core.Models;

namespace Migratic.Core;

public interface IMigraticRunner
{
    Task<Result<List<Migration>>> GetMigrationsFromProviders(IEnumerable<IMigrationProvider> migrationScriptProviders);
    Task<Result> ExecuteAllOrNothingMigration(List<Migration> migrations);
    Task<Result> ExecuteTransactionPerMigration(List<Migration> migrations);
    Task<Result> InitializeMigratic();
    Task<Result<IEnumerable<MigraticHistory>>> GetMigraticHistory();
}