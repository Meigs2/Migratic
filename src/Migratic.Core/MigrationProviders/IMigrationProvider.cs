using System.Collections.Generic;
using System.Threading.Tasks;
using Meigs2.Functional;
using Meigs2.Functional.Results;

namespace Migratic.Core;

public interface IMigrationProvider
{
    string ProviderName { get; }
    Result<IEnumerable<Migration>> GetMigrations();
}