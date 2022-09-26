using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Functional.Core;
using Microsoft.Extensions.DependencyInjection;
using Migratic.Core.Abstractions;
using Migratic.Core.Models;

namespace Migratic.Core;

public record MigraticConfiguration
{
    protected internal MigrationVersion StartingVersion { get; set; }
    protected internal Option<MigrationVersion> TargetVersion { get; set; } = Option.None;
    protected internal IEnumerable<MigrationProvider> MigrationScriptProviders { get; } = new List<MigrationProvider>();
    protected internal Encoding FileEncoding { get; set; } = Encoding.UTF8;
    protected internal Option<TimeSpan> CommandTimeout { get; set; } = Option.None;
    
    protected internal IEnumerable<string> SearchPaths { get; set; } = new List<string>();
    protected internal IEnumerable<string> SearchPatterns { get; set; }
    protected internal IEnumerable<Assembly> EmbeddedResourceMigrationsAssemblies { get; set; } = new List<Assembly>();
    protected internal IEnumerable<Assembly> CodeBasedMigrationsAssemblies { get; set; } = new List<Assembly>();
    
    public string Table { get; set; } = "Prime.Migrations";
    public string Schema { get; set; } = "system";
    public TransactionStrategy TransactionStrategy { get; set; } = TransactionStrategy.PerMigration;
    public bool IsClustered { get; set; } = true;
    public string ConnectionString { get; set; }
    
    public string VersionedMigrationPrefix { get; set; } = "V";
    public string RepeatableMigrationPrefix { get; set; } = "R";
    public string BaselineMigrationPrefix { get; set; } = "B";
    public string VersionSeparator { get; set; } = "_";
    public string NameSeparator { get; set; } = "__";
    public string FileExtension { get; set; } = ".sql";
    
    public string Prefix { get; set; } = "${";
    public string Suffix { get; set; } = "}";
    public bool UsePlaceholders { get; set; } = true;

    internal IMigraticDatabaseProvider _databaseProvider;
}

public static class MigrationConfigurationExtensions
{
    public static IServiceCollection AddCodeBasedMigration<T>(this IServiceCollection services, T migration) where T : ICodeBasedMigration
    {
        services.AddSingleton<ICodeBasedMigration>(migration);
        return services;
    }
    
    public static IServiceCollection AddCodeBasedMigrationsFromAssembly(this IServiceCollection builder, Assembly assembly)
    {
        var types = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(ICodeBasedMigration)));
        foreach (var type in types)
        {
            builder.AddSingleton(typeof(ICodeBasedMigration), type);
        }
        
        return builder;
    }
    
    public static MigraticConfiguration UsingPostgres(this MigraticConfiguration configuration)
    {
        configuration._databaseProvider = new PostgresDatabaseProvider();
        return configuration;
    }
}

public class PostgresDatabaseProvider : IMigraticDatabaseProvider
{
    public PostgresDatabaseProvider()
    {
    }

    public IEnumerable<MigraticHistory> GetHistory()
    {
        throw new NotImplementedException();
    }

    public bool HistoryTableExists()
    {
        throw new NotImplementedException();
    }

    public bool HistoryTableSchemaExists()
    {
        throw new NotImplementedException();
    }

    public Task<Result> CreateHistoryTableSchema()
    {
        throw new NotImplementedException();
    }

    public Task<Result> CreateHistoryTable()
    {
        throw new NotImplementedException();
    }

    public Task<Result> InsertHistoryEntry(Migration migration)
    {
        throw new NotImplementedException();
    }

    public Task<Result> InsertHistoryEntries(IEnumerable<Migration> migrations)
    {
        throw new NotImplementedException();
    }
}
