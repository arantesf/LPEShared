using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;

namespace Revit.Common
{
    public class StringValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value != null && value.ToString() != "")
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "O campo não pode estar vazio");
            }
        }
    }
}
