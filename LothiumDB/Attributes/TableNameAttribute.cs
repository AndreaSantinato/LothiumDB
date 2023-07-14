using System;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace LothiumDB.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    sealed public class TableNameAttribute : ValidationAttribute
    {
        #region Property

        private readonly string _tableName;

        public string? Table { get { return _tableName; } }

        public override object TypeId => typeof(TableNameAttribute);

        #endregion

        public TableNameAttribute(string tableName) => _tableName = tableName;

        public override bool IsValid(object value)
        {
            var tbName = (String)value;
            bool result = true;
            if (this.Table != null && this.Table == value) result = true;
            return result;
        }

        public override string FormatErrorMessage(string name) => String.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, this.Table);

    }
}
