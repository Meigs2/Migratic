using Migratic.Core.Abstractions;

namespace Migratic.Core
{
    public class Migrator
    {
        private readonly IDatabase _database;

        internal Migrator(IDatabase database)
        {
            _database = database;
        }
    }
}