using System.Collections.Generic;
using System.Threading.Tasks;
using Functional.Core;

namespace Migratic.Core;

public interface IMigrationProvider
{
    string ProviderName { get; }
    Result<IEnumerable<Migration>> GetMigrations();
}