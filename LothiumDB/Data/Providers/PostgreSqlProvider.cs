// System Class
using System.Data;
// Custom Class
using LothiumDB.Enumerations;
using LothiumDB.Extensions;
using LothiumDB.Interfaces;
// NuGet Packages
using Npgsql;

// Namespace
namespace LothiumDB.Data.Providers;

/// <summary>
/// Defines A Provider For A PostgreSQL Database Instance
/// </summary>
public sealed class PostgreSqlProvider : IDatabaseProvider
{
    public ProviderTypesEnum ProviderType() => ProviderTypesEnum.PostgreSql;

    public string VariablePrefix() => "@";

    public string CreateConnectionString(params object[] args)
    {
        if (!args.Any()) return string.Empty;
        
        return new NpgsqlConnectionStringBuilder()
        {
            Timeout = 30,
            Host = (string)args[0],
            Username = (string)args[1],
            Password = (string)args[2],
            Database = (string)args[3],
            TrustServerCertificate = (bool)args[4]
        }.ConnectionString;
    }

    public IDbConnection CreateConnection(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        return new NpgsqlConnection(connectionString);
    }

    public SqlBuilder BuildPageQuery<T>(PageObject<T> pageObj, SqlBuilder sql)
    {
        throw new NotImplementedException();
    }

    public SqlBuilder CreateAuditTable()
    {
        throw new NotImplementedException();
    }
}