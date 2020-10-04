using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.SqlRunner.Models;

namespace Hrimsoft.SqlRunner
{
    public interface ISqlScriptRunner
    {
        /// <summary> Execute sql script </summary>
        Task ExecuteAsync(ScriptInfo scriptInfo, CancellationToken cancellation);
    }
}