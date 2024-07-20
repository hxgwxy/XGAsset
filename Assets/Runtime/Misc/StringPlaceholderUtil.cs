using System;
using System.Collections.Generic;
using System.Text;

namespace XGAsset.Runtime.Misc
{
    public static class StringPlaceholderUtil
    {
        private static Dictionary<string, string> _cache;

        private static StringBuilder _stringBuilder;

        public static void Add(string key, string value)
        {
            _cache ??= new Dictionary<string, string>();
            _cache[key] = value;
        }

        public static void Remove(string key)
        {
            _cache?.Remove(key);
        }

        public static string Get(string key)
        {
            if (_cache?.ContainsKey(key) ?? false)
            {
                return _cache[key];
            }

            return string.Empty;
        }

        public static void Clear()
        {
            _cache?.Clear();
        }

        public static string GetString(string s)
        {
            _stringBuilder ??= new StringBuilder(240);
            _stringBuilder.Clear();

            if (string.IsNullOrEmpty(s))
                return string.Empty;

            var index = 0;
            while (true)
            {
                var leftIndex = s.IndexOf("{", index, StringComparison.Ordinal);
                if (leftIndex < 0)
                    break;
                var rightIndex = s.IndexOf("}", leftIndex, StringComparison.Ordinal);
                if (rightIndex < 0)
                    break;

                _stringBuilder.Append(s.Substring(index, leftIndex - index));
                var key = s.Substring(leftIndex + 1, rightIndex - leftIndex - 1);
                if (_cache?.ContainsKey(key) ?? false)
                {
                    _stringBuilder.Append(_cache[key]);
                }
                else
                {
                    _stringBuilder.Append(s.Substring(leftIndex, rightIndex - leftIndex + 1));
                }

                index = rightIndex + 1;
            }

            if (index < s.Length)
            {
                _stringBuilder.Append(s.Substring(index, s.Length - index));
            }

            return _stringBuilder.Length == 0 ? s : _stringBuilder.ToString();
        }
    }
}