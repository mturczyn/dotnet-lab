namespace IsolationLevelInSqlWithEF;

[Flags]
enum ExampleType
{
    DirtyRead = 1,
    NonRepeatableRead = 2,
    PhantomRead = 4,
    SnapshotIsolation = 8,
    All = 127
}
