// System Class
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Configuration;
using System.Drawing.Imaging;
using System.Reflection.Metadata;
using System.Data.Common;
// Custom Class
using Microsoft.Data.SqlClient;
using LothiumDB;
using LothiumDB.Enumerations;
using LothiumDB.Interfaces;
using LothiumDB.Providers;
using LothiumDB.Extensions;

namespace LothiumDB.Helpers
{
    /// <summary>
    /// Helper Class that contains methods used by other class
    /// for complex or specific actions
    /// </summary>
    internal class DatabaseUtility
    {

        /// <summary>
        /// This method retrive the database's provider class from a specific name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IDbProvider InizializeProviderByName(string name)
        {
            IDbProvider provider = null;
            switch (name) 
            {
                case nameof(MSSqlServerProvider):
                    provider = new MSSqlServerProvider();
                    break;
                case nameof(MySqlProvider):
                    provider = new MySqlProvider();
                    break;
            }
            if (provider == null) provider = new MSSqlServerProvider();
            return provider;
        }

        /// <summary>
        /// This method verify if the param passed for the connection string is valid
        /// </summary>
        /// <param name="param">Contains the parameter that need to be verify</param>
        /// <returns></returns>
        public static bool VerifyConnectionStringParameters(object param)
        {
            if (param == null) return true;
            if (String.IsNullOrEmpty((string?)param)) return true;
            return false;
        }

        /// <summary>
        /// Retrieve all the parameter's variables inside a sql query
        /// </summary>
        /// <param name="parVarPrefix">Contains the specific variable prefix to search</param>
        /// <param name="sqlQuery">Contains the actual sql query</param>
        /// <returns></returns>
        public static MatchCollection GetAllQueryParameters(string parVarPrefix, string sqlQuery)
        {
            Regex rxParams = new Regex($@"(?<!{parVarPrefix}){parVarPrefix}\w+", RegexOptions.Compiled);
            return rxParams.Matches(sqlQuery);
        }

        /// <summary>
        /// Generate a new Database Command's Parameter for the selected Database's Provider
        /// </summary>
        /// <param name="provider">Contains the current Database's Provider</param>
        /// <param name="command">Contains the Database Provider's current Command</param>
        /// <param name="args">Contains all the Arguments to add in the Database Command's Parameters</param>
        public static void AddParameters(IDbProvider provider, ref IDbCommand command, object[] args)
        {
            foreach (var match in GetAllQueryParameters(provider.VariablePrefix(), command.CommandText))
            {
                var p = match.ToString();
                int argIndex = (int)Convert.ToInt64(p.Replace("Par", "").Replace(provider.VariablePrefix(), ""));
                command.Parameters.Add(ParameterFormatter(provider, command, $"{p}", args[argIndex]));
            }

            if (command.CommandText.Contains($"{provider.VariablePrefix().ToString()}{provider.VariablePrefix().ToString()}"))
                command.CommandText = command.CommandText.Replace($"{provider.VariablePrefix().ToString()}{provider.VariablePrefix().ToString()}", provider.VariablePrefix().ToString());
        }

        /// <summary>
        /// Format a new parameter with the input data for the selected Database's Provider
        /// </summary>
        /// <param name="provider">Contains the current Database's Provider</param>
        /// <param name="command">Contains the Database Provider's current Command</param>
        /// <param name="name">Contains the Database Parameter's Name</param>
        /// <param name="value">Contains the Database Parameter's Value</param>
        /// <returns></returns>
        private static IDbDataParameter ParameterFormatter(IDbProvider provider, IDbCommand command, string name, object value)
        {
            var par = command.CreateParameter();

            par.ParameterName = name;
            par.Value = value;

            if (value.GetType() == typeof(object))
                par.DbType = DbType.Object;
            else if (value.GetType() == typeof(bool))
                par.DbType = DbType.Boolean;
            else if (value.GetType() == typeof(byte))
                par.DbType = DbType.Byte;
            else if (value.GetType() == typeof(string))
                par.DbType = DbType.String;
            else if (value.GetType() == typeof(short))
                par.DbType = DbType.Int16;
            else if (value.GetType() == typeof(ushort))
                par.DbType = DbType.UInt16;
            else if (value.GetType() == typeof(int))
                par.DbType = DbType.Int32;
            else if (value.GetType() == typeof(uint))
                par.DbType = DbType.UInt32;
            else if (value.GetType() == typeof(long))
                par.DbType = DbType.Int64;
            else if (value.GetType() == typeof(ulong))
                par.DbType = DbType.UInt64;
            else if (value.GetType() == typeof(double))
                par.DbType = DbType.Double;
            else if (value.GetType() == typeof(decimal))
                par.DbType = DbType.Decimal;
            else if (value.GetType() == typeof(Guid))
                par.DbType = DbType.Guid;
            else if (value.GetType() == typeof(DateOnly))
                par.DbType = DbType.Date;
            else if (value.GetType() == typeof(DateTime))
                par.DbType = DbType.DateTime;

            return par;
        }

        /// <summary>
        /// Populate the Params Object with a new parameters object array
        /// </summary>
        /// <param name="args"></param>
        public static object[] AddNewArgsToSqlParamsArray(object[] currArgs, object[] newArgs)
        {
            int argsLenght = 0;
            object[] unifiedArgsArray = null;

            if (newArgs == null || newArgs.Length == 0) return currArgs;
            if (currArgs == null || currArgs.Length == 0) currArgs = new object[0];

            argsLenght = currArgs.Length + newArgs.Length;
            unifiedArgsArray = new object[argsLenght];
            Array.Copy(currArgs, unifiedArgsArray, currArgs.Length);
            Array.Copy(newArgs, 0, unifiedArgsArray, currArgs.Length, newArgs.Length);

            return unifiedArgsArray;
        }

        /// <summary>
        /// Define what is the current sql query's type
        /// </summary>
        /// <param name="sqlQuery">Contains the sql command</param>
        /// <returns>The SQL Query Command's Type</returns>
        public static SqlCommandType DefineSQLCommandType(string sqlQuery)
        {
            if (sqlQuery.Contains("SELECT")) return SqlCommandType.Select;
            if (sqlQuery.Contains("INSERT")) return SqlCommandType.Insert;
            if (sqlQuery.Contains("UPDATE")) return SqlCommandType.Update;
            if (sqlQuery.Contains("DELETE")) return SqlCommandType.Delete;
            return SqlCommandType.None;
        }

        /// <summary>
        /// Create a formatted Sql Command with all the parameters and their values
        /// </summary>
        /// <param name="varPrefix">Contains the sql variable's prefix</param>
        /// <param name="sql">Contains the actual sql query</param>
        /// <param name="args">Contains the actual sql query variable's values</param>
        /// <returns></returns>
        public static string FormatSQLCommandQuery(string varPrefix, string sql, object[] args)
        {
            string formattedSQLQuery = "{0}\n\n/// Query Params ///\n{1}";
            string formattedParameters = String.Empty;
            foreach (var parameter in DatabaseUtility.GetAllQueryParameters(varPrefix, sql))
            {
                var index = (int)Int64.Parse(parameter.ToString().Replace("Par", String.Empty).Replace(varPrefix, String.Empty));
                formattedParameters += $"\n{index}) {parameter.ToString()} = {args[index]}";
            }
            return String.Format(formattedSQLQuery, sql, formattedParameters);
        }
    }
}
