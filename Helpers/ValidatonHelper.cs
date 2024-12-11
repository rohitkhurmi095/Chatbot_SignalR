using System.Globalization;
using System.Text.RegularExpressions;

namespace TravelInsuranceAdvisor.Helpers
{
    public class ValidatonHelper
    {
        //Date validator
        public bool IsValidDate(string date)
        {
            string dateFormat = "dd/MM/yyyy";
            return !string.IsNullOrWhiteSpace(date) && DateTime.TryParseExact(date, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        //Age validator
        public bool IsValidAge(string age)
        {
            if (string.IsNullOrWhiteSpace(age))
            {
                return false;
            }
            if (int.TryParse(age, out int ageInt))
            {
                return ageInt >= 18 && ageInt <= 75;
            }
            return false;
        }

        //Name validator
        public bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name, @"^[a-zA-Z\s]+$");
        }

        //Email validator
        public bool IsValidEmail(string email)
        {
            try
            {
                var emailAddress = new System.Net.Mail.MailAddress(email);
                return emailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }

        //Phone no. validator
        public bool IsValidPhoneNumber(string phoneNumber)
        {
            // Regular expression for exactly 10 digits
            string pattern = @"^\d{10}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }


        //Calculate Age
        public int CalculateAgeFromDOB(DateTime dob)
        {
            var today = DateTime.Today;
            int age = today.Year - dob.Year;

            if (dob.Date > today.AddYears(-age))
            {
                age--;
            }
            return age;
        }
    }
}
