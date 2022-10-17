using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Functional.Core;

namespace Migratic.Core;

public class FileBasedMigrationProvider : MigrationProvider
{
    public FileBasedMigrationProvider(MigraticConfiguration configuration) : base(configuration) { }
    public override string ProviderName => "File Based";

    public override async Task<Result<IEnumerable<Migration>>> GetMigrations()
    {
        var result = new List<Migration>();
        
        foreach (var directory in Configuration.SearchPaths)
        {
            var path = Path.IsPathRooted(directory)
                ? directory
                : Path.Combine(Directory.GetCurrentDirectory(), directory);
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The directory {path} does not exist");
            }

            foreach (var file in Directory.EnumerateDirectories(path, "*.*")
                                           // only include files which match the patterns specified in the configuration
                                          .Where(file => Configuration.SearchPatterns.Any(
                                                     pattern => file.EndsWith(pattern))))
            {
                // get the file name
                var fileName = Path.GetFileName(file);
                if (fileName == null) { continue; }

                var migrationType = MigrationType.FromString(fileName, Configuration);
                if (migrationType.IsNone) { continue; }

                var migrationVersion = MigrationVersion.FromString(fileName, Configuration);
                if (migrationVersion.IsNone) { continue; }

                // get the migration script
                var migrationScript = File.ReadAllText(file);

                // create the migration
                var migration = new Migration(migrationType.Value, migrationVersion.Value, fileName, migrationScript);

                result.Add(migration);
            }
        }

        return result;
    }
}
