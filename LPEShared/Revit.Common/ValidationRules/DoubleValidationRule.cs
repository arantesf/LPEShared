using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;

namespace Revit.Common
{
    public class DoubleValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (double.TryParse(value.ToString(), out double result))
            {
                if (result != 0)
                {
                    return ValidationResult.ValidResult;
                }
                return new ValidationResult(false, "O valor não pode ser 0");
            }
            else
            {
                return new ValidationResult(false, "Fomato incorreto");
            }
        }
    }
}
