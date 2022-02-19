using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using TokenManagerProvider;

namespace API_Tracy.Providers
{
    public static class AuthenFunctionProviders
    {
        public const string url_domain = "*";
        //public const string url_domain = "http://103.77.167.60,http://103.77.167.60:222,http://localhost:3000";
        public static bool CheckValidate(IHeaderDictionary headers)
        {
            bool result = false;

            if (headers["access-token"].ToString().Trim() != "" && headers["username"].ToString().Trim() != "")
            {
                string accessToken = headers["access-token"].ToString();
                string username = headers["username"].ToString();

                string[] token_output = TokenManager.ValidateToken(accessToken);
                string username_output = TokenManager.ValidateToken(username)[0];
                if (username_output.Equals(token_output[0]) && DateTime.Now < DateTime.Parse(token_output[1]))
                    result = true;
            }
            return result;
        }
        public static string GenerateSlug(this string phrase)
        {
            string str = phrase.RemoveAccent().ToLower();
            // invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces into one space   
            str = Regex.Replace(str, @"\s+", " ").Trim();
            // cut and trim 
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-"); // hyphens   
            return str;
        }
        public static string RemoveAccent(this string txt)
        {
            StringBuilder sbReturn = new StringBuilder();
            var arrayText = txt.Normalize(NormalizationForm.FormD).ToCharArray();
            foreach (char letter in arrayText)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                    sbReturn.Append(letter);
            }
            return sbReturn.ToString();
        }
    }
}