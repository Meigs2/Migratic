using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Migratic.Core;

public class AssemblyEmbeddedMigrationProvider : MigrationProvider
{
    public AssemblyEmbeddedMigrationProvider(MigraticConfiguration configuration) : base(configuration) { }
    public override string ProviderName => "Assembly Embedded";

    public override IEnumerable<Migration> GetMigrations()
    {
        // the configuration can specify multiple assemblies to search for migrations
        foreach (var assembly in Configuration.EmbeddedResourceMigrationsAssemblies)
        {
            var resources = assembly.GetManifestResourceNames();

            foreach (var resource in resources)
            {
                var resourceName = resource;
                if (resourceName == null) { continue; }

                var migrationType = MigrationType.FromString(resourceName, Configuration);
                if (migrationType.IsNone) { continue; }

                var migrationVersion = MigrationVersion.FromString(resourceName, Configuration);
                if (migrationVersion.IsNone) { continue; }

                var migrationScript = GetResourceString(assembly, resourceName);

                var migration = new Migration(migrationType.Value,
                                              migrationVersion.Value,
                                              GetResourceFileName(resourceName),
                                              migrationScript);

                yield return migration;
            }
        }
    }

    internal static string GetResourceString(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) { return string.Empty; }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal static string GetResourceFileName(string resource)
    {
        string[] parts = resource.Split('.');
        if (parts.Length < 2) { throw new ArgumentException("Invalid resource name", nameof(resource)); }

        return parts[^2] + "." + parts.Last();
    }
}
