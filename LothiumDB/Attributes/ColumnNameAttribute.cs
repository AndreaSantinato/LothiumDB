// System Class
using System;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace LothiumDB.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    sealed public class ColumnNameAttribute : ValidationAttribute
    {
        #region Property

        private readonly string _columnName;

        public string? Column { get { return _columnName; } }

        public override object TypeId => typeof(ColumnNameAttribute);

        #endregion

        public ColumnNameAttribute(string columnName) => _columnName = columnName;

        public override bool IsValid(object value)
        {
            var tbName = (String)value;
            bool result = true;
            if (this.Column != null && this.Column == value) result = true;
            return result;
        }

        public override string FormatErrorMessage(string name) => String.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, this.Column);

    }
}
