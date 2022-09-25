using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Functional.Core;
using MediatR;

namespace Migratic.Core.Commands;

public record ExecuteMigrationCommand(Migration Migration) : IRequest<Result<Migration>>
{
    public Migration Migration { get; set; } = Migration;
}

internal class ExecuteMigrationCommandHandler : IRequestHandler<ExecuteMigrationCommand, Result<Migration>>
{
    private DbCommand _command;
    public ExecuteMigrationCommandHandler(DbCommand command) { _command = command; }

    public async Task<Result<Migration>> Handle(ExecuteMigrationCommand request,
        CancellationToken cancellationToken)
    {
        try { return await request.Migration.Execute(_command); }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}

public abstract class MigrationExecutionBehaviour : IPipelineBehavior<ExecuteMigrationCommand, Result<Migration>>
{
    public async Task<Result<Migration>> Handle(ExecuteMigrationCommand request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<Result<Migration>> next)
    {
        request.Migration = await BeforeMigrationExecution(request.Migration);
        var result = await next();
        await AfterMigrationExecution(result);
        return result;
    }

    public abstract Task<Migration> BeforeMigrationExecution(Migration migration);
    public abstract Task AfterMigrationExecution(Result<Migration> migration);
}

public abstract class BeforeMigrationExecutionBehaviour : MigrationExecutionBehaviour
{
    public sealed override Task AfterMigrationExecution(Result<Migration> migration) { return Task.CompletedTask; }
}

public abstract class AfterMigrationExecutionBehaviour : MigrationExecutionBehaviour
{
    public sealed override Task<Migration> BeforeMigrationExecution(Migration migration)
    {
        return Task.FromResult(migration);
    }
}
