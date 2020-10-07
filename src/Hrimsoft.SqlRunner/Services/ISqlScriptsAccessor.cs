using System.Collections.Generic;
using Hrimsoft.SqlRunner.Models;

namespace Hrimsoft.SqlRunner
{
    public interface ISqlScriptsAccessor
    {
        /// <summary>
        /// Creates an ordered sql scripts from all projects. 
        /// </summary>
        /// <returns>Returns an order where the 1st item is the 1st to be run</returns>
        ICollection<ScriptInfo> GetUpScripts();

        /// <summary>
        /// Creates an ordered sql scripts from all projects in down folder. 
        /// </summary>
        /// <returns>Returns an order where the 1st item is the 1st to be run</returns>
        ICollection<ScriptInfo> GetDownScripts();
    }
}