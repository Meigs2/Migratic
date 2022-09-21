using System;
using System.Collections.Generic;
using System.Linq;
using Functional.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Migratic.Core;

public interface ICodeBasedMigration
{
    string GetScript();
}

public class CodeBasedMigrationProvider : MigrationProvider
{
    private IServiceProvider _serviceProvider;

    public CodeBasedMigrationProvider(IServiceProvider serviceProvider, MigraticConfiguration configuration) : base(
        configuration)
    {
        _serviceProvider = serviceProvider;
    }

    public override string ProviderName => "Code Based";

    public override IEnumerable<Migration> GetMigrations()
    {
        // get all migrations from the service provider that implement ICodeBasedMigration
        var migrations = _serviceProvider.GetServices<ICodeBasedMigration>();

        // return the migrations as a list of Migration objects
        return migrations.Bind(m =>
                          {
                              var className = m.GetType().Name;
                              var migrationType = MigrationType.FromString(className, Configuration);
                              if (migrationType.IsNone) { return Option.None; }

                              var migrationVersion = MigrationVersion.FromString(className, Configuration);
                              if (migrationVersion.IsNone) { return Option.None; }

                              // create the migration
                              var migration = new Migration(migrationType.Value,
                                                            migrationVersion.Value,
                                                            className,
                                                            m.GetScript());

                              // yield the migration
                              return migration.ToSome();
                          })
                         .Map(x => x).ToList();
    }
}
