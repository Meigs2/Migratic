using Functional.Core;
using Functional.Core.Enumeration;

namespace Migratic.Core;

public record MigrationType : DynamicEnumeration<MigrationType, string>
{
    public static readonly MigrationType Versioned = new("Versioned", "V");
    public static readonly MigrationType Repeatable = new("Repeatable", "R");
    public static readonly MigrationType Baseline = new("Baseline", "B");
    
    private MigrationType(string name, string value) : base(name, value)
    {
    }

    public static Option<MigrationType> FromString(string text, MigraticConfiguration configuration)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Option.None;
        }

        if (text == configuration.VersionedMigrationPrefix) { return Versioned; }

        if (text == configuration.RepeatableMigrationPrefix) { return Repeatable; }

        if (text == configuration.BaselineMigrationPrefix) { return Baseline; }

        return Option.None;
    }
}
