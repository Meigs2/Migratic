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
    private readonly ILogger _logger;

    // expose a function constructor that takes in an action to configure the migrator
    public static MigraticBuilder Create()
    {
        return new MigraticBuilder(new ServiceCollection());
    }

    public Migratic(MigraticConfiguration config, ILogger logger)
    {
        _logger = logger;
        _config = config;
    }

    public Result Migrate()
    {
        // get all the migrations
        var migrations = _config.MigrationScriptProviders.SelectMany(provider => provider.GetMigrations())
                                .OrderBy(migration => migration.Version)
                                .ToList();
        return Result.Success;
    }

    public Result Baseline() { return Result.Success; }
    public Result Repair() { return Result.Success; }
    public Result Clean() { return Result.Success; }
    public void Status() { }
    public void Version() { }
    public void Info() { }
}

public class MigraticBuilder
{
    private readonly IServiceCollection _services;
    private MigraticConfiguration? _configuration;
    private ILogger? _logger;
    internal MigraticBuilder(IServiceCollection services) { _services = services; }

    public MigraticBuilder WithLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    public MigraticBuilder WithConfiguration(Action<MigraticConfiguration>? configure)
    {
        var migraticConfiguration = new MigraticConfiguration();
        configure?.Invoke(migraticConfiguration);
        _configuration = migraticConfiguration;
        return this;
    }

    public Option<Migratic> Build()
    {
        _configuration ??= new MigraticConfiguration();
        _logger ??= new ConsoleLogger();
        _services.AddSingleton(_configuration);
        _services.AddSingleton(_logger);
        _services.AddSingleton<Migratic>();
        return _services?.BuildServiceProvider()?.GetService<Migratic>() ?? Option<Migratic>.None;
    }
}

public static class MigraticExtensions
{
    public static IServiceCollection AddMigratic(this IServiceCollection services, Action<MigraticBuilder>? configure)
    {
        var migraticBuilder = new MigraticBuilder(services);
        if (configure != null) { configure(migraticBuilder); }
        else { migraticBuilder.Build(); }

        configure?.Invoke(migraticBuilder);
        services.AddSingleton(migraticBuilder);
        services.AddSingleton<Migratic>();
        if (services.All(x => x.ServiceType != typeof(ILogger))) { services.AddLogging(); }

        return services;
    }
}
