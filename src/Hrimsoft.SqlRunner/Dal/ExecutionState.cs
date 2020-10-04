namespace Hrimsoft.SqlRunner
{
    public enum ExecutionState
    {
        Executing,
        /// <summary> Successfully completed session </summary>
        Completed,
        /// <summary> Transaction has been rolled back </summary>
        Canceled
    }
}