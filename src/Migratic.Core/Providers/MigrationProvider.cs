using System.Collections.Generic;
using System.Threading.Tasks;

namespace Migratic.Core;

public abstract class MigrationProvider
{
    public abstract string ProviderName { get; }
    protected MigraticConfiguration Configuration { get; }
    public abstract IEnumerable<Migration> GetMigrations();

    protected MigrationProvider(MigraticConfiguration configuration)
    {
        Configuration = configuration;
    }
}
