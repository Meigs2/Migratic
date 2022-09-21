using System;
using System.Linq;
using Functional.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Migratic.Core.Abstractions;

namespace Migratic.Core;

public sealed class Migratic
{
    private readonly MigraticConfiguration _config;
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly ILogger _logger;
        
    // expose a function constructor that takes in an action to configure the migrator
    public static Migratic Configure(Action<MigraticConfiguration>? configure)
    {
        var migraticConfiguration = new MigraticConfiguration();
        configure?.Invoke(migraticConfiguration);
        return new Migratic(migraticConfiguration);
    }

    private Migratic(MigraticConfiguration config, ILogger logger) : this(config)
    {
        _logger = logger;
    }

    private Migratic(MigraticConfiguration configuration)
    {
        _config = configuration;
    }

    public Exceptional Migrate()
    {
        // get all the migrations
        var migrations = _config.MigrationScriptProviders
            .SelectMany(provider => provider.GetMigrations())
            .OrderBy(migration => migration.Version)
            .ToList();
        
        return Exceptional.Success;
    }
    public Exceptional Baseline() { return Exceptional.Success; }
    public Exceptional Repair() { return Exceptional.Success; }
    public Exceptional Clean() { return Exceptional.Success; }
    public void Status() { }
    public void Version() { }
    public void Info() { }
}

public static class MigraticExtensions
{
    public static IServiceCollection AddMigratic(this IServiceCollection services, Action<MigraticConfiguration>? configure)
    {
        var migraticConfiguration = new MigraticConfiguration();
        configure?.Invoke(migraticConfiguration);
        services.AddSingleton(migraticConfiguration);
        services.AddSingleton<Migratic>();
        
        if (services.All(x => x.ServiceType != typeof(ILogger)))
        {
            services.AddLogging();
        }
        
        return services;
    }
}
