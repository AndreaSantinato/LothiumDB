// System Class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Custom Class
using LothiumDB.Core;
using LothiumDB.Enumerations;

namespace LothiumDB
{
    public interface ISqlBuilder
    {
        SqlBuilder Append(string value, params object[] args);
        SqlBuilder Select(params object[] columns);
        SqlBuilder Select(int topElements, params object[] columns);
        SqlBuilder From(string table);
        SqlBuilder From(params object[] tables);
        SqlBuilder FromNested(SqlBuilder sqlNested);
        SqlBuilder Where(string sqlClause, params object[] args);
        SqlBuilder WhereNested(SqlBuilder sql);
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
}
