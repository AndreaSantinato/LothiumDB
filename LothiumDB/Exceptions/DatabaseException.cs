using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace LothiumDB.Exceptions;

/// <summary>
/// Define a DatabaseConfiguration custom exception
/// It provided a set of methods to validate the db's configuration object
/// </summary>
internal class DatabaseException : Exception
{
    #region Constructors

    public DatabaseException() : base() { }

    public DatabaseException(string? message) : base(message) { }

    public DatabaseException(string? message, DatabaseException? innerException) : base(message, innerException) { }

    #endregion Constructors

    /// <summary>
    /// Throw an exception if the passed connection is not valid to perform db's operations
    /// </summary>
    /// <param name="connection">Contains the database context's connection</param>
    /// <exception cref="ArgumentException"></exception>
    internal static void ThrowIfConnectionIsNull([NotNull] IDbConnection? connection)
    {
        if (connection is null)
            ThrowException("The connection must be declared and created to perform database operations!");

        if (string.IsNullOrEmpty(connection.ConnectionString))
            ThrowException("The connection string must be specified to perform database operations!");
    }

    [DoesNotReturn]
    private static void ThrowException(string? errorMessage)
    => throw new DatabaseException(errorMessage);
}
