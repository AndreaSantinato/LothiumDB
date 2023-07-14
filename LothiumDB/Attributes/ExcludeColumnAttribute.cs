using System;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace LothiumDB.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    sealed public class ExcludeColumnAttribute : ValidationAttribute
    {
        #region Property

        private readonly bool _isExcluded;

        public bool? isExcluded { get { return _isExcluded; } }

        public override object TypeId => typeof(ExcludeColumnAttribute);

        #endregion

        public ExcludeColumnAttribute() => _isExcluded = true;

        public override bool IsValid(object value)
        {
            bool result = true;
            if (this.isExcluded != null && this.isExcluded == true) result = true;
            return result;
        }

        public override string FormatErrorMessage(string name) => String.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, this.isExcluded);

    }
}
