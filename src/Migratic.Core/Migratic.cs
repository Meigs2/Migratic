using System;
using System.Collections.Generic;
using System.Linq;
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

internal sealed class MigraticRunner : IMigraticRunner
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IMigraticDatabaseProvider _databaseProvider;

    public MigraticRunner(ILogger logger, IMediator mediator, IMigraticDatabaseProvider databaseProvider)
    {
        this._logger = logger;
        _mediator = mediator;
        this._databaseProvider = databaseProvider;
    }

    public async Task<Result<List<Migration>>> GetMigrationsFromProviders(
        IEnumerable<IMigrationProvider> migrationScriptProviders)
    {
        var providedMigrations = new List<Migration>();
        foreach (var provider in migrationScriptProviders)
        {
            var providerMigration = provider.GetMigrations();
            if (providerMigration.IsFailure)
            {
                _logger.LogError("Failed to get migrations from provider");
                _logger.LogError(providerMigration.ToString());
                return await Result<List<Migration>>.Failure("Failed to get migrations from provider").ToTask();
            }

            providedMigrations.AddRange(providerMigration.Value);
        }

        return await Result<List<Migration>>.Success(providedMigrations).ToTask();
    }

    public async Task<Result> ExecuteAllOrNothingMigration(List<Migration> migrations)
    {
        // Use one transaction for all migrations. If one fails, rollback all.
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        foreach (var migration in migrations)
        {
            var result = await ExecuteMigration(migration);
            if (result.IsFailure)
                return result.WithError($"{migration.Description} failed, rolling back all migrations.");
        }

        var insertionResult = await _databaseProvider.InsertHistoryEntries(migrations);
        if (insertionResult.IsFailure) return insertionResult.WithError("Failed to insert history entries");
        scope.Complete();
        return Result.Success;
    }

    public async Task<Result> ExecuteTransactionPerMigration(List<Migration> migrations)
    {
        var i = 0;
        foreach (var migration in migrations)
        {
            // Use one transaction per migration. If one fails, rollback that migration.
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var result = await ExecuteMigration(migration);
            if (result.IsFailure)
            {
                _logger.LogError("Failed to apply migration {MigrationVersion}", migration.Version);
                return result.WithError(
                    $"Migration {migration.Description} failed, rolling back. {i} migration(s) executed successfully.");
            }

            var insertionResult = await _databaseProvider.InsertHistoryEntry(migration);
            if (insertionResult.IsFailure) return insertionResult.WithError("Failed to insert history entries");
            scope.Complete();
            i++;
        }

        return Result.Success;
    }

    public async Task<Result> InitializeMigratic()
    {
        return await InitializeMigraticSchema() + await InitializeMigraticTable();
    }

    private async Task<Result> InitializeMigraticTable()
    {
        if (!_databaseProvider.MigraticSchemaExists())
            return Result.Failure(new Migratic.MigraticSchemaNotInitializedError());
        if (_databaseProvider.MigraticTableExists()) return Result.Success;
        _logger.LogInformation("Migratic history table does not exist, creating it");
        return (await _databaseProvider.CreateHistoryTable()).Map(onSuccess: () => Result.Success,
                                                                  onFailure: error =>
                                                                      Result.Failure(
                                                                          new Migratic.
                                                                              MigraticTableNotInitializedError()));
    }

    public async Task<Result> InitializeMigraticSchema()
    {
        if (_databaseProvider.MigraticSchemaExists()) return Result.Success;
        _logger.LogInformation("Migratic history schema does not exist, creating it");
        return (await _databaseProvider.CreateMigraticSchema()).Map(onSuccess: () => Result.Success,
                                                                    onFailure: error =>
                                                                        Result.Failure(error)
                                                                              .WithError(
                                                                                   new Migratic.
                                                                                       MigraticSchemaNotInitializedError()));
    }

    public async Task<Result<IEnumerable<MigraticHistory>>> GetMigraticHistory()
    {
        return await _databaseProvider.GetHistory();
    }

    public async Task<Result<Migration>> ExecuteMigration(Migration migration)
    {
        var result = await _mediator.Send(new ExecuteMigrationCommand(migration));
        if (result.IsFailure)
        {
            _logger.LogError("Failed to execute migration");
            _logger.LogError(result.ToString());
        }

        return result;
    }
}

public sealed class Migratic
{
    private readonly MigraticConfiguration _config;
    private readonly ILogger _logger;
    private readonly IMigraticRunner _migraticRunner;
    public static MigraticBuilder CreateBuilder() => new(new ServiceCollection());

    public Migratic(MigraticConfiguration config, IMigraticRunner migraticRunner)
    {
        _config = config;
        _migraticRunner = migraticRunner;
    }

    public async Task<Result> Migrate()
    {
        var initializationResult = await _migraticRunner.InitializeMigratic();
        if (initializationResult.IsFailure)
        {
            return initializationResult.WithError("Unable to initialize migratic history table and/or schemas.");
        }

        var providedMigrations = await _migraticRunner.GetMigrationsFromProviders(_config.MigrationScriptProviders);
        if (providedMigrations.IsFailure)
        {
            return providedMigrations.WithError("Unable to get migrations from providers.");
        }

        var history = await _migraticRunner.GetMigraticHistory();
        if (history.IsFailure) { return history.WithError("Unable to get migratic history."); }

        var orderedProvidedMigrations = providedMigrations.Value.OrderBy(m => m.Version).ToList();
        if (orderedProvidedMigrations.Count == 0)
        {
            _logger.LogInformation("No migrations found");
            return Result.Success;
        }

        var orderedMigrationsToApply = GetMigrationsToApply(orderedProvidedMigrations, history.Value).ToList();
        if (!orderedMigrationsToApply.Any())
        {
            _logger.LogInformation("No migrations to apply");
            return Result.Success;
        }

        _logger.LogInformation("Applying {Count} new migrations", orderedMigrationsToApply.Count());
        _logger.LogInformation("Using transaction strategy: {ConfigTransactionStrategy}", _config.TransactionStrategy);
        if (_config.TransactionStrategy == TransactionStrategy.AllOrNothing)
        {
            return await _migraticRunner.ExecuteAllOrNothingMigration(orderedMigrationsToApply);
        }

        if (_config.TransactionStrategy == TransactionStrategy.PerMigration)
        {
            return await _migraticRunner.ExecuteTransactionPerMigration(orderedMigrationsToApply);
        }

        return new InvalidOperationException("Invalid transaction strategy");
    }

    public IEnumerable<Migration> GetMigrationsToApply(IEnumerable<Migration> providedMigrations,
        IEnumerable<MigraticHistory> executedHistory)
    {
        var appliedMigrations = executedHistory.Select(h => h.Version);
        var maxVersion = appliedMigrations.Where(x => x.IsSome).Max(x => x.Value).ToOption();
        return maxVersion.IsNone ? providedMigrations : providedMigrations.Where(m => m.Version > maxVersion.Value);
    }

    public record MigraticSchemaNotInitializedError() : UnexpectedError("Migratic schema not initialized")
    {
    }

    public record MigraticTableNotInitializedError() : UnexpectedError("Migratic table not initialized")
    {
    }

    public Result Baseline() { return Result.Success; }
    public Result Repair() { return Result.Success; }
    public Result Clean() { return Result.Success; }
    public IEnumerable<MigraticHistory> Status() { return Enumerable.Empty<MigraticHistory>(); }
    public void Version() { }
    public void Info() { }
}

public interface IMigraticBuilder
{
    MigraticBuilder WithLogger(ILogger logger);
    MigraticBuilder Configuration(Action<MigraticConfiguration>? configure);
    MigraticBuilder DatabaseProvider(IMigraticDatabaseProvider databaseProvider);
}

public class MigraticBuilder : IMigraticBuilder
{
    private readonly IServiceCollection _services;
    private IMigraticDatabaseProvider _databaseProvider;
    private MigraticConfiguration? _configuration;
    private ILogger? _logger;
    internal MigraticBuilder(IServiceCollection services) { _services = services; }

    public MigraticBuilder WithLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    public MigraticBuilder Configuration(Action<MigraticConfiguration>? configure)
    {
        var migraticConfiguration = new MigraticConfiguration();
        configure?.Invoke(migraticConfiguration);
        _configuration = migraticConfiguration;
        return this;
    }

    public MigraticBuilder DatabaseProvider(IMigraticDatabaseProvider databaseProvider)
    {
        _databaseProvider = databaseProvider;
        return this;
    }

    internal Option<Migratic> Build()
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
    public static IServiceCollection AddMigratic(this IServiceCollection services, Action<IMigraticBuilder>? configure)
    {
        var migraticBuilder = new MigraticBuilder(services);
        if (configure != null) { configure(migraticBuilder); }
        else { migraticBuilder.Build(); }

        configure?.Invoke(migraticBuilder);
        services.AddScoped(s => migraticBuilder.Build().ValueOrThrow());
        services.AddSingleton<ILogger, ConsoleLogger>();
        return services;
    }
}