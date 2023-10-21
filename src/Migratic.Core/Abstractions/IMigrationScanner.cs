using System.Collections.Generic;

namespace Migratic.Core.Abstractions
{
    public interface IMigrationScanner
    {
        IEnumerable<IMigration> Scan();

        Result<MigrationName, string> Parse(string name);
    }
}