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
    /// Throw an exception if the passed value is null
    /// </summary>
    /// <param name="value"></param>
    /// <param name="parameterName"></param>
    internal static void ThrowIfNull([NotNull] object? value, string parameterName = "")
    {
        if (value is null)
            ThrowException(
                string.IsNullOrEmpty(parameterName)
                    ? "The passed object is null!"
                    : $"The object {parameterName} is null!"
            );

    }

    /// <summary>
    /// Throw an exception if the passed value is empty
    /// </summary>
    /// <param name="value"></param>
    /// <param name="parameterName"></param>
    internal static void ThrowIfEmpty([NotNull] string value, string parameterName = "")
    {
        if (value == string.Empty)
        {
            ThrowException(
                string.IsNullOrEmpty(parameterName)
                    ? "The passed string is empty!"
                    : $"The string {parameterName} is empty!"
            );
        }
    }

    /// <summary>
    /// Throw an exception if the passed value is null or empty
    /// </summary>
    /// <param name="value"></param>
    /// <param name="parameterName"></param>
    internal static void ThrowIfNullOrEmpty([NotNull] string value, string parameterName = "")
    {
        ThrowIfNull(value, nameof(value));
        ThrowIfEmpty(value, nameof(value));
    }

    [DoesNotReturn]
    private static void ThrowException(string? errorMessage)
    => throw new DatabaseException(errorMessage);
}
