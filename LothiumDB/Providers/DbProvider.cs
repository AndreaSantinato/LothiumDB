using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LothiumDB.Interfaces;

namespace LothiumDB.DatabaseProviders
{
    public class DbProvider : IDbProvider
    {
        public virtual IDbConnection GenerateConnection(string dbConnString)
        {
            IDbConnection dbConnection = null;
            return dbConnection;
        }

        public virtual IDbCommand GenerateCommand(IDbConnection dbConnection) => dbConnection.CreateCommand();

        public IDbDataParameter GenereateParameter(IDbCommand dbCommand) => dbCommand.CreateParameter();

        public virtual string GenerateConnectionString(params object[] args) => string.Empty;

        public virtual string VariablePrefix() => String.Empty;

        public virtual string BuildPageQuery(string query, long offset, long element)
        {
            string pageQuery = query; 
            pageQuery += $"\n OFFSET {offset} ROWS";
            if (element > 0) pageQuery += $"\n FETCH NEXT {element} ROWS ONLY";
            return pageQuery;
        }
    }
}
