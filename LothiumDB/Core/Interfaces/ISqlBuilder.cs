using LothiumDB.Tools;

namespace LothiumDB.Core.Interfaces;

/// <summary>
/// This interface defines the structure of the Built-In SqlBuilder
/// </summary>
public interface ISqlBuilder
{
    SqlBuilder Append(string value, params object[] args);

    public SqlBuilder Select(string tableName, params object[] tableColumns);

    public SqlBuilder Select(int topElements, string tableName, params object[] tableColumns);
    
    SqlBuilder Where(string sqlClause, params object[] args);
    
    SqlBuilder GroupBy(params object[] args);
    
    SqlBuilder OrderBy(params object[] args);
    
    SqlBuilder InnerJoin(string table, string conditions, params object[] args);
    
    SqlBuilder LeftJoin(string table, string conditions, params object[] args);
    
    SqlBuilder RightJoin(string table, string conditions, params object[] args);
    
    SqlBuilder OuterJoin(SqlBuilder sql);
    
    SqlBuilder LeftOuterJoin(SqlBuilder sql);
    
    SqlBuilder RightOuterJoin(SqlBuilder sql);
    
    SqlBuilder FullOuterJoin(SqlBuilder sql);
    
    SqlBuilder InsertIntoTable(string table, object[] columns, object[] values);
    
    SqlBuilder UpdateTable(string table, Dictionary<string, object> setValues, Dictionary<string, object> whereValues);
    
    SqlBuilder DeleteTable(string table, Dictionary<string, object>? whereValues = null);
}