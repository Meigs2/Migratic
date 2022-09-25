using System;
using Functional.Core;

namespace Migratic.Core.Models;

public class MigraticHistory
{
    public int Id { get; set; }
    public int Major { get; set; }
    public int? Minor { get; set; }
    public int? Patch { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ProviderType { get; set; }
    public string Checksum { get; set; }
    public DateTime AppliedAt { get; set; }
    public string AppliedBy { get; set; }
    public bool Success { get; set; }
    
    public Option<MigrationVersion> Version => MigrationVersion.From(Major, Minor, Patch);
}
