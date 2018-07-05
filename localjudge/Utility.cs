using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace localjudge
{
    class ThreadSafeBoolean
    {
        private int _v = 0;

        public bool V
        {
            get { return (Interlocked.CompareExchange(ref _v, 1, 1) == 1); }
            set
            {
                if (value) Interlocked.CompareExchange(ref _v, 1, 0);
                else Interlocked.CompareExchange(ref _v, 0, 1);
            }
        }
    }

    static class Utility
    {
        public static TAttribute GetAttribute<TAttribute>(this Enum value)
            where TAttribute : Attribute
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            return type.GetField(name)
                .GetCustomAttributes(false)
                .OfType<TAttribute>()
                .SingleOrDefault();
        }

        public static T MostCommon<T>(this IEnumerable<T> list)
        {
            return list.GroupBy(i => i).OrderByDescending(group => group.Count())
                .Select(group => group.Key).First();
        }

        public static string GetDatasetConfigPath(JudgeContext judgeContext, string problemId)
        {
            return Path.Combine(judgeContext.dataDirectory, problemId, "config.json");
        }

        private static Random random = new Random();
        public static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
