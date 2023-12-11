// System Class
using System;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace LothiumDB.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    sealed public class PrimaryKeyAttribute : ValidationAttribute
    {
        #region Property

        private readonly string _primaryKey;
        private readonly bool _isAutoIncremenetKey;

        public string? PrimaryKey { get { return _primaryKey; } }
        public bool IsAutoIncremenetKey { get { return _isAutoIncremenetKey; } }

        public override object TypeId => typeof(PrimaryKeyAttribute);

        #endregion

        public PrimaryKeyAttribute(string primaryKey, bool isAutoIncremenetKey = false)
        {
            _primaryKey = primaryKey;
            _isAutoIncremenetKey = isAutoIncremenetKey;
        }

        public override bool IsValid(object value)
        {
            var tbName = (String)value;
            bool result = true;
            if (this.PrimaryKey != null && this.PrimaryKey == value) result = true;
            return result;
        }

        public override string FormatErrorMessage(string name) => String.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, this.PrimaryKey);

    }
}
