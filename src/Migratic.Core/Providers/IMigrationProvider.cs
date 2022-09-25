using System.Collections.Generic;

namespace Migratic.Core;

public interface IMigrationProvider
{
    string ProviderName { get; }
    IEnumerable<Migration> GetMigrations();
}