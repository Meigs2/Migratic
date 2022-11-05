using Meigs2.Functional.Enumeration;

namespace Migratic.Core;

public record TransactionStrategy : Enumeration<TransactionStrategy, int>
{
    public static TransactionStrategy PerMigration { get; } = new(nameof(PerMigration),1);
    public static TransactionStrategy AllOrNothing { get; } = new(nameof(AllOrNothing), 2);

    private TransactionStrategy(string name, int value) : base(name, value)
    {
    }
}
