using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace GraphML.Core
{
    public static class Extensions
    {
        public static string GetPropertyName<T>(this Expression<Func<T, dynamic>> property)
        {
            string propertyName;
            switch (property.Body)
            {
                case UnaryExpression unary:
                    switch (unary.Operand)
                    {
                        case BinaryExpression binaryExpression:
                            propertyName = ((MemberExpression) binaryExpression.Left).Member.Name;
                            break;
                        case MemberExpression memberExpression:
                            propertyName = memberExpression.Member.Name;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                case MemberExpression member:
                    propertyName = member.Member.Name;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return propertyName;
        }

        public static Type GetPropertyType<T>(this Expression<Func<T, dynamic>> property)
        {
            Type propertyType;
            switch (property.Body)
            {
                case UnaryExpression unary:
                    switch (unary.Operand)
                    {
                        case MemberExpression memberExpression:
                            propertyType = ((PropertyInfo)memberExpression.Member).PropertyType; 
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                case MemberExpression member:
                    propertyType = ((PropertyInfo)member.Member).PropertyType; 
                    break;
                default:
                    throw new NotImplementedException();
            }

            return propertyType;
        }

        public static string CreateMD5(this string input)
        {
            // Use input string to calculate MD5 hash
            using var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            var sb = new StringBuilder();
            for (var i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("X2"));
            return sb.ToString();
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(
            this IEnumerable<T> source, int size)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                    bucket = new T[size];

                bucket[count++] = item;

                if (count != size)
                    continue;

                yield return bucket.Select(x => x);

                bucket = null;
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (bucket != null && count > 0)
            {
                Array.Resize(ref bucket, count);
                yield return bucket.Select(x => x);
            }
        }
    }
}