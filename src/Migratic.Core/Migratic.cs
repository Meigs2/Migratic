using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Functional.Core;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Migratic.Core.Commands;

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
        if (!await _databaseProvider.MigraticSchemaExists())
            return Result.Failure(new Migratic.MigraticSchemaNotInitializedError());
        if (await _databaseProvider.MigraticTableExists()) return Result.Success;
        _logger.LogInformation("Migratic history table does not exist, creating it");
        return (await _databaseProvider.CreateHistoryTable()).Map(onSuccess: () => Result.Success,
                                                                  onFailure: error =>
                                                                      Result.Failure(
                                                                          new Migratic.
                                                                              MigraticTableNotInitializedError()));
    }

    public async Task<Result> InitializeMigraticSchema()
    {
        if (await _databaseProvider.MigraticSchemaExists()) return Result.Success;
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

    public Migratic(MigraticConfiguration config, IMigraticRunner migraticRunner, ILogger logger)
    {
        _config = config;
        _migraticRunner = migraticRunner;
        _logger = logger;
    }

    public async Task<Result> Migrate()
    {
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

    /// <summary>
    /// Performs a baseline migration. Will execute the baseline migration script and bring the database to the version
    /// specified in the baseline migration script. Will not execute any other migrations.
    /// </summary>
    /// <returns></returns>
    public Result Baseline() { return Result.Success; }
    
    /// <summary>
    /// Performs a repair operation on the migratic history table.
    /// Will remove any failed migrations from the history table, re-align the checksums and versions of the remaining
    /// migrations and mark any missing migrations as deleted.
    /// </summary>
    /// <returns></returns>
    public Result Repair() { return Result.Success; }
    
    /// <summary>
    /// Fully reverts the database to the initial state.
    /// Removes all tables, schemas and data from the database. Intended to be used in testing environments.
    /// </summary>
    /// <returns></returns>
    public Result Clean() { return Result.Success; }

    /// <summary>
    /// Performs a dry run of the migration process. Returns information about the current state of the database, the
    /// migrations that are to be applied and other information.
    /// </summary>
    /// <returns></returns>
    public MigraticStatus Status() { throw new NotImplementedException();}
    
    /// <summary>
    /// Gets the current version of the database. Returns None if the database is not initialized.
    /// </summary>
    public Option<MigrationVersion> Version() { return Option.None; }
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

public interface IMigraticRunner
{
}
