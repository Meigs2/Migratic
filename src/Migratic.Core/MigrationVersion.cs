using System;
using Functional.Core;
using static Functional.Core.F;

namespace Migratic.Core;

public record MigrationVersion : IComparable<MigrationVersion>
{
    public int Major { get; init; }
    public Option<int> Minor { get; init; }
    public Option<int> Patch { get; init; }
    public override string ToString() => $"{Major}{"." + Minor}.{"." + Patch}";

    private MigrationVersion(int major, Option<int> minor, Option<int> patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    private static Option<MigrationVersion> From(int major, Option<int> minor, Option<int> patch)
    {
        return Validate(new MigrationVersion(major, minor, patch))
           .Match(Invalid: _ => Option<MigrationVersion>.None, Valid: version => version);
    }

    public static Validation<MigrationVersion> Validate(MigrationVersion migrationVersion)
    {
        return migrationVersion.ToOption()
                               .Match(Some: v => IsValidMajorVersion(v)
                                                .Bind(IsValidMinorVersion)
                                                .Bind(IsValidPatchVersion),
                                      None: () => Invalid("Version cannot be null"));
    }

    private static Validation<MigrationVersion> IsValidMajorVersion(MigrationVersion migrationVersion) =>
        migrationVersion.Major >= 0
            ? Valid(migrationVersion)
            : Invalid($"Major version must be greater than or equal to 0, but was {migrationVersion.Major}");

    private static Validation<MigrationVersion> IsValidMinorVersion(MigrationVersion minor) => minor.Minor.Match(
        Some: m => m >= 0 ? Valid(minor) : Invalid($"Minor version must be greater than or equal to 0, but was {m}"),
        None: () => Valid(minor));

    private static Validation<MigrationVersion> IsValidPatchVersion(MigrationVersion patch) => patch.Patch.Match(
        Some: p => p >= 0 ? Valid(patch) : Invalid($"Patch version must be greater than or equal to 0, but was {p}"),
        None: () => Valid(patch));

    public static Option<MigrationVersion> FromString(string value, MigraticConfiguration configuration)
    {
        try
        {
            var parts = value.Split(configuration.VersionSeparator);
            if (parts == null || parts.Length == 0) return Option.None;
            var major = parts[0].Parse<int>();
            if (major.IsNone) return Option.None;
            var minor = parts.Length > 1 ? parts[1].Parse<int>() : Option.None;
            var patch = parts.Length > 2 ? parts[1].Parse<int>() : Option.None;
            return new MigrationVersion(major.Value, minor, patch);
        }
        catch (Exception _) { return Option.None; }
    }

    // implement operators for greater than, less than, etc.
    // Use helper functions to make it easier to read

    // A version number is greater than another if their internal values are greater, or in
    // the case of one of the values being Option.None, the Version with the more specific value is
    // greater.

    private static Option<bool> IsVersionGreaterThan(Option<int> left, Option<int> right) =>
        left.Match(Some: l => right.Match(Some: r => l > r ? Some(true) :
                                              l == r ? Option.None : Some(false),
                                          None: () => Some(true)),
                   None: () => right.Match(Some: _ => Some(false), None: () => Option.None));

    public static Option<bool> IsGreaterThan(MigrationVersion left, MigrationVersion right) =>
        IsVersionGreaterThan(left.Major, right.Major)
           .Match(Some: isGreaterThan => isGreaterThan ? Some(true) : IsVersionGreaterThan(left.Minor, right.Minor),
                  None: () => IsVersionGreaterThan(left.Minor, right.Minor)
                     .Match(Some: isGreaterThan =>
                                isGreaterThan ? Some(true) : IsVersionGreaterThan(left.Patch, right.Patch),
                            None: () => IsVersionGreaterThan(left.Patch, right.Patch)));

    public static bool operator >(MigrationVersion left, MigrationVersion right) =>
        IsGreaterThan(left, right).GetOrElse(false);

    public static bool operator <(MigrationVersion left, MigrationVersion right) =>
        IsGreaterThan(right, left).GetOrElse(false);

    public static bool operator >=(MigrationVersion left, MigrationVersion right) =>
        IsGreaterThan(left, right).GetOrElse(false) || left == right;

    public static bool operator <=(MigrationVersion left, MigrationVersion right) =>
        IsGreaterThan(right, left).GetOrElse(false) || left == right;

    public static Option<MigrationVersion> From(int major)
    {
        return From(major, Option.None, Option.None);
    }
    
    public static Option<MigrationVersion> From(int major, int minor)
    {
        return From(major, minor, Option.None);
    }
    
    public static Option<MigrationVersion> From(int major, int minor, int patch)
    {
        return From(major, minor.ToOption(), patch.ToOption());
    }
    
    public int CompareTo(MigrationVersion other)
    {
        if (this > other) return 1;
        if (this < other) return -1;
        return 0;
    }

}
