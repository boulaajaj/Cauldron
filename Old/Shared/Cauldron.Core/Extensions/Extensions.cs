﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Linq;

#if WINDOWS_UWP

using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;

#else

using System.Security.Cryptography;

#endif

namespace Cauldron.Core.Extensions
{
    /// <summary>
    /// Provides usefull extension methods
    /// </summary>
    public static class Extensions
    {
        private static readonly Regex _getLinesRegex = new Regex("\r\n|\r|\n", RegexOptions.Compiled);
        private static readonly Regex _parseQueryRegex = new Regex(@"[?|&]([\w\.]+)=([^?|^&]+)", RegexOptions.Compiled);

        /// <summary>
        /// Compresses a utf8 encoded string using gzip
        /// </summary>
        /// <param name="data">The data to be compressed</param>
        /// <returns>The compressed string as byte array</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="data"/> is empty</exception>
        public static byte[] Compress(this string data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data == "")
                throw new ArgumentException("data cannot be empty");

            return Compression.Compress(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Compresses data using gzip
        /// </summary>
        /// <param name="data">The data to compress</param>
        /// <returns>The compressed data in bytes</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is null</exception>
        public static byte[] Compress(this byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return Compression.Compress(data);
        }

        /// <summary>
        /// Concatenates a jagged array to a one-dimensional array
        /// </summary>
        /// <typeparam name="T">The element type of the array</typeparam>
        /// <param name="arrays">The jagged array</param>
        /// <returns>An one dimensional array</returns>
        public static T[] Concat<T>(this T[][] arrays)
        {
            var result = Array.CreateInstance(typeof(T), arrays.Sum(x => x.Length));
            int offset = 0;

            for (int i = 0; i <= arrays.Length - 1; i++)
            {
                System.Buffer.BlockCopy(arrays[i], 0, result, offset, arrays[i].Length);
                offset += arrays[i].Length;
            }

            return (T[])result;
        }

        /// <summary>
        /// Concats an item to an array creating a new array containing the original array and the item.
        /// </summary>
        /// <typeparam name="T">The element type of the array</typeparam>
        /// <param name="arrayA">The array</param>
        /// <param name="item">The item to add to the array</param>
        /// <returns>A new array containing the original array and the item.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="arrayA"/> is null</exception>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
        public static T[] Concat<T>(this T[] arrayA, T item)
        {
            if (arrayA == null)
                throw new ArgumentNullException(nameof(arrayA));

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var result = Array.CreateInstance(typeof(T), arrayA.Length + 1);

            if (arrayA.Length > 0)
                Array.Copy(arrayA, 0, result, 0, arrayA.Length);
            result.SetValue(item, result.Length - 1);

            return (T[])result;
        }

        /// <summary>
        /// Concatenates two arrays together creating a new array containing both arrays
        /// </summary>
        /// <typeparam name="T">The element type of the array</typeparam>
        /// <param name="arrayA">The first array</param>
        /// <param name="arrayB">The second array</param>
        /// <returns>A new array containing both arrays.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="arrayA"/> is null</exception>
        /// <exception cref="ArgumentNullException"><paramref name="arrayB"/> is null</exception>
        public static T[] Concat<T>(this T[] arrayA, T[] arrayB)
        {
            if (arrayA == null)
                throw new ArgumentNullException(nameof(arrayA));

            if (arrayB == null)
                throw new ArgumentNullException(nameof(arrayB));

            var result = Array.CreateInstance(typeof(T), arrayA.Length + arrayB.Length);

            if (arrayA.Length > 0)
                Array.Copy(arrayA, 0, result, 0, arrayA.Length);
            if (arrayB.Length > 0)
                Array.Copy(arrayB, 0, result, arrayA.Length, arrayB.Length);

            return (T[])result;
        }

        /// <summary>
        /// Returns a value indicating whether a specified substring occurs within this string.
        /// </summary>
        /// <param name="text">The string to seek from.</param>
        /// <param name="value">The string to seek.</param>
        /// <param name="comparisonType">
        /// One of the enumeration values that specifies the rules for the search.
        /// </param>
        /// <returns>
        /// True if the value parameter occurs within this string, or if value is the empty string
        /// (""); otherwise, false.
        /// </returns>
        public static bool Contains(this string text, string value, StringComparison comparisonType) =>
            text.IndexOf(value, comparisonType) >= 0;

        /// <summary>
        /// Copies an array.
        /// </summary>
        /// <typeparam name="T">The element type of the array</typeparam>
        /// <param name="array">The array</param>
        /// <returns>A new array that contains the same elements as <paramref name="array"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null</exception>
        public static T[] Copy<T>(this T[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            var result = Array.CreateInstance(typeof(T), array.Length);
            Array.Copy(array, result, array.Length);
            return (T[])result;
        }

        /// <summary>
        /// Creates a new instance of System.String with the same value as a specified System.String.
        /// </summary>
        /// <param name="value">The string to copy.</param>
        /// <returns>A new string with the same value as str.</returns>
        public static string Copy(this string value)
        {
            if (value == null)
                return null;

#if WINDOWS_UWP || NETCORE
            if (value == "")
                return "";

            char[] result = new char[value.Length];
            value.CopyTo(0, result, 0, value.Length);
            return new string(result);
#else
            return string.Copy(value);
#endif
        }

        /// <summary>
        /// Returns distinct elements from a sequence by using a selector to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="selector">
        /// An expression used to determines whether the specified object are equal
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> that contains distinct elements from the source sequence.
        /// </returns>
        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, bool> selector)
        {
            var comparer = new DynamicEqualityComparer<TSource>(selector);
            return source.Distinct(comparer);
        }

        /// <summary>
        /// Gets the string enclosed by two strings
        /// </summary>
        /// <param name="target">The string that contains the string to be retrieved</param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>The enclosed string</returns>
        public static string EnclosedIn(this string target, string start = "(", string end = ")")
        {
            if (string.IsNullOrEmpty(target))
                return target;

            int startPos = target.IndexOf(start) + start.Length;

            if (startPos < 0)
                return target;

            int endPos = target.IndexOf(end, startPos + 1);

            if (endPos <= startPos)
                endPos = target.Length - 1;

            return target.Substring(startPos, endPos - startPos);
        }

        /// <summary>
        /// Replaces the values of data in memory with random values. The GC handle will be freed.
        /// </summary>
        /// <remarks>Will only work on <see cref="GCHandleType.Pinned"/></remarks>
        /// <param name="target"></param>
        /// <param name="targetLength"></param>
        public static void FillWithRandomValues(this GCHandle target, int targetLength)
        {
            unsafe
            {
                byte* insecureData = (byte*)target.AddrOfPinnedObject();

                for (int i = 0; i < targetLength; i++)
                    insecureData[i] = Randomizer.NextByte();

                target.Free();
            }
        }

        /// <summary>
        /// Performs the specified action on each element of the <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">The type that is contained in the <see cref="IEnumerable{T}"/></typeparam>
        /// <param name="collection">The collection to perform the action on</param>
        /// <param name="action">
        /// The <see cref="Action{T}"/> delegate to perform on each element of the <see cref="IEnumerable{T}"/>.
        /// </param>
        /// <returns>Returns <paramref name="collection"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null</exception>
        public static IEnumerable<T> Foreach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (collection == null)
                return null;

            foreach (var item in collection)
                action(item);

            return collection;
        }

