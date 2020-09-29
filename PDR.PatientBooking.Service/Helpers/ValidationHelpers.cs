using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PDR.PatientBooking.Service.Helpers
{
    public static class ValidationHelpers
    {
        public static bool Email(string email)
        {
            string validEmailPattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
                      + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
                      + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";

            var emailTest = new Regex(validEmailPattern, RegexOptions.IgnoreCase);
            return emailTest.IsMatch(email);
        }
    }
}
