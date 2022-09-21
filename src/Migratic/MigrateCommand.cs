using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace Migratic;

[Command("migrate", Description = "Attempts to migrate the database.")]
public class MigrateCommand : ICommand
{
    [CommandOption("Connection-String", Description = "Connection string to the database you wish to connect to.", IsRequired = true)]
    public string ConnectionString { get; set; }
    
    [CommandOption("provider",'p', Description = "Database provider.")]
    public string DatabaseType { get; set; }
    
    public ValueTask ExecuteAsync(IConsole console)
    {
        return default;
    }
}