        /// <summary>
        /// Gets a specified length of bytes.
        /// <para/>
        /// If the specified length <paramref name="length"/> is longer than the source array the
        /// source array will be returned instead.
        /// </summary>
        /// <param name="target">The Array that contains the data to copy.</param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>
        /// <returns>Returns an array of bytes</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="length"/> is 0</exception>
        public static byte[] GetBytes(this byte[] target, uint length)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (length == 0)
                throw new ArgumentException("length cannot be 0");

            if (length >= target.Length)
                return target;

            byte[] value = new byte[length];

            Array.Copy(target, value, (int)length);
            return value;
        }

        /// <summary>
        /// Gets a specified length of bytes
        /// </summary>
        /// <param name="target">The Array that contains the data to copy.</param>
        /// <param name="startingPosition">
        /// A 32-bit integer that represents the index in the sourceArray at which copying begins.
        /// </param>
        /// <param name="length">A 32-bit integer that represents the number of elements to copy.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Parameter <paramref name="startingPosition"/> and <paramref name="length"/> are out of range
        /// </exception>
        public static byte[] GetBytes(this byte[] target, int startingPosition, int length)
        {
            if (length + startingPosition > target.Length)
                throw new ArgumentOutOfRangeException("length", "Parameter startingPosition and length are out of range");

            byte[] value = new byte[length];

            Array.Copy(target, startingPosition, value, 0, length);
            return value;
        }

        /// <summary>
        /// Get the hash representing the string. The hash algorithm used is <see cref="HashAlgorithms.Md5"/>
        /// </summary>
        /// <param name="target">The string to hash</param>
        /// <returns>The hash value</returns>
        public static string GetHash(this string target) => target.GetHash(HashAlgorithms.Md5);

        /// <summary>
        /// Get the hash representing the string
        /// </summary>
        /// <param name="target">The string to hash</param>
        /// <param name="algorithm">The hash algortihm to use</param>
        /// <returns>The hash value</returns>
        public static string GetHash(this string target, HashAlgorithms algorithm)
        {
#if WINDOWS_UWP
            if (algorithm == HashAlgorithms.Md5)
            {
                var md5 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
                var buffer = CryptographicBuffer.ConvertStringToBinary(target, BinaryStringEncoding.Utf8);
                var hashed = md5.HashData(buffer);
                return CryptographicBuffer.EncodeToHexString(hashed);
            }
            else if (algorithm == HashAlgorithms.Sha512 || algorithm == HashAlgorithms.Sha256)
            {
                var sha = HashAlgorithmProvider.OpenAlgorithm(algorithm == HashAlgorithms.Sha512 ? HashAlgorithmNames.Sha256 : HashAlgorithmNames.Sha512);
                var buffer = CryptographicBuffer.ConvertStringToBinary(target, BinaryStringEncoding.Utf8);

                var hashed = sha.HashData(buffer);
                byte[] bytes;

                CryptographicBuffer.CopyToByteArray(hashed, out bytes);
                return Convert.ToBase64String(bytes);
            }
            else
                throw new NotSupportedException("Unsupported hash algorithm");

#elif NETCORE
            return UTF8Encoding.UTF8.GetBytes(target).GetHash(algorithm);

#else
            return UTF8Encoding.UTF8.GetBytes(target).GetHash(algorithm);

#endif
        }

