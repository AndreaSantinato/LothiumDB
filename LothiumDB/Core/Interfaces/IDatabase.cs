using LothiumDB.Tools;

namespace LothiumDB.Core.Interfaces;

internal interface IDatabase : IDatabaseCore
{
    /// <summary>
    /// Select all the elements inside a table without specify the Sql query
    /// </summary>
    /// <returns>A value based of the object type</returns>
    public List<T> FindAll<T>();

    /// <summary>
    /// Select all the elements inside a table using a specific Sql query
    /// </summary>
    /// <returns>A value based of the object type</returns>
    public List<T> FindAll<T>(SqlBuilder sql);

    /// <summary>
    /// Select a single specific element inside a table
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="sql">Contains the query command to be executed</param>
    /// <returns>A value based of the object type</returns>
    /// <returns></returns>
    public T FindSingle<T>(SqlBuilder sql);

    /// <summary>
    /// Search inside the database's if a record exist
    /// </summary>
    /// <param name="sql">Contains the SQL object</param>
    /// <returns></returns>
    public bool Exist(SqlBuilder sql);

    /// <summary>
    /// Search inside the database's if a record exist
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns></returns>
    public bool Exist<T>(object obj);

    /// <summary>
    /// Insert the passed object inside a table of the database in the form of a row
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Insert<T>(object obj);

    /// <summary>
    /// Update a number of element inside a table of the database
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Update<T>(object obj);

    /// <summary>
    /// Delete a number of element inside a table of the database
    /// </summary>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Delete<T>(object obj);

    /// <summary>
    /// If an object already exist inside the database will update it, otherwise will cretae it
    /// </summary>
    /// <typeparam name="T">Contains the type for the returned object</typeparam>
    /// <param name="obj">Contains the object with the db table's mapping</param>
    /// <returns>Return an object that contains the number of affected rows</returns>
    public object Save<T>(object obj);
}