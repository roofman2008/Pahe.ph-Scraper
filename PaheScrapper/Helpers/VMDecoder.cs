using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace PaheScrapper.Helpers
{
    public static class VMDecoderMethods
    {
        public static string[] SplitArray(this string input, string character)
        {
            var tmpSplit = Regex.Split(input, character);
            var tmpResult = new string[tmpSplit.Length - 2];
            Array.Copy(tmpSplit, 1, tmpResult, 0, tmpSplit.Length - 2);
            return tmpResult;
        }

        public static string[] SliceArray(this string[] input, int length)
        {
            var tmpResult = new string[length];
            Array.Copy(input, tmpResult, length);
            return tmpResult;
        }

        public static string[] ReverseArray(this string[] input)
        {
            var tmpResult = input;
            Array.Reverse(tmpResult);
            return tmpResult;
        }

        public static string ReduceArray(this string[] input, Func<string, string, int, string> func, string initValue)
        {
            string tmpValue = initValue;

            for (int currentIndex = 0; currentIndex < input.Length; currentIndex++)
            {
                tmpValue = func(tmpValue, input[currentIndex], currentIndex);
            }

            return tmpValue;
        }
    }

    public static class VMDecoder
    {
        private static string[] _0xc81e = {
            "", "split", "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/", "slice", "indexOf", "",
            "", ".", "pow", "reduce", "reverse", "0"
        };
        
        private static int _0xe12c(string d, int e, int f)
        {
            string[] g = _0xc81e[2].SplitArray(_0xc81e[0]);                       
            string[] h = g.SliceArray(e);                                           
            List<string> hIndexed = h.ToList();
            string[] i = g.SliceArray(f);                                           
            string j = d.SplitArray(_0xc81e[0]).ReverseArray().ReduceArray((a, b, c) =>
            {
                if (hIndexed.IndexOf(b) != -1)
                    return a = (int.Parse(a) + hIndexed.IndexOf(b) * (Math.Pow(e, c))).ToString(CultureInfo.InvariantCulture);
                return a;
            }, "0");
            string k = _0xc81e[0];

            while (int.Parse(j) > 0)
            {
                k = i[int.Parse(j) % f] + k;
                j = ((int.Parse(j) - (int.Parse(j) % f)) / f).ToString();
            }

            return int.Parse(k);
        }

        public static string eval(string h, int u, string n, int t, int e, int r)
        {
            string result = "";
            int len = h.Length;
            for (var i = 0; i < len; i++)
            {
                var s = "";
                while (h[i] != n[e])
                {
                    s += h[i];
                    i++;
                }

                for (var j = 0; j < n.Length; j++)
                    s = s.Replace(n[j], char.Parse(j.ToString()));

                int utf16Value = _0xe12c(s, e, 10) - t;
                result += (char)utf16Value;
            }

            return result;
        }
    }
}