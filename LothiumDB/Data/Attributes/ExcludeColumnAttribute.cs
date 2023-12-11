// System Class
using System;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace LothiumDB.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    sealed public class ExcludeColumnAttribute : ValidationAttribute
    {
        #region Property

        private readonly bool _excluded;

        public bool? IsExcluded { get => _excluded; }

        public override object TypeId => typeof(ExcludeColumnAttribute);

        #endregion

        public ExcludeColumnAttribute() => _excluded = true;

        public override bool IsValid(object value)
        {
            bool result = true;
            if (this.IsExcluded != null && this.IsExcluded == true) result = true;
            return result;
        }

        public override string FormatErrorMessage(string name) => String.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, this.IsExcluded);

    }
}
