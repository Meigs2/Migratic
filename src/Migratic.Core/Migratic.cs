using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Transactions;
using Functional.Core;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Migratic.Core.Abstractions;
using Migratic.Core.Commands;
using Migratic.Core.Models;

namespace Migratic.Core;

public sealed class Migratic
{
    private readonly MigraticConfiguration _config;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IMigraticDatabaseProvider _databaseProvider;

    // expose a function constructor that takes in an action to configure the migrator
    public static MigraticBuilder Create() { return new MigraticBuilder(new ServiceCollection()); }

    public Migratic(MigraticConfiguration config, ILogger logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        _config = config;
    }

    public async Task<Result> Migrate()
    {
        if (!_databaseProvider.HistoryTableSchemaExists())
        {
            _logger.LogInformation("No history history table schema was found. Attempting to create it");
            var result = await _databaseProvider.CreateHistoryTableSchema();
            if (result.IsFailure)
            {
                _logger.LogError("Failed to create history table schema");
                _logger.LogError(result.ToString());
                return result;
            }
        }

        if (!_databaseProvider.HistoryTableExists())
        {
            _logger.LogInformation("No history table was found. Attempting to create it");
            var result = await _databaseProvider.CreateHistoryTable();
            if (result.IsFailure)
            {
                _logger.LogError("Failed to create history table");
                _logger.LogError(result.ToString());
                return result;
            }
        }

        // get all migrations that have not been applied
        var providedMigrations = new List<Migration>();
        foreach (var provider in _config.MigrationScriptProviders)
        {
            var providerMigration = await provider.GetMigrations();
            if (providerMigration.IsFailure)
            {
                _logger.LogError("Failed to get migrations from provider");
                _logger.LogError(providerMigration.ToString());
            }
            else { providedMigrations.AddRange(providerMigration.Value); }
        }

        providedMigrations = providedMigrations.OrderBy(m => m.Version).ToList();
        if (providedMigrations.Count == 0)
        {
            _logger.LogInformation("No migrations found");
            return Result.Success;
        }

        var history = _databaseProvider.GetHistory();
        var unappliedMigrations = UnappliedMigrations(providedMigrations, history).ToList();
        
        if (!unappliedMigrations.Any())
        {
            _logger.LogInformation("No migrations to apply");
            return Result.Success;
        }
        
        _logger.LogInformation("Applying {Count} migrations", unappliedMigrations.Count());
        _logger.LogInformation("Using transaction strategy: {ConfigTransactionStrategy}", _config.TransactionStrategy);
        
        if (_config.TransactionStrategy == TransactionStrategy.AllOrNothing)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            foreach (var migration in providedMigrations)
            {
                var result = await MigrateInternal(migration);
                if (result.IsFailure)
                    return result.WithError($"{migration.Description} failed, rolling back all migrations.");
                scope.Complete();
            }

            return Result.Success;
        }

        if (_config.TransactionStrategy == TransactionStrategy.PerMigration)
        {
            var i = 0;
            foreach (var migration in providedMigrations)
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                var result = await MigrateInternal(migration);
                if (result.IsFailure)
                    return result.WithError(
                        $"Migration {migration.Description} failed, rolling back. {i} migration(s) executed successfully.");
                scope.Complete();
                i++;
            }

            return Result.Success;
        }

        return new InvalidOperationException("Invalid transaction strategy");
    }

    private IEnumerable<Migration> UnappliedMigrations(IEnumerable<Migration> providedMigrations,
        IEnumerable<MigraticHistory> history)
    {
        var appliedMigrations = history.Select(h => h.Version);
        var maxVersion = appliedMigrations.Where(x => x.IsSome).Max(x => x.Value).ToOption();
        return maxVersion.IsNone ? providedMigrations : providedMigrations.Where(m => m.Version > maxVersion.Value);
    }

    private async Task<Result<Migration>> MigrateInternal(Migration migration)
    {
        return await _mediator.Send(new ExecuteMigrationCommand(migration));
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