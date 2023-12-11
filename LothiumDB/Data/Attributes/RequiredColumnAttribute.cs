// System Class
using System;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace LothiumDB.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    sealed public class RequiredColumnAttribute : ValidationAttribute
    {
        #region Property

        private readonly bool _required;

        public bool? IsRequired { get => _required; }

        public override object TypeId => typeof(RequiredColumnAttribute);

        #endregion

        public RequiredColumnAttribute() => _required = true;

        public override bool IsValid(object value)
        {
            bool result = true;
            if (this.IsRequired != null && this.IsRequired == true) result = true;
            return result;
        }

        public override string FormatErrorMessage(string name) => String.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, this.IsRequired);

    }
}