        /// <summary>
        /// Get the hash representing the string
        /// </summary>
        /// <param name="target">The bytes array to hash</param>
        /// <param name="algorithm">The hash algortihm to use</param>
        /// <returns>The hash value</returns>
        public static string GetHash(this byte[] target, HashAlgorithms algorithm)
        {
#if WINDOWS_UWP
            if (algorithm == HashAlgorithms.Md5)
            {
                var md5 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
                var buffer = CryptographicBuffer.CreateFromByteArray(target);
                var hashed = md5.HashData(buffer);
                return CryptographicBuffer.EncodeToHexString(hashed);
            }
            else if (algorithm == HashAlgorithms.Sha512 || algorithm == HashAlgorithms.Sha256)
            {
                var sha = HashAlgorithmProvider.OpenAlgorithm(algorithm == HashAlgorithms.Sha512 ? HashAlgorithmNames.Sha256 : HashAlgorithmNames.Sha512);
                var buffer = CryptographicBuffer.CreateFromByteArray(target);

                var hashed = sha.HashData(buffer);
                byte[] bytes;

                CryptographicBuffer.CopyToByteArray(hashed, out bytes);
                return Convert.ToBase64String(bytes);
            }
            else
                throw new NotSupportedException("Unsupported hash algorithm");
#elif NETCORE

            if (algorithm == HashAlgorithms.Md5)
                using (var md5 = MD5.Create())
                    return BitConverter.ToString(md5.ComputeHash(target)).Replace("-", "");
            else if (algorithm == HashAlgorithms.Sha256)
                using (var sha = SHA256.Create())
                    return Convert.ToBase64String(sha.ComputeHash(target));
            else if (algorithm == HashAlgorithms.Sha512)
                using (var sha = SHA512.Create())
                    return Convert.ToBase64String(sha.ComputeHash(target));
            else
                throw new NotSupportedException("Unsupported hash algorithm");
#else
            if (algorithm == HashAlgorithms.Md5)
                using (var md5 = new MD5CryptoServiceProvider())
                    return BitConverter.ToString(md5.ComputeHash(target)).Replace("-", "");
            else if (algorithm == HashAlgorithms.Sha256)
                using (var sha = SHA256.Create())
                    return Convert.ToBase64String(sha.ComputeHash(target));
            else if (algorithm == HashAlgorithms.Sha512)
                using (var sha = SHA512.Create())
                    return Convert.ToBase64String(sha.ComputeHash(target));
            else
                throw new NotSupportedException("Unsupported hash algorithm");

#endif
        }

        /// <summary>
        /// Splits a string into lines
        /// </summary>
        /// <param name="value">The string to be slitted</param>
        /// <returns>The lines of the string</returns>
        public static string[] GetLines(this string value) => _getLinesRegex.Split(value);

        /// <summary>
        /// Gets the stacktrace of the exception and the inner exceptions recursively
        /// </summary>
        /// <param name="e">The exception with the stack trace</param>
        /// <returns>A string representation of the stacktrace</returns>
        public static string GetStackTrace(this Exception e)
        {
            var sb = new StringBuilder();
            var ex = e;

            do
            {
                sb.AppendLine("Exception Type: " + ex.GetType().Name);
                sb.AppendLine("Source: " + ex.Source);
                sb.AppendLine(ex.Message);
                sb.AppendLine("------------------------");
                sb.AppendLine(ex.StackTrace);
                sb.AppendLine("------------------------");

                ex = ex.InnerException;
            } while (ex != null);

            return ApplicationInfo.ApplicationName + "\r\n" + sb.ToString();
        }

        /// <summary>
        /// Retrieves the target object referenced by the current <see cref="WeakReference{T}"/> object
        /// <para/>
        /// Returns null if the target is not available
        /// </summary>
        /// <typeparam name="T">The type of the object referenced.</typeparam>
        /// <param name="weakReference">The current <see cref="WeakReference{T}"/> object</param>
        /// <returns>Contains the target object, if it is available; otherwise null</returns>
        public static T GetTarget<T>(this WeakReference<T> weakReference) where T : class
        {
            if (weakReference == null)
                return null;

            T value;
            if (weakReference.TryGetTarget(out value))
                return value;

            return null;
        }

        /// <summary>
        /// Retrieves the types of the object array
        /// </summary>
        /// <param name="objs">The object array to get the type from</param>
        /// <returns>An array of types that represents the object array</returns>
        public static Type[] GetTypes(this object[] objs)
        {
            if (objs == null || objs.Length == 0)
                return new Type[0];

            var types = new Type[objs.Length];

            for (int i = 0; i < objs.Length; i++)
                types[i] = objs[i] == null ? typeof(object) : objs[i].GetType();

            return types;
        }

