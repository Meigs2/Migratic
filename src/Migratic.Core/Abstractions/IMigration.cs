namespace Migratic.Core.Abstractions
{
    public interface IMigration
    {
        MigrationResult Apply();
    }

    public class MigrationResult
    {
        public bool IsSuccess { get; set; }
    }
}