using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AnFood.Infrastructure
{
    public partial class Utils
    {
        public static string GetConfig(string code)
        {

            IConfigurationRoot configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                                         .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                                                                         .AddJsonFile($"appsettings.production.json",
                                                                         optional: true, reloadOnChange: false)
                                                                         .Build();

            var value = configuration[code];
            return value;
        }
        /// <summary>
        /// Check if a string is a guid or not
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        /// 

        public static bool IsGuid(string inputString)
        {
            try
            {
                var guid = new Guid(inputString);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static string GenerateNewRandom()
        {
            Random generator = new Random();
            String r = generator.Next(0, 1000000).ToString("D6");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateNewRandom();
            }
            return r;
        }

        private static readonly string[] VietnameseSigns = new string[]
        {
        "aAeEoOuUiIdDyY ",
        "��??��?????a?????",
        "��??��?????A?????A?",
        "��???�?????",
        "��???�?????",
        "��??��?????o?????",
        "��??��?????O?????",
        "��??uu?????",
        "��??UU?????",
        "��??i",
        "��??I",
        "d",
        "�",
        "�????",
        "�????",
        " "
        };
        public static string RemoveVietnameseSign(string str)
        {

            for (int i = 1; i < VietnameseSigns.Length; i++)
            {

                for (int j = 0; j < VietnameseSigns[i].Length; j++)

                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);

            }

            return str;

        }

        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
