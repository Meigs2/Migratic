using System.Collections.Generic;
using System.Threading.Tasks;
using Functional.Core;

namespace Migratic.Core;

public abstract class MigrationProvider
{
    public abstract string ProviderName { get; }
    protected MigraticConfiguration Configuration { get; }
    public abstract Task<Result<IEnumerable<Migration>>> GetMigrations();

    protected MigrationProvider(MigraticConfiguration configuration)
    {
        Configuration = configuration;
    }
}
