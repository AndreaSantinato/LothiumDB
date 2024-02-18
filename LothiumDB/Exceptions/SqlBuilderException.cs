using System.Diagnostics.CodeAnalysis;
using LothiumDB.Tools;

namespace LothiumDB.Exceptions;

/// <summary>
/// Define a SqlBuilder Exception
/// </summary>
internal class SqlBuilderException : DatabaseException
{
    #region Constructors

    public SqlBuilderException() : base() { }

    public SqlBuilderException(string? message) : base(message) { }

    public SqlBuilderException(string? message, SqlBuilderException? innerException) : base(message, innerException) { }

    #endregion Constructors

    /// <summary>
    /// Throw an exception if the passed sql builder is not valid to perform
    /// operations into the database's instance
    /// </summary>
    /// <param name="sqlBuilder">Contains the sql builder to verify</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void ThrowIfBuilderIsNullOrEmpty([NotNull] SqlBuilder? sqlBuilder)
    {
        ThrowIfNull(sqlBuilder, nameof(sqlBuilder));
        ThrowIfSqlNullOrEmpty(sqlBuilder.Query, sqlBuilder.Params);
    }

    /// <summary>
    /// Throw an exception if the passed sql and arguments are not valid to perform
    /// operations into the database's instance
    /// </summary>
    /// <param name="sql">Contains the query to verify</param>
    /// <param name="args">Contains the query's arguments to verify</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void ThrowIfSqlNullOrEmpty([NotNull] string? sql, object[]? args)
    {
        ThrowIfNullOrEmpty(sql!, nameof(sql));

        if (sql.Contains('@') && (args is null || !args.Any())) 
            ThrowException("The providee sql query have declared variables but no passed parameters!");
    }

    [DoesNotReturn]
    private static void ThrowException(string? errorMessage)
        => throw new SqlBuilderException(errorMessage);
}
