using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AnFood.Application.Helper
{
    public class HelperProcess
    {
        public static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "file";

            text = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in text)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            text = sb.ToString().Normalize(NormalizationForm.FormC);
            text = Regex.Replace(text, @"[^a-zA-Z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-").Trim('-');

            return text.ToLowerInvariant();
        }

        public static class PhoneValidator
        {
            private static readonly Regex _vnPhoneRegex =
                new Regex(@"^(0|\+84|84)(3|5|7|8|9)[0-9]{8}$",
                          RegexOptions.Compiled);

            public static bool IsValidVietnamPhone(string phone)
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return false;

                phone = phone.Trim();

                return _vnPhoneRegex.IsMatch(phone);
            }
        }
    }
}
