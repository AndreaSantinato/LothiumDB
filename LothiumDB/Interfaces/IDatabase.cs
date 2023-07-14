// System Class
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Custom Class
using LothiumDB;
using LothiumDB.DatabaseExceptions;
using LothiumDB.Enumerations;

namespace LothiumDB.Interfaces
{
    public interface IDatabase
    {
        /// <summary>
        /// Invoke the DB Scalar command in the Database Istance and return a single value of a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public T Scalar<T>(SqlBuilder sql);

        /// <summary>
        /// Invoke the DB NonQuery command in the Database Istance and return the number of completed operations
        /// </summary>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>An int value that count all the affected table rows</returns>
        public int Execute(SqlBuilder sql);

        /// <summary>
        /// Invoke the DB Query command in the Database Istance and cast it to a specific object type
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        public List<T> Query<T>(SqlBuilder sql);

        /// <summary>
        /// Select all the elements inside a table without specify the Sql query
        /// </summary>
        /// <returns>A value based of the object type</returns>
        public List<T> FetchAll<T>();

        /// <summary>
        /// Select a single specific element inside a table
        /// </summary>
        /// <typeparam name="T">Contains the type for the returned object</typeparam>
        /// <param name="sql">Contains the query command to be executed</param>
        /// <param name="args">Contains all the extra arguments of the query</param>
        /// <returns>A value based of the object type</returns>
        /// <returns></returns>
        public T SingleFetch<T>(string sql, params object[] args);

        /// <summary>
        /// Insert the passed object inside a table of the database in the form of a row
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Insert(object obj);

        /// <summary>
        /// Insert the passed object inside a table of the database in the form of a row
        /// </summary>
        /// <param name="table">Contains the name of the table</param>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Insert(string table, object obj);

        /// <summary>
        /// Update a number of element inside a table of the database
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Update(object obj);

        /// <summary>
        /// Update a number of element inside a table of the database
        /// </summary>
        /// <param name="table">Contains the name of the table</param>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Update(string table, object obj);

        /// <summary>
        /// Delete a number of element inside a table of the database
        /// </summary>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Delete(object obj);

        /// <summary>
        /// Delete a number of element inside a table of the database
        /// </summary>
        /// <param name="table">Contains the name of the table</param>
        /// <param name="obj">Contains the object with the db table's mapping</param>
        /// <returns>Return an object that contains the number of affected rows</returns>
        public object Delete(string table, object obj);
    }
}
