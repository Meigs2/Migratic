using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Functional.Core;
using Migratic.Core;

namespace Migratic.Postgresql;

public class PostgresqlDatabaseProvider : IMigraticDatabaseProvider
{
    private readonly MigraticConfiguration _config;
    private readonly IDbConnection _connection;
    private string? _currentUser;
    private string CurrentUser => _currentUser ??= GetCurrentUser().Value;

    public PostgresqlDatabaseProvider(MigraticConfiguration config, IDbConnection connection)
    {
        _config = config;
        _connection = connection;
    }

    public string ProviderType => "Postgres";

    public async Task<Result<Migration>> Execute(Migration migration)
    {
        try
        {
            await _connection.ExecuteAsync(migration.Sql);
            return migration.SetSuccess().WithAppliedBy(CurrentUser);
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public async Task<Result<IEnumerable<MigraticHistory>>> GetHistory()
    {
        try
        {
            var result = await _connection.QueryAsync<MigraticHistory>(
                $"SELECT * FROM {_config.Schema}.{_config.Table}");

            return result.ToResult();
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public async Task<Result<MigraticStatus>> GetStatus()
    {
        var history = await GetHistory();
        if (history.IsFailure)
        {
            return Result<MigraticStatus>.Failure(history.Errors);
        }

        return CreateStatus(history.Value);
    }

    public async Task<bool> MigraticSchemaExists()
    {
        var result = await _connection.QueryAsync<int>($@"
            SELECT COUNT(*) FROM information_schema.schemata
            WHERE schema_name = '{_config.Schema}'
        ");
        return result.FirstOrDefault() > 0;
    }

    public async Task<bool> MigraticTableExists()
    {
        var result = await _connection.QueryAsync<int>($@"
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = '{_config.Schema}'
            AND table_name = '{_config.Table}'
        ");
        return result.FirstOrDefault() > 0;
    }

    public async Task<Result> CreateMigraticSchema()
    {
        try
        {
            await _connection.ExecuteAsync($"CREATE SCHEMA {_config.Schema}");
            return Result.Success;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public async Task<Result> CreateHistoryTable()
    {
        try
        {
            await _connection.ExecuteAsync($@"
                CREATE TABLE {_config.Schema}.{_config.Table} (
                    id SERIAL PRIMARY KEY,
                    major INT NOT NULL,
                    minor INT,
                    patch INT,
                    description VARCHAR(255) NOT NULL,
                    provider_type VARCHAR(255) NOT NULL,
                    checksum VARCHAR(255) NOT NULL,
                    applied_at TIMESTAMP NOT NULL,
                    applied_by VARCHAR(255) NOT NULL,
                    success BOOLEAN NOT NULL
                )
            ");
            return Result.Success;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public async Task<Result> InsertHistoryEntry(Migration migration)
    {
        try
        {
            await _connection.ExecuteAsync($@"
                INSERT INTO {_config.Schema}.{_config.Table} (
                    major,
                    minor,
                    patch,
                    description,
                    provider_type,
                    checksum,
                    applied_at,
                    applied_by,
                    success
                ) VALUES (
                    @Major,
                    @Minor,
                    @Patch,
                    @Description,
                    @ProviderType,
                    @Checksum,
                    @AppliedAt,
                    @AppliedBy,
                    @Success
                )
            ",
                                           new
                                           {
                                               migration.Version.Major,
                                               migration.Version.Minor,
                                               migration.Version.Patch,
                                               migration.Description,
                                               ProviderType,
                                               migration.Checksum,
                                               migration.AppliedAt,
                                               migration.AppliedBy,
                                               migration.Success
                                           });
            return Result.Success;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public async Task<Result> InsertHistoryEntries(IEnumerable<Migration> migrations)
    {
        try
        {
            await _connection.ExecuteAsync($@"
                INSERT INTO {_config.Schema}.{_config.Table} (
                    major,
                    minor,
                    patch,
                    description,
                    provider_type,
                    checksum,
                    applied_at,
                    applied_by,
                    success
                ) VALUES (
                    @Major,
                    @Minor,
                    @Patch,
                    @Description,
                    @ProviderType,
                    @Checksum,
                    @AppliedAt,
                    @AppliedBy,
                    @Success
                )
            ",
                                           migrations.Select(migration => new
                                                                          {
                                                                              migration.Version.Major,
                                                                              migration.Version.Minor,
                                                                              migration.Version.Patch,
                                                                              migration.Description,
                                                                              ProviderType,
                                                                              migration.Checksum,
                                                                              migration.AppliedAt,
                                                                              migration.AppliedBy,
                                                                              migration.Success
                                                                          }));
            return Result.Success;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public Result<MigraticStatus> CreateStatus(IEnumerable<MigraticHistory> history)
    {
        var appliedMigrations = history.Where(m => m.Success);
        var failedMigrations = history.Where(m => !m.Success);
        return new MigraticStatus
               {
                   History = history, AppliedMigrations = appliedMigrations, FailedMigrations = failedMigrations
               };
    }

    public Result<string> GetCurrentUser()
    {
        try
        {
            var result = _connection.Query<string>("SELECT CURRENT_USER");
            return result.FirstOrDefault();
        }
        catch (Exception e)
        {
            return e;
        }
    }

    public async Task<Result> DropAllTables()
    {
        try
        {
            await _connection.ExecuteAsync($@"
                SELECT 'DROP TABLE IF EXISTS ' || string_agg(oid::regclass::text, ', ') || ' CASCADE'
                FROM pg_class
                WHERE relkind = 'r'
                AND relname !~ '^(pg_|sql_)'
                AND relnamespace = 'public'::regnamespace;
            ");
            return Result.Success;
        }
        catch (Exception e)
        {
            return e;
        }
    }
}
