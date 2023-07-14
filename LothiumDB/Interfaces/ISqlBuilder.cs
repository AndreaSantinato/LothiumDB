// System Class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Custom Class
using LothiumDB;
using LothiumDB.Enumerations;

namespace LothiumDB.Interfaces
{
    public interface ISqlBuilder
    {
        //// <summary>
        /// Append Element to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder Append(string value, params object[] args);

        //// <summary>
        /// Append a Select Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder Select(params object[] args);

        /// <summary>
        /// Append a From Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder From(params object[] args);

        /// <summary>
        /// Append a Where Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder Where(string value, params object[] args);

        /// <summary>
        /// Append a Group By Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder GroupBy(params object[] args);

        /// <summary>
        /// Append an Order By Clause to the final Query
        /// </summary>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder OrderBy(params object[] args);

        /// <summary>
        /// Append an Inner Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder InnerJoin(string tableName, string joinConditions, params object[] args);

        /// <summary>
        /// Append a Left Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder LeftJoin(string tableName, string joinConditions, params object[] args);

        /// <summary>
        /// Append a Right Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder RightJoin(string tableName, string joinConditions, params object[] args);

        /// <summary>
        /// Append an Outer Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder OuterJoin(string tableName, string joinConditions, params object[] args);

        /// <summary>
        /// Append a Left Outer Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder LeftOuterJoin(string tableName, string joinConditions, params object[] args);

        /// <summary>
        /// Append a Right Outer Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder RightOuterJoin(string tableName, string joinConditions, params object[] args);

        /// <summary>
        /// Append a Full Outer Join Clause To the final Query
        /// </summary>
        /// <param name="tableName">Contains the name of the table to join with</param>
        /// <param name="joinConditions">Contains the clause to add in the join</param>
        /// <param name="args">Contains all the arguments to append</param>
        /// <returns>An Sql Objects With the Appended Value to the final Query result</returns>
        SqlBuilder FullOuterJoin(string tableName, string joinConditions, params object[] args);
    }
}
