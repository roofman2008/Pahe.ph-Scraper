using System;

namespace PaheScrapper.Helpers
{
    public static class RuntimeParser
    {
        public static TimeSpan Parse(string str)
        {
            if (String.IsNullOrEmpty(str))
                return TimeSpan.Zero;

            var tokens = str.Replace("~", "").Split(new char[] {' ', '-'}, StringSplitOptions.RemoveEmptyEntries);

            int hours = 0;
            int mins = 0;

            foreach (var token in tokens)
            {
                if (token.ToLower().Contains("h"))
                {
                    var maxIndex = token.IndexOf("h") + 1;
                    var tmpToken = token.Substring(0, maxIndex);

                    tmpToken = tmpToken.Replace("آ¬", "");

                    if (tmpToken != "h" && !String.IsNullOrEmpty(tmpToken))
                        if (!tmpToken.Contains("."))
                            if (int.TryParse(tmpToken, out var @null1))
                                hours = int.Parse(tmpToken.Replace("h", "").TrimStart().TrimEnd());
                        else if (int.TryParse(tmpToken, out var @null2))
                                hours = int.Parse(tmpToken.Replace("h", "").TrimStart().TrimEnd()
                                    .Split(new char[] {'.'})[0]);

                }
                else if (token.ToLower().Contains("mn"))
                {
                    var maxIndex = token.IndexOf("mn") + 2;
                    var tmpToken = token.Substring(0, maxIndex);

                    tmpToken = tmpToken.Replace("آ¬", "");

                    if (tmpToken != "mn" && !String.IsNullOrEmpty(tmpToken))
                        mins = int.Parse(tmpToken.Replace("mn", "").TrimStart().TrimEnd());
                }
            }

            return new TimeSpan(hours, mins, 0);
        }
    }
}