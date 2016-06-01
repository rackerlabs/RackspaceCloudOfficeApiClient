using System;
using System.Collections.Generic;
using System.Linq;

namespace Rackspace.CloudOffice
{
    internal static class Extensions
    {
        public static TValue GetCaseInensitive<TValue>(this IDictionary<string, TValue> dict, string key)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));

            if (!dict.ContainsKey(key))
                key = dict.Keys.FirstOrDefault(k => string.Equals(key, k, StringComparison.InvariantCultureIgnoreCase))
                    ?? key;

            return dict[key];
        }
    }
}