        /// <summary>
        /// Searches for the specified byte array and returns the zero-based index of the first
        /// occurrence within the entire <see cref="Array"/>
        /// </summary>
        /// <param name="data">The <see cref="Array"/> that could contain <paramref name="value"/></param>
        /// <param name="value">
        /// The object to locate in the <see cref="Array"/>. The value can be null for reference types.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of item within the entire <see
        /// cref="Array"/>, if found; otherwise, –1.
        /// </returns>
        public static long IndexOf(this byte[] data, byte[] value)
        {
            if (value.Length > data.Length)
                return -1;

            unsafe
            {
                var dataLength = data.Length;
                var findLength = value.Length;
                var unequal = false;

                fixed (byte* pData = data, pFind = value)
                {
                    byte* dataPointer = pData;
                    byte* findPointer = pFind;

                    do
                    {
                        byte* currentDataPointer = dataPointer;

                        do
                        {
                            if (*currentDataPointer != *findPointer)
                            {
                                unequal = true;
                                break;
                            }

                            findPointer++;
                            currentDataPointer++;
                        }
                        while (--findLength > 0);

                        if (!unequal)
                            return dataPointer - pData;

                        unequal = false;
                        // reset the length
                        findLength = value.Length;
                        // move the pointer back to the next byte
                        dataPointer++;
                        // reset the find pointer
                        findPointer = pFind;
                    }
                    while (--dataLength > findLength);
                }
            }

            return -1;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the entire <see cref="Array"/>
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Array"/></typeparam>
        /// <param name="target">The <see cref="Array"/> that could contain <paramref name="value"/></param>
        /// <param name="value">
        /// The object to locate in the <see cref="Array"/>. The value can be null for reference types.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of item within the entire <see
        /// cref="Array"/>, if found; otherwise, –1.
        /// </returns>
        public static int IndexOf<T>(this T[] target, T value)
        {
            for (int i = 0; i < target.Length; i++)
                if (target[i].Equals(value))
                    return i;

            return -1;
        }

        /// <summary>
        /// Checkes if the string is encoded in Base64.
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <returns>True if the string is base 64 encoded; otherwise false</returns>
        public static bool IsBase64String(this string str)
        {
            if (str.Replace(" ", "").Length % 4 != 0)
                return false;

            try
            {
                Convert.FromBase64String(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the value is null. If not, it will invoke <paramref name="action"/>
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="value">The value to check</param>
        /// <param name="action">The action to invoke if <paramref name="value"/> is not null</param>
        /// <returns>the instance of the value</returns>
        public static T IsNotNull<T>(this T value, Action<T> action) where T : class
        {
            if (value != null)
            {
                action(value);
                return value;
            }

            return null;
        }

        /// <summary>
        /// Checks if the value is null. If not, it will invoke <paramref name="func"/>
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <typeparam name="TResult">The return type</typeparam>
        /// <param name="value">The value to check</param>
        /// <param name="func">The func to invoke if <paramref name="value"/> is not null</param>
        /// <returns>The result of <paramref name="value"/></returns>
        public static TResult IsNotNull<T, TResult>(this T value, Func<T, TResult> func) where T : class
        {
            if (value != null)
                return func(value);

            return default(TResult);
        }

        /// <summary>
        /// Concatenates the members of a constructed <see cref="IEnumerable{T}"/> collection of type
        /// <see cref="string"/>, using the specified <paramref name="separator"/> between each member.
        /// </summary>
        /// <param name="source">A collection that contains the strings to concatenate.</param>
        /// <param name="separator">
        /// The string to use as a <paramref name="separator"/>. <paramref name="separator"/> is
        /// included in the returned string only if values has more than one element.
        /// </param>
        /// <returns>
        /// A string that consists of the members of values delimited by the <paramref
        /// name="separator"/> string. If values has no members, the method returns <see cref="string.Empty"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static string Join(this IEnumerable<string> source, string separator) => string.Join(separator, source);

        /// <summary>
        /// Concatenates the elements of an object array, using the specified <paramref
        /// name="separator"/> between each element.
        /// </summary>
        /// <param name="source">An array that contains the elements to concatenate.</param>
        /// <param name="separator">
        /// The string to use as a <paramref name="separator"/>. <paramref name="separator"/> is
        /// included in the returned string only if values has more than one element.
        /// </param>
        /// <returns>
        /// A string that consists of the elements of values delimited by the <paramref
        /// name="separator"/> string. If values is an empty array, the method returns <see cref="string.Empty"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static string Join(this object[] source, string separator) => string.Join(separator, source);

        /// <summary>
        /// Concatenates the members of a collection, using the specified <paramref
        /// name="separator"/> between each member.
        /// </summary>
        /// <param name="source">A collection that contains the objects to concatenate.</param>
        /// <param name="separator">
        /// The string to use as a <paramref name="separator"/>. <paramref name="separator"/> is
        /// included in the returned string only if values has more than one element.
        /// </param>
        /// <returns>
        /// A string that consists of the members of values delimited by the <paramref
        /// name="separator"/> string. If values has no members, the method returns <see cref="string.Empty"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static string Join<T>(this IEnumerable<T> source, string separator) => string.Join(separator, source);

        /// <summary>
        /// Concatenates all the elements of a string array, using the specified <paramref
        /// name="separator"/> between each element.
        /// </summary>
        /// <param name="source">An array that contains the elements to concatenate.</param>
        /// <param name="separator">
        /// The string to use as a <paramref name="separator"/>. <paramref name="separator"/> is
        /// included in the returned string only if values has more than one element.
        /// </param>
        /// <returns>
        /// A string that consists of the elements in value delimited by the <paramref
        /// name="separator"/> string. If value is an empty array, the method returns <see cref="string.Empty"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static string Join(this string[] source, string separator) => string.Join(separator, source);

        /// <summary>
        /// Returns a string containing a specified number of characters from the left side of a string.
        /// </summary>
        /// <param name="source">
        /// <see cref="string"/> expression from which the leftmost characters are returned.
        /// </param>
        /// <param name="length">
        /// Numeric expression indicating how many characters to return. If 0, a zero-length string (
        /// <see cref="string.Empty"/>) is returned. If greater than or equal to the number of
        /// characters in str, the entire string is returned.
        /// </param>
        /// <returns>
        /// Returns a string containing a specified number of characters from the left side of a string.
        /// </returns>
        public static string Left(this string source, int length)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            if (length == 0)
                return string.Empty;

            if (source.Length < length || source.Length == length)
                return source;

            return source.Substring(0, length);
        }

        /// <summary>
        /// Replaces the first char of a string against a lower cased char
        /// </summary>
        /// <param name="target">The string to replace</param>
        /// <returns>Returns a new string with a lower cased first character</returns>
        public unsafe static string LowerFirstCharacter(this string target)
        {
            if (target == null)
                return null;

            if (target == "")
                return "";

            if (target.Length == 1)
                return target.ToLower();

            var result = target.Copy();

            fixed (char* chr = result)
                *chr = char.ToLower(*chr);

            return result;
        }

        /// <summary>
        /// Moves the specified item to a new location in the collection
        /// </summary>
        /// <typeparam name="T">The Type of item contained in the collection</typeparam>
        /// <param name="source">The source collection that contains the item</param>
        /// <param name="entry">The item to move</param>
        /// <param name="relativeIndex">The new position of the item relativ to its current position.</param>
        public static void Move<T>(this ObservableCollection<T> source, T entry, int relativeIndex)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var entryIndex = source.IndexOf(entry);
            var newIndex = entryIndex + relativeIndex;

            if (newIndex < 0)
                newIndex = 0;

            if (newIndex > source.Count - 1)
                newIndex = source.Count - 1;

            if (entryIndex == newIndex)
                return;

            source.Move(entryIndex, newIndex);
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending order according to a key.
        /// </summary>
        /// <param name="array">A sequence of values to order.</param>
        /// <typeparam name="TElements">Der Typ der Elemente von source.</typeparam>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>
        /// An <see cref="IOrderedEnumerable{TElement}"/> whose elements are sorted according to a key.
        /// </returns>
        public static IOrderedEnumerable<TElements> OrderBy<TElements>(this Array array, Func<TElements, bool> keySelector) =>
            array.Cast<TElements>().OrderBy(keySelector);

        /// <summary>
        /// Parses a query string into a NameValueCollection using UTF8 encoding.
        /// </summary>
        /// <param name="uri">The uri to parse.</param>
        /// <returns>A dictionary of query parameters and values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is null</exception>
        public static ReadOnlyDictionary<string, string> ParseQueryString(this Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            var match = _parseQueryRegex.Match(uri.PathAndQuery);
            var parameters = new Dictionary<string, string>();
            while (match.Success)
            {
                parameters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return parameters.AsReadOnly();
        }

        /// <summary>
        /// Picks a random element from the given array
        /// </summary>
        /// <typeparam name="T">The type of element in the array</typeparam>
        /// <param name="array">The array to pick a random element from</param>
        /// <returns>The randomly picked element</returns>
        public static T RandomPick<T>(this T[] array) => array[Randomizer.Next(0, array.Length - 1)];

        /// <summary>
        /// Reads all characters from the current position to the end of the stream.
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <returns>
        /// The rest of the stream as a string, from the current position to the end. If the current
        /// position is at the end of the stream, returns an empty string ("").
        /// </returns>
        /// <exception cref="OutOfMemoryException">
        /// There is insufficient memory to allocate a buffer for the returned string.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public static string ReadToEnd(this Stream stream, Encoding encoding)
        {
            using (StreamReader reader = new StreamReader(stream, encoding))
                return reader.ReadToEnd();
        }

        /// <summary>
        /// Reads all characters from the current position to the end of the stream.
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <returns>
        /// The rest of the stream as a string, from the current position to the end. If the current
        /// position is at the end of the stream, returns an empty string ("").
        /// </returns>
        /// <exception cref="OutOfMemoryException">
        /// There is insufficient memory to allocate a buffer for the returned string.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        public static string ReadToEnd(this Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        /// <summary>
        /// Removes all null elements from the array.
        /// </summary>
        /// <typeparam name="T">The element type of the array</typeparam>
        /// <param name="array">The array</param>
        /// <returns>A new array with all non null elements</returns>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null</exception>
        public static T[] RemoveNull<T>(this T[] array) where T : class
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            var newSize = 0;

            for (int i = 0; i < array.Length; i++)
                if (array[i] != null)
                    newSize++;

            var newArray = new T[newSize];
            var counter = 0;

            for (int i = 0; i < array.Length; i++)
                if (array[i] != null)
                    newArray[counter++] = array[i];

            return newArray;
        }

        /// <summary>
        /// Replaces a series of chars <paramref name="oldChars"/> with a single char <paramref name="newChar"/>.
        /// </summary>
        /// <param name="value">The string with the chars to replace</param>
        /// <param name="oldChars">The old chars to be replaced by <paramref name="newChar"/></param>
        /// <param name="newChar">The new char that replaces the old chars</param>
        /// <param name="startingIndex">The index where to start replacing chars</param>
        /// <returns>
        /// A copy of the original string with the chars defined by <paramref name="oldChars"/>
        /// replaced by <paramref name="newChar"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">value is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startingIndex"/> is higher than <paramref name="value"/> length
        /// </exception>
        public static string Replace(this string value, char[] oldChars, char newChar, int startingIndex = 0)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value.Length < startingIndex)
                throw new ArgumentOutOfRangeException("startingIndex cannot be higher than value length");

            if (value.Length == 0)
                return "";

            var result = value.Copy();
            result.ReplaceMe(oldChars, newChar, startingIndex);
            return result;
        }

        /// <summary>
        /// Replaces a char in the given index with <paramref name="newChar"/>
        /// </summary>
        /// <param name="value">The string to replace the char</param>
        /// <param name="index">The index of the char</param>
        /// <param name="newChar">The new char</param>
        /// <exception cref="ArgumentNullException">value is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is higher than <paramref name="value"/> length
        /// </exception>
        public unsafe static void Replace(this string value, uint index, char newChar)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value.Length < index)
                throw new ArgumentOutOfRangeException("index cannot be higher than value length");

            if (value.Length == 0)
                return;

            fixed (char* chr = value)
                *(chr + index) = newChar;
        }

        /// <summary>
        /// Replaces a series of chars <paramref name="oldChars"/> with a single char <paramref name="newChar"/>.
        /// <para/>
        /// ATTENTION: The original string is the target of the manipulation.
        /// </summary>
        /// <param name="value">The string with the chars to replace</param>
        /// <param name="oldChars">The old chars to be replaced by <paramref name="newChar"/></param>
        /// <param name="newChar">The new char that replaces the old chars</param>
        /// <param name="startingIndex">The index where to start replacing chars</param>
        /// <exception cref="ArgumentNullException">value is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startingIndex"/> is higher than <paramref name="value"/> length
        /// </exception>
        public unsafe static void ReplaceMe(this string value, char[] oldChars, char newChar, int startingIndex = 0)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value.Length < startingIndex)
                throw new ArgumentOutOfRangeException("startingIndex cannot be higher than value length");

            if (value.Length == 0)
                return;

            fixed (char* chr = value)
                for (int i = startingIndex; i < value.Length; i++)
                {
                    var valueChar = *(chr + i);
                    for (int x = 0; x < oldChars.Length; x++)
                        if (valueChar == oldChars[x])
                            *(chr + i) = newChar;
                }
        }

        /// <summary>
        /// Returns a string containing a specified number of characters from the right side of a string.
        /// </summary>
        /// <param name="source">
        /// <see cref="string"/> expression from which the rightmost characters are returned.
        /// </param>
        /// <param name="length">
        /// Numeric expression indicating how many characters to return. If 0, a zero-length string (
        /// <see cref="string.Empty"/>) is returned. If greater than or equal to the number of
        /// characters in str, the entire string is returned.
        /// </param>
        /// <returns>
        /// Returns a string containing a specified number of characters from the right side of a string.
        /// </returns>
        public static string Right(this string source, int length)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            if (length == 0)
                return string.Empty;

            if (source.Length < length || source.Length == length)
                return source;

            return source.Substring(source.Length - length, length);
        }

        /// <summary>
        /// Runs the <see cref="Task"/> synchronously on the default <see cref="TaskScheduler"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result produced by this <see cref="Task"/></typeparam>
        /// <param name="task">The task instance</param>
        /// <returns>The value returned by the function</returns>
        public static TResult RunSync<TResult>(this Task<TResult> task) => AsyncHelper.RunSync(() => task);

        /// <summary>
        /// Runs the <see cref="Task"/> synchronously on the default <see cref="TaskScheduler"/>.
        /// </summary>
        /// <param name="task">The task instance</param>
        public static void RunSync(this Task task) => AsyncHelper.RunSync(() => task);

        /// <summary>
        /// Returns the elements of the first dimension of a multidimensional array
        /// </summary>
        /// <typeparam name="T">The type that is contained in the array</typeparam>
        /// <param name="array">The array to get the dimension from</param>
        /// <param name="column">The second dimension of the array</param>
        /// <returns>The second dimension of the array depending on the <paramref name="column"/></returns>
        public static IEnumerable<T> SliceColumn<T>(this T[,] array, int column)
        {
            for (int i = array.GetLowerBound(0); i <= array.GetUpperBound(0); i++)
                yield return array[i, column];
        }

        /// <summary>
        /// Returns the elements of the second dimension of a multidimensional array
        /// </summary>
        /// <typeparam name="T">The type that is contained in the array</typeparam>
        /// <param name="array">The array to get the dimension from</param>
        /// <param name="row">The first dimension of the array</param>
        /// <returns>The second dimension of the array depending on the <paramref name="row"/></returns>
        public static IEnumerable<T> SliceRow<T>(this T[,] array, int row)
        {
            for (int i = array.GetLowerBound(1); i <= array.GetUpperBound(1); i++)
                yield return array[row, i];
        }

        /// <summary>
        /// Swaps two elements in a collection
        /// </summary>
        /// <typeparam name="T">The type that is contained in the collection</typeparam>
        /// <param name="collection">The collection where the elements should be swaped</param>
        /// <param name="a">The first element to swap</param>
        /// <param name="b">The second element to swap with</param>
        /// <returns>The collection</returns>
        public static IList<T> Swap<T>(this IList<T> collection, T a, T b)
        {
            var indexOfA = collection.IndexOf(a);
            var indexOfB = collection.IndexOf(b);

            collection[indexOfA] = b;
            collection[indexOfB] = a;

            return collection;
        }

        /// <summary>
        /// Tries to performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// <para/>
        /// This will dispose an object if it implements the <see cref="IDisposable "/> interface.
        /// </summary>
        /// <param name="context">The object to dispose</param>
        public static void TryDispose(this object context)
        {
            var disposable = context as IDisposable;
            disposable?.Dispose();
        }

        /// <summary>
        /// Tries to encode a byte array to a string by detecting its encoding.
        /// <para/>
        /// It will try to detect the encoding for for UTF-7, UTF-8/16/32 (bom, no bom, little and
        /// big endian), and local default codepage, and potentially other codepages.
        /// </summary>
        /// <param name="data">The byte array that contains the string to be encoded</param>
        /// <returns>The encoded string</returns>
        /// <example>
        /// <code>
        /// var text = Assemblies.GetManifestResource("embedded-text.txt").TryEncode();
        /// </code>
        /// </example>
        public static string TryEncode(this byte[] data)
        {
            // Original: http://stackoverflow.com/questions/1025332/determine-a-strings-encoding-in-c-sharp
            // Dan W - 2012

            // First check the low hanging fruit by checking if a BOM/signature exists (sourced from http://www.unicode.org/faq/utf_bom.html#bom4)
            if (data.Length >= 4 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0xFE && data[3] == 0xFF)
                // UTF-32, big-endian
                return Encoding.GetEncoding("utf-32BE").GetString(data, 4, data.Length - 4);
            else if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xFE && data[2] == 0x00 && data[3] == 0x00)
                // UTF-32, little-endian
                return Encoding.UTF32.GetString(data, 4, data.Length - 4);
            else if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
                // UTF-16, big-endian
                return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
            else if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
                // UTF-16, little-endian
                return Encoding.Unicode.GetString(data, 2, data.Length - 2);
            else if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                // UTF-8
                return Encoding.UTF8.GetString(data, 3, data.Length - 3);
            else if (data.Length >= 3 && data[0] == 0x2b && data[1] == 0x2f && data[2] == 0x76)
                // UTF-7
                return Encoding.UTF7.GetString(data, 3, data.Length - 3);

            // If the code reaches here, no BOM/signature was found, so now we need to 'taste' the
            // file to see if can manually discover the encoding. A high taster value is desired for UTF-8
            var taster = data.Length;    // Taster size can't be bigger than the filesize obviously.

            // Some text files are encoded in UTF8, but have no BOM/signature. Hence the below
            // manually checks for a UTF8 pattern. This code is based off the top answer at:
            // http://stackoverflow.com/questions/6555015/check-for-invalid-utf8 For our purposes, an
            // unnecessarily strict (and terser/slower) implementation is shown at:
            // http://stackoverflow.com/questions/1031645/how-to-detect-utf-8-in-plain-c For the
            // below, false positives should be exceedingly rare (and would be either slightly
            // malformed UTF-8 (which would suit our purposes
            // anyway) or 8-bit extended ASCII/UTF-16/32 at a vanishingly long shot).
            int i = 0;
            bool utf8 = false;

            while (i < taster - 4)
            {
                // If all characters are below 0x80, then it is valid UTF8, but UTF8 is not
                // 'required' (and therefore the text is more desirable to be treated as the default
                // codepage of the computer). Hence, there's no "utf8 = true;" code unlike the next
                // three checks.
                if (data[i] <= 0x7F)
                {
                    i += 1;
                    continue;
                }
                if (data[i] >= 0xC2 && data[i] <= 0xDF && data[i + 1] >= 0x80 && data[i + 1] < 0xC0)
                {
                    i += 2;
                    utf8 = true;
                    continue;
                }
                if (data[i] >= 0xE0 && data[i] <= 0xF0 && data[i + 1] >= 0x80 && data[i + 1] < 0xC0 && data[i + 2] >= 0x80 && data[i + 2] < 0xC0)
                {
                    i += 3;
                    utf8 = true;
                    continue;
                }
                if (data[i] >= 0xF0 && data[i] <= 0xF4 && data[i + 1] >= 0x80 && data[i + 1] < 0xC0 && data[i + 2] >= 0x80 && data[i + 2] < 0xC0 && data[i + 3] >= 0x80 && data[i + 3] < 0xC0)
                {
                    i += 4;
                    utf8 = true;
                    continue;
                }

                utf8 = false;
                break;
            }

            if (utf8 == true)
                return Encoding.UTF8.GetString(data);

            // The next check is a heuristic attempt to detect UTF-16 without a BOM. We simply look
            // for zeroes in odd or even byte places, and if a certain threshold is reached, the code
            // is 'probably' UF-16.
            double threshold = 0.1; // proportion of chars step 2 which must be zeroed to be diagnosed as utf-16. 0.1 = 10%
            int count = 0;

            for (int n = 0; n < taster; n += 2)
                if (data[n] == 0) count++;

            if ((double)count / taster > threshold)
                return Encoding.BigEndianUnicode.GetString(data);

            count = 0;

            for (int n = 1; n < taster; n += 2)
                if (data[n] == 0)
                    count++;

            if ((double)count / taster > threshold)
                return Encoding.Unicode.GetString(data); // (little-endian)

            // Finally, a long shot - let's see if we can find "charset=xyz" or "encoding=xyz" to
            // identify the encoding:
            for (int n = 0; n < taster - 9; n++)
            {
                if (
                    ((data[n + 0] == 'c' || data[n + 0] == 'C') && (data[n + 1] == 'h' || data[n + 1] == 'H') && (data[n + 2] == 'a' || data[n + 2] == 'A') && (data[n + 3] == 'r' || data[n + 3] == 'R') && (data[n + 4] == 's' || data[n + 4] == 'S') && (data[n + 5] == 'e' || data[n + 5] == 'E') && (data[n + 6] == 't' || data[n + 6] == 'T') && (data[n + 7] == '=')) ||
                    ((data[n + 0] == 'e' || data[n + 0] == 'E') && (data[n + 1] == 'n' || data[n + 1] == 'N') && (data[n + 2] == 'c' || data[n + 2] == 'C') && (data[n + 3] == 'o' || data[n + 3] == 'O') && (data[n + 4] == 'd' || data[n + 4] == 'D') && (data[n + 5] == 'i' || data[n + 5] == 'I') && (data[n + 6] == 'n' || data[n + 6] == 'N') && (data[n + 7] == 'g' || data[n + 7] == 'G') && (data[n + 8] == '='))
                    )
                {
                    if (data[n + 0] == 'c' || data[n + 0] == 'C')
                        n += 8;
                    else
                        n += 9;

                    if (data[n] == '"' || data[n] == '\'')
                        n++;

                    int oldn = n;

                    while (n < taster && (data[n] == '_' || data[n] == '-' || (data[n] >= '0' && data[n] <= '9') || (data[n] >= 'a' && data[n] <= 'z') || (data[n] >= 'A' && data[n] <= 'Z')))
                        n++;

                    var nb = new byte[n - oldn];

                    Array.Copy(data, oldn, nb, 0, n - oldn);

                    try
                    {
                        var internalEnc = Encoding.ASCII.GetString(nb);
                        return Encoding.GetEncoding(internalEnc).GetString(data);
                    }
                    catch
                    {
                        // If C# doesn't recognize the name of the encoding, break.
                        break;
                    }
                }
            }

            // If all else fails, the encoding is probably (though certainly not
            // definitely) the user's local codepage! One might present to the user a list of
            // alternative encodings as shown here:
            // http://stackoverflow.com/questions/8509339/what-is-the-most-common-encoding-of-each-language
            // A full list can be found using Encoding.GetEncodings();
            return Encoding.ASCII.GetString(data);
        }

        /// <summary>
        /// Uncompresses a gzip compressed data
        /// </summary>
        /// <param name="compressedData">The compressed data to uncompress</param>
        /// <returns>The uncompressed string</returns>
        /// <exception cref="ArgumentNullException"><paramref name="compressedData"/> is null</exception>
        public static byte[] Uncompress(this byte[] compressedData)
        {
            if (compressedData == null)
                throw new ArgumentNullException(nameof(compressedData));

            return Compression.Decompress(compressedData);
        }

        /// <summary>
        /// Uncompresses a gzip compressed string
        /// </summary>
        /// <param name="compressedData">The compressed data to uncompress</param>
        /// <returns>The uncompressed data</returns>
        /// <exception cref="ArgumentNullException"><paramref name="compressedData"/> is null</exception>
        public static string UncompressString(this byte[] compressedData)
        {
            if (compressedData == null)
                throw new ArgumentNullException(nameof(compressedData));

            var data = Compression.Decompress(compressedData);
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }
    }
}