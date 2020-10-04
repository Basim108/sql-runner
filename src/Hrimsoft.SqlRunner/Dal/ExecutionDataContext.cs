using System.Data;

namespace Hrimsoft.SqlRunner
{
    public class ExecutionDataContext
    {
        public ExecutionDataContext(IDbConnection connection, IDbTransaction transaction)
        {
            Connection  = connection;
            Transaction = transaction;
            State       = ExecutionState.Executing;
        }

        public IDbConnection  Connection  { get; }
        public IDbTransaction Transaction { get; }
        public ExecutionState State       { get; set; }
    }
}