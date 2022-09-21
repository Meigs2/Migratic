using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Functional.Core;
using MediatR;

namespace Migratic.Core.Commands;

public record ExecuteMigrationCommand : IRequest<Exceptional<Migration>>
{
    public Migration Migration { get; internal set; }
}

internal class ExecuteMigrationCommandHandler : IRequestHandler<ExecuteMigrationCommand, Exceptional<Migration>>
{
    private DbCommand _command;
    public ExecuteMigrationCommandHandler(DbCommand command) { _command = command; }

    public async Task<Exceptional<Migration>> Handle(ExecuteMigrationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await request.Migration.Execute(_command);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}

public abstract class MigrationExecutionBehaviour : IPipelineBehavior<ExecuteMigrationCommand, Exceptional<Migration>>
{
    public async Task<Exceptional<Migration>> Handle(ExecuteMigrationCommand request, CancellationToken cancellationToken, RequestHandlerDelegate<Exceptional<Migration>> next)
    {
        request.Migration = await BeforeMigrationExecution(request.Migration);
        var result = await next();
        await AfterMigrationExecution(result);
        return result;
    }

    public abstract Task<Migration> BeforeMigrationExecution(Migration migration);

    public abstract Task AfterMigrationExecution(Exceptional<Migration> migration);
}

public abstract class BeforeMigrationExecutionBehaviour : MigrationExecutionBehaviour
{
    public sealed override Task AfterMigrationExecution(Exceptional<Migration> migration)
    {
        return Task.CompletedTask;
    }
}

public abstract class AfterMigrationExecutionBehaviour : MigrationExecutionBehaviour
{
    public sealed override Task<Migration> BeforeMigrationExecution(Migration migration)
    {
        return Task.FromResult(migration);
    }
}
