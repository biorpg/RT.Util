using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>This class offers some generic static functions which are hard to categorize under any more specific classes.</summary>
    public static partial class Ut
    {
        /// <summary>
        ///     Converts file size in bytes to a string that uses KB, MB, GB or TB.</summary>
        /// <param name="size">
        ///     The file size in bytes.</param>
        /// <returns>
        ///     The converted string.</returns>
        public static string SizeToString(long size) =>
            size == 0
                ? "0"
                : size < 1024
                    ? size.ToString("#,###")
                    : size < 1024 * 1024
                        ? (size / 1024d).ToString("#,###.## KB")
                        : size < 1024 * 1024 * 1024
                            ? (size / 1024d / 1024d).ToString("#,###.## MB")
                            : size < 1024L * 1024 * 1024 * 1024
                                ? (size / 1024d / 1024d / 1024d).ToString("#,###.## GB")
                                : (size / 1024d / 1024d / 1024d / 1024d).ToString("#,###.## TB");

        /// <summary>Returns the smaller of the two IComparable values. If the values are equal, returns the first one.</summary>
        public static T Min<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) <= 0 ? val1 : val2;
        }

        /// <summary>Returns the smaller of the three IComparable values. If two values are equal, returns the earlier one.</summary>
        public static T Min<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) <= 0 ? val1 : val2;
            return c1.CompareTo(val3) <= 0 ? c1 : val3;
        }

        /// <summary>Returns the smallest of all arguments passed in. Uses the Linq .Min extension method to do the work.</summary>
        public static T Min<T>(params T[] args) where T : IComparable<T>
        {
            return args.Min();
        }

        /// <summary>Returns the larger of the two IComparable values. If the values are equal, returns the first one.</summary>
        public static T Max<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) >= 0 ? val1 : val2;
        }

        /// <summary>Returns the larger of the three IComparable values. If two values are equal, returns the earlier one.</summary>
        public static T Max<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) >= 0 ? val1 : val2;
            return c1.CompareTo(val3) >= 0 ? c1 : val3;
        }

        /// <summary>Returns the largest of all arguments passed in. Uses the Linq .Max extension method to do the work.</summary>
        public static T Max<T>(params T[] args) where T : IComparable<T>
        {
            return args.Max();
        }

        /// <summary>
        ///     Sends the specified sequence of key strokes to the active application. See remarks for details.</summary>
        /// <param name="keys">
        ///     A collection of objects of type <see cref="Keys"/>, <see cref="char"/>, or <c>System.Tuple&lt;Keys,
        ///     bool&gt;</c>.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="keys"/> was null.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="keys"/> contains an object which is of an unexpected type. Only <see cref="Keys"/>, <see
        ///     cref="char"/> and <c>System.Tuple&lt;System.Windows.Forms.Keys, bool&gt;</c> are accepted.</exception>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><description>
        ///             For objects of type <see cref="Keys"/>, the relevant key is pressed and released.</description></item>
        ///         <item><description>
        ///             For objects of type <see cref="char"/>, the specified Unicode character is simulated as a keypress and
        ///             release.</description></item>
        ///         <item><description>
        ///             For objects of type <c>Tuple&lt;Keys, bool&gt;</c> or <c>ValueType&lt;Keys, bool&gt;</c>, the bool
        ///             specifies whether to simulate only a key-down (false) or only a key-up (true).</description></item></list></remarks>
        /// <example>
        ///     <para>
        ///         The following example demonstrates how to use this method to send the key combination Win+R:</para>
        ///     <code>
        ///         Ut.SendKeystrokes(Ut.NewArray&lt;object&gt;(
        ///             (Keys.LWin, false),
        ///             Keys.R,
        ///             (Keys.LWin, true)
        ///         ));</code></example>
        public static void SendKeystrokes(IEnumerable<object> keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            var input = new List<WinAPI.INPUT>();
            foreach (var elem in keys)
            {
                void sendTuple(Keys key, bool isUp)
                {
                    var keyEvent = new WinAPI.INPUT
                    {
                        Type = WinAPI.INPUT_KEYBOARD,
                        SpecificInput = new WinAPI.MOUSEKEYBDHARDWAREINPUT
                        {
                            Keyboard = new WinAPI.KEYBDINPUT { wVk = (ushort) key }
                        }
                    };
                    if (isUp)
                        keyEvent.SpecificInput.Keyboard.dwFlags |= WinAPI.KEYEVENTF_KEYUP;
                    input.Add(keyEvent);
                }

                if (elem is Tuple<Keys, bool> t)
                    sendTuple(t.Item1, t.Item2);
                else if (elem is ValueTuple<Keys, bool> vt)
                    sendTuple(vt.Item1, vt.Item2);
                else
                {
                    if (!(elem is Keys || elem is char))
                        throw new ArgumentException(@"The input collection is expected to contain only objects of type Keys, char, Tuple<Keys, bool> or ValueTuple<Keys, bool>.", nameof(keys));
                    var keyDown = new WinAPI.INPUT
                    {
                        Type = WinAPI.INPUT_KEYBOARD,
                        SpecificInput = new WinAPI.MOUSEKEYBDHARDWAREINPUT
                        {
                            Keyboard = (elem is Keys)
                                ? new WinAPI.KEYBDINPUT { wVk = (ushort) (Keys) elem }
                                : new WinAPI.KEYBDINPUT { wScan = (char) elem, dwFlags = WinAPI.KEYEVENTF_UNICODE }
                        }
                    };
                    var keyUp = keyDown;
                    keyUp.SpecificInput.Keyboard.dwFlags |= WinAPI.KEYEVENTF_KEYUP;
                    input.Add(keyDown);
                    input.Add(keyUp);
                }
            }
            var inputArr = input.ToArray();
            WinAPI.SendInput((uint) inputArr.Length, inputArr, Marshal.SizeOf(input[0]));
        }

        /// <summary>
        ///     Sends the specified key the specified number of times.</summary>
        /// <param name="key">
        ///     Key stroke to send.</param>
        /// <param name="times">
        ///     Number of times to send the <paramref name="key"/>.</param>
        public static void SendKeystrokes(Keys key, int times)
        {
            if (times > 0)
                SendKeystrokes(Enumerable.Repeat((object) key, times));
        }

        /// <summary>Sends key strokes equivalent to typing the specified text.</summary>
        public static void SendKeystrokesForText(string text)
        {
            if (!string.IsNullOrEmpty(text))
                SendKeystrokes(text.Cast<object>());
        }

        /// <summary>
        ///     Reads the specified file and computes the SHA1 hash function from its contents.</summary>
        /// <param name="path">
        ///     Path to the file to compute SHA1 hash function from.</param>
        /// <returns>
        ///     Result of the SHA1 hash function as a string of hexadecimal digits.</returns>
        public static string Sha1(string path)
        {
            using (var f = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return SHA1.Create().ComputeHash(f).ToHex();
        }

        /// <summary>
        ///     Reads the specified file and computes the MD5 hash function from its contents.</summary>
        /// <param name="path">
        ///     Path to the file to compute MD5 hash function from.</param>
        /// <returns>
        ///     Result of the MD5 hash function as a string of hexadecimal digits.</returns>
        public static string Md5(string path)
        {
            using (var f = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return MD5.Create().ComputeHash(f).ToHex();
        }

        /// <summary>Returns the version of the entry assembly (the .exe file) in a standard format.</summary>
        public static string VersionOfExe()
        {
            var v = Assembly.GetEntryAssembly().GetName().Version;
            return $"{v.Major}.{v.Minor}.{v.Build} ({v.Revision})"; // in our use: v.Build is build#, v.Revision is p4 changelist
        }

        /// <summary>
        ///     Checks the specified condition and causes the debugger to break if it is false. Throws an <see
        ///     cref="InternalErrorException"/> afterwards.</summary>
        [DebuggerHidden]
        public static void Assert(bool assertion, string message = null)
        {
            if (!assertion)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                throw new Exception(message ?? "Assertion failure");
            }
        }

        /// <summary>
        ///     Checks the specified condition and causes the debugger to break if it is false. Throws an <see
        ///     cref="InternalErrorException"/> afterwards.</summary>
        [DebuggerHidden]
        public static void AssertAll<T>(IEnumerable<T> collection, Func<T, bool> assertion, string message = null)
        {
            if (!collection.All(assertion))
            {
                var failure = collection.FirstOrDefault(x => !assertion(x)); // only so that it can be inspected in the debugger
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                throw new Exception(message ?? "Assertion failure");
            }
        }

        /// <summary>Determines whether the Ctrl key is pressed.</summary>
        public static bool Ctrl { get { return Control.ModifierKeys.HasFlag(Keys.Control); } }
        /// <summary>Determines whether the Alt key is pressed.</summary>
        public static bool Alt { get { return Control.ModifierKeys.HasFlag(Keys.Alt); } }
        /// <summary>Determines whether the Shift key is pressed.</summary>
        public static bool Shift { get { return Control.ModifierKeys.HasFlag(Keys.Shift); } }

        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Action Lambda(Action method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Action<T> Lambda<T>(Action<T> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Action<T1, T2> Lambda<T1, T2>(Action<T1, T2> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Action<T1, T2, T3> Lambda<T1, T2, T3>(Action<T1, T2, T3> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Action<T1, T2, T3, T4> Lambda<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Action<T1, T2, T3, T4, T5> Lambda<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Func<TResult> Lambda<TResult>(Func<TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Func<T, TResult> Lambda<T, TResult>(Func<T, TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Func<T1, T2, TResult> Lambda<T1, T2, TResult>(Func<T1, T2, TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Func<T1, T2, T3, TResult> Lambda<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Func<T1, T2, T3, T4, TResult> Lambda<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make
        ///     any difference.</summary>
        public static Func<T1, T2, T3, T4, T5, TResult> Lambda<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> method) { return method; }

        /// <summary>Allows the use of type inference when creating .NET’s KeyValuePair&lt;TK,TV&gt;.</summary>
        public static KeyValuePair<TKey, TValue> KeyValuePair<TKey, TValue>(TKey key, TValue value) { return new KeyValuePair<TKey, TValue>(key, value); }

        /// <summary>
        ///     Returns the parameters as a new array.</summary>
        /// <remarks>
        ///     Useful to circumvent Visual Studio’s bug where multi-line literal arrays are not auto-formatted.</remarks>
        public static T[] NewArray<T>(params T[] parameters) { return parameters; }

        /// <summary>
        ///     Instantiates a fully-initialized array with the specified dimensions.</summary>
        /// <param name="size">
        ///     Size of the first dimension.</param>
        /// <param name="initialiser">
        ///     Function to initialise the value of every element.</param>
        /// <typeparam name="T">
        ///     Type of the array element.</typeparam>
        public static T[] NewArray<T>(int size, Func<int, T> initialiser)
        {
            if (initialiser == null)
                throw new ArgumentNullException(nameof(initialiser));
            var result = new T[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = initialiser(i);
            }
            return result;
        }

        /// <summary>
        ///     Instantiates a fully-initialized rectangular jagged array with the specified dimensions.</summary>
        /// <param name="size1">
        ///     Size of the first dimension.</param>
        /// <param name="size2">
        ///     Size of the second dimension.</param>
        /// <param name="initialiser">
        ///     Optional function to initialise the value of every element.</param>
        /// <typeparam name="T">
        ///     Type of the array element.</typeparam>
        public static T[][] NewArray<T>(int size1, int size2, Func<int, int, T> initialiser = null)
        {
            var result = new T[size1][];
            for (int i = 0; i < size1; i++)
            {
                var arr = new T[size2];
                if (initialiser != null)
                    for (int j = 0; j < size2; j++)
                        arr[j] = initialiser(i, j);
                result[i] = arr;
            }
            return result;
        }

        /// <summary>
        ///     Instantiates a fully-initialized "rectangular" jagged array with the specified dimensions.</summary>
        /// <param name="size1">
        ///     Size of the first dimension.</param>
        /// <param name="size2">
        ///     Size of the second dimension.</param>
        /// <param name="size3">
        ///     Size of the third dimension.</param>
        /// <param name="initialiser">
        ///     Optional function to initialise the value of every element.</param>
        /// <typeparam name="T">
        ///     Type of the array element.</typeparam>
        public static T[][][] NewArray<T>(int size1, int size2, int size3, Func<int, int, int, T> initialiser = null)
        {
            var result = new T[size1][][];
            for (int i = 0; i < size1; i++)
            {
                var arr = new T[size2][];
                for (int j = 0; j < size2; j++)
                {
                    var arr2 = new T[size3];
                    if (initialiser != null)
                        for (int k = 0; k < size2; k++)
                            arr2[k] = initialiser(i, j, k);
                    arr[j] = arr2;
                }
                result[i] = arr;
            }
            return result;
        }

        /// <summary>
        ///     Returns the integer represented by the specified string, or null if the string does not represent a valid
        ///     32-bit integer.</summary>
        public static int? ParseInt32(string value) => int.TryParse(value, out var result) ? (int?) result : null;
        /// <summary>
        ///     Returns the integer represented by the specified string, or null if the string does not represent a valid
        ///     64-bit integer.</summary>
        public static long? ParseInt64(string value) => long.TryParse(value, out var result) ? (long?) result : null;
        /// <summary>
        ///     Returns the floating-point number represented by the specified string, or null if the string does not
        ///     represent a valid double-precision floating-point number.</summary>
        public static double? ParseDouble(string value) => double.TryParse(value, out var result) ? (double?) result : null;
        /// <summary>
        ///     Returns the date/time stamp represented by the specified string, or null if the string does not represent a
        ///     valid date/time stamp.</summary>
        public static DateTime? ParseDateTime(string value) => DateTime.TryParse(value, out var result) ? (DateTime?) result : null;
        /// <summary>
        ///     Returns the enum value represented by the specified string, or null if the string does not represent a valid
        ///     enum value.</summary>
        public static T? ParseEnum<T>(string value, bool ignoreCase = false) where T : struct => Enum.TryParse<T>(value, ignoreCase, out var result) ? (T?) result : null;

        /// <summary>
        ///     Creates a delegate using Action&lt;,*&gt; or Func&lt;,*&gt; depending on the number of parameters of the
        ///     specified method.</summary>
        /// <param name="firstArgument">
        ///     Object to call the method on, or null for static methods.</param>
        /// <param name="method">
        ///     The method to call.</param>
        public static Delegate CreateDelegate(object firstArgument, MethodInfo method)
        {
            var param = method.GetParameters();
            return Delegate.CreateDelegate(
                method.ReturnType == typeof(void)
                    ? param.Length == 0 ? typeof(Action) : actionType(param.Length).MakeGenericType(param.Select(p => p.ParameterType).ToArray())
                    : funcType(param.Length).MakeGenericType(param.Select(p => p.ParameterType).Concat(method.ReturnType).ToArray()),
                firstArgument,
                method
            );
        }

        private static Type funcType(int numParameters)
        {
            switch (numParameters)
            {
                case 0: return typeof(Func<>);
                case 1: return typeof(Func<,>);
                case 2: return typeof(Func<,,>);
                case 3: return typeof(Func<,,,>);
                case 4: return typeof(Func<,,,,>);
                case 5: return typeof(Func<,,,,,>);
                case 6: return typeof(Func<,,,,,,>);
                case 7: return typeof(Func<,,,,,,,>);
                case 8: return typeof(Func<,,,,,,,,>);
                case 9: return typeof(Func<,,,,,,,,,>);
                case 10: return typeof(Func<,,,,,,,,,,>);
                case 11: return typeof(Func<,,,,,,,,,,,>);
                case 12: return typeof(Func<,,,,,,,,,,,,>);
                case 13: return typeof(Func<,,,,,,,,,,,,,>);
                case 14: return typeof(Func<,,,,,,,,,,,,,,>);
                case 15: return typeof(Func<,,,,,,,,,,,,,,,>);
                case 16: return typeof(Func<,,,,,,,,,,,,,,,,>);
            }
            throw new ArgumentException("numParameters must be between 0 and 16.", nameof(numParameters));
        }

        private static Type actionType(int numParameters)
        {
            switch (numParameters)
            {
                case 0: return typeof(Action);
                case 1: return typeof(Action<>);
                case 2: return typeof(Action<,>);
                case 3: return typeof(Action<,,>);
                case 4: return typeof(Action<,,,>);
                case 5: return typeof(Action<,,,,>);
                case 6: return typeof(Action<,,,,,>);
                case 7: return typeof(Action<,,,,,,>);
                case 8: return typeof(Action<,,,,,,,>);
                case 9: return typeof(Action<,,,,,,,,>);
                case 10: return typeof(Action<,,,,,,,,,>);
                case 11: return typeof(Action<,,,,,,,,,,>);
                case 12: return typeof(Action<,,,,,,,,,,,>);
                case 13: return typeof(Action<,,,,,,,,,,,,>);
                case 14: return typeof(Action<,,,,,,,,,,,,,>);
                case 15: return typeof(Action<,,,,,,,,,,,,,,>);
                case 16: return typeof(Action<,,,,,,,,,,,,,,,>);
            }
            throw new ArgumentException("numParameters must be between 0 and 16.", nameof(numParameters));
        }

        /// <summary>
        ///     Executes the specified function with the specified argument.</summary>
        /// <typeparam name="TSource">
        ///     Type of the argument to the function.</typeparam>
        /// <typeparam name="TResult">
        ///     Type of the result of the function.</typeparam>
        /// <param name="source">
        ///     The argument to the function.</param>
        /// <param name="func">
        ///     The function to execute.</param>
        /// <returns>
        ///     The result of the function.</returns>
        public static TResult Apply<TSource, TResult>(this TSource source, Func<TSource, TResult> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            return func(source);
        }

        /// <summary>
        ///     Executes the specified action with the specified argument.</summary>
        /// <typeparam name="TSource">
        ///     Type of the argument to the action.</typeparam>
        /// <param name="source">
        ///     The argument to the action.</param>
        /// <param name="action">
        ///     The action to execute.</param>
        /// <returns>
        ///     The result of the function.</returns>
        public static void Apply<TSource>(this TSource source, Action<TSource> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            action(source);
        }

        /// <summary>
        ///     Executes the specified action. If the action results in a file sharing violation exception, the action will be
        ///     repeatedly retried after a short delay (which increases after every failed attempt).</summary>
        /// <param name="action">
        ///     The action to be attempted and possibly retried.</param>
        /// <param name="maximum">
        ///     Maximum amount of time to keep retrying for. When expired, any sharing violation exception will propagate to
        ///     the caller of this method. Use null to retry indefinitely.</param>
        /// <param name="onSharingVio">
        ///     Action to execute when a sharing violation does occur (is called before the waiting).</param>
        public static void WaitSharingVio(Action action, TimeSpan? maximum = null, Action onSharingVio = null)
        {
            WaitSharingVio<bool>(() => { action(); return true; }, maximum, onSharingVio);
        }

        /// <summary>
        ///     Executes the specified function. If the function results in a file sharing violation exception, the function
        ///     will be repeatedly retried after a short delay (which increases after every failed attempt).</summary>
        /// <param name="func">
        ///     The function to be attempted and possibly retried.</param>
        /// <param name="maximum">
        ///     Maximum amount of time to keep retrying for. When expired, any sharing violation exception will propagate to
        ///     the caller of this method. Use null to retry indefinitely.</param>
        /// <param name="onSharingVio">
        ///     Action to execute when a sharing violation does occur (is called before the waiting).</param>
        public static T WaitSharingVio<T>(Func<T> func, TimeSpan? maximum = null, Action onSharingVio = null)
        {
            var started = DateTime.UtcNow;
            int sleep = 279;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (IOException ex)
                {
                    int hResult = 0;
                    try { hResult = (int) ex.GetType().GetProperty("HResult", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(ex, null); }
                    catch { }
                    if (hResult != -2147024864) // 0x80070020 ERROR_SHARING_VIOLATION
                        throw;
                    onSharingVio?.Invoke();
                }

                if (maximum != null)
                {
                    int leftMs = (int) (maximum.Value - (DateTime.UtcNow - started)).TotalMilliseconds;
                    if (sleep > leftMs)
                    {
                        if (leftMs > 0)
                            Thread.Sleep(leftMs);
                        return func(); // or throw the sharing vio exception
                    }
                }

                Thread.Sleep(sleep);
                sleep = Math.Min((sleep * 3) >> 1, 10000);
            }
        }

        /// <summary>
        ///     Queues the specified action to be executed on the thread pool. This is just a shortcut for
        ///     <c>ThreadPool.QueueUserWorkItem</c>, and also does not require the method to accept a parameter (which has
        ///     been useless ever since C# gained support for lambdas).</summary>
        public static void ThreadPool(Action task)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ => task());
        }

        /// <summary>Swaps the values of the specified two variables.</summary>
        public static void Swap<T>(ref T one, ref T two)
        {
            T t = one;
            one = two;
            two = t;
        }

        /// <summary>
        ///     Finds the longest substring that all of the specified input strings contain.</summary>
        /// <param name="strings">
        ///     Strings to examine.</param>
        /// <returns>
        ///     The longest shared substring. This may be the empty string, but not will not be <c>null</c>.</returns>
        public static string GetLongestCommonSubstring(params string[] strings)
        {
            if (strings == null)
                throw new ArgumentNullException(nameof(strings));
            if (strings.Length < 1)
                throw new ArgumentException("The 'strings' array must contain at least one value.", nameof(strings));

            if (strings.Length == 1)
                return strings[0];

            // Optimisation: Instantiate these things only once (including the closure class for the lambda)
            var skipped = strings.Skip(1);

            for (var len = strings.Min(str => str.Length); len >= 1; len--)
            {
                var maxIndex = strings[0].Length - len;
                for (var index = 0; index <= maxIndex; index++)
                {
                    var substr = strings[0].Substring(index, len);
                    if (skipped.All(s => s.Contains(substr)))
                        return substr;
                }
            }
            return "";
        }

        /// <summary>
        ///     Converts the bytes at the specified <paramref name="index"/> within the specified <paramref name="buffer"/> to
        ///     a 16-bit unsigned integer.</summary>
        /// <param name="buffer">
        ///     Array to take values from.</param>
        /// <param name="index">
        ///     Index within the array at which to start taking values.</param>
        /// <param name="bigEndian">
        ///     <c>true</c> to interpret the data as big-endian byte order; <c>false</c> for little-endian byte order.</param>
        /// <returns>
        ///     The converted 16-bit unsigned integer.</returns>
        public static ushort BytesToUShort(byte[] buffer, int index, bool bigEndian = false)
        {
            return bigEndian
                ? (ushort) ((buffer[index] << 8) | buffer[index + 1])
                : (ushort) (buffer[index] | (buffer[index + 1] << 8));
        }

        /// <summary>
        ///     Converts the bytes at the specified <paramref name="index"/> within the specified <paramref name="buffer"/> to
        ///     a 16-bit signed integer.</summary>
        /// <param name="buffer">
        ///     Array to take values from.</param>
        /// <param name="index">
        ///     Index within the array at which to start taking values.</param>
        /// <param name="bigEndian">
        ///     <c>true</c> to interpret the data as big-endian byte order; <c>false</c> for little-endian byte order.</param>
        /// <returns>
        ///     The converted 16-bit signed integer.</returns>
        public static short BytesToShort(byte[] buffer, int index, bool bigEndian = false)
        {
            return bigEndian
                ? (short) ((buffer[index] << 8) | buffer[index + 1])
                : (short) (buffer[index] | (buffer[index + 1] << 8));
        }

        /// <summary>
        ///     Converts the bytes at the specified <paramref name="index"/> within the specified <paramref name="buffer"/> to
        ///     a 32-bit unsigned integer.</summary>
        /// <param name="buffer">
        ///     Array to take values from.</param>
        /// <param name="index">
        ///     Index within the array at which to start taking values.</param>
        /// <param name="bigEndian">
        ///     <c>true</c> to interpret the data as big-endian byte order; <c>false</c> for little-endian byte order.</param>
        /// <returns>
        ///     The converted 32-bit unsigned integer.</returns>
        public static uint BytesToUInt(byte[] buffer, int index, bool bigEndian = false)
        {
            return bigEndian
                ? ((uint) buffer[index] << 24) | ((uint) buffer[index + 1] << 16) | ((uint) buffer[index + 2] << 8) | buffer[index + 3]
                : buffer[index] | ((uint) buffer[index + 1] << 8) | ((uint) buffer[index + 2] << 16) | ((uint) buffer[index + 3] << 24);
        }

        /// <summary>
        ///     Converts the bytes at the specified <paramref name="index"/> within the specified <paramref name="buffer"/> to
        ///     a 32-bit signed integer.</summary>
        /// <param name="buffer">
        ///     Array to take values from.</param>
        /// <param name="index">
        ///     Index within the array at which to start taking values.</param>
        /// <param name="bigEndian">
        ///     <c>true</c> to interpret the data as big-endian byte order; <c>false</c> for little-endian byte order.</param>
        /// <returns>
        ///     The converted 32-bit signed integer.</returns>
        public static int BytesToInt(byte[] buffer, int index, bool bigEndian = false) => bigEndian
                ? (buffer[index] << 24) | (buffer[index + 1] << 16) | (buffer[index + 2] << 8) | buffer[index + 3]
                : buffer[index] | (buffer[index + 1] << 8) | (buffer[index + 2] << 16) | (buffer[index + 3] << 24);

        /// <summary>
        ///     Converts the bytes at the specified <paramref name="index"/> within the specified <paramref name="buffer"/> to
        ///     a 64-bit unsigned integer.</summary>
        /// <param name="buffer">
        ///     Array to take values from.</param>
        /// <param name="index">
        ///     Index within the array at which to start taking values.</param>
        /// <param name="bigEndian">
        ///     <c>true</c> to interpret the data as big-endian byte order; <c>false</c> for little-endian byte order.</param>
        /// <returns>
        ///     The converted 64-bit unsigned integer.</returns>
        public static ulong BytesToULong(byte[] buffer, int index, bool bigEndian = false) => bigEndian
                ? ((ulong) buffer[index] << 56) | ((ulong) buffer[index + 1] << 48) | ((ulong) buffer[index + 2] << 40) | ((ulong) buffer[index + 3] << 32) | ((ulong) buffer[index + 4] << 24) | ((ulong) buffer[index + 5] << 16) | ((ulong) buffer[index + 6] << 8) | buffer[index + 7]
                : buffer[index] | ((ulong) buffer[index + 1] << 8) | ((ulong) buffer[index + 2] << 16) | ((ulong) buffer[index + 3] << 24) | ((ulong) buffer[index + 4] << 32) | ((ulong) buffer[index + 5] << 40) | ((ulong) buffer[index + 6] << 48) | ((ulong) buffer[index + 7] << 56);

        /// <summary>
        ///     Converts the bytes at the specified <paramref name="index"/> within the specified <paramref name="buffer"/> to
        ///     a 64-bit signed integer.</summary>
        /// <param name="buffer">
        ///     Array to take values from.</param>
        /// <param name="index">
        ///     Index within the array at which to start taking values.</param>
        /// <param name="bigEndian">
        ///     <c>true</c> to interpret the data as big-endian byte order; <c>false</c> for little-endian byte order.</param>
        /// <returns>
        ///     The converted 64-bit signed integer.</returns>
        public static long BytesToLong(byte[] buffer, int index, bool bigEndian = false) => bigEndian
                ? ((long) buffer[index] << 56) | ((long) buffer[index + 1] << 48) | ((long) buffer[index + 2] << 40) | ((long) buffer[index + 3] << 32) | ((long) buffer[index + 4] << 24) | ((long) buffer[index + 5] << 16) | ((long) buffer[index + 6] << 8) | buffer[index + 7]
                : buffer[index] | ((long) buffer[index + 1] << 8) | ((long) buffer[index + 2] << 16) | ((long) buffer[index + 3] << 24) | ((long) buffer[index + 4] << 32) | ((long) buffer[index + 5] << 40) | ((long) buffer[index + 6] << 48) | ((long) buffer[index + 7] << 56);

        private static class EnumAttributeCache<TAttribute>
        {
            public static Dictionary<Type, Dictionary<Enum, TAttribute[]>> Dictionary = new Dictionary<Type, Dictionary<Enum, TAttribute[]>>();
        }

        /// <summary>
        ///     Returns the set of custom attributes of the specified <typeparamref name="TAttribute"/> type that are attached
        ///     to the declaration of the enum value represented by <paramref name="enumValue"/>.</summary>
        /// <typeparam name="TAttribute">
        ///     The type of the custom attributes to retrieve.</typeparam>
        /// <param name="enumValue">
        ///     The enum value for which to retrieve the custom attributes.</param>
        /// <returns>
        ///     An array containing the custom attributes, or <c>null</c> if <paramref name="enumValue"/> does not correspond
        ///     to a declared value.</returns>
        /// <remarks>
        ///     This method keeps an internal cache forever.</remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="enumValue"/> is <c>null</c>.</exception>
        public static TAttribute[] GetCustomAttributes<TAttribute>(this Enum enumValue) where TAttribute : Attribute
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));
            var enumType = enumValue.GetType();
            var dic = EnumAttributeCache<TAttribute>.Dictionary;
            TAttribute[] arr;
            if (!dic.ContainsKeys(enumType, enumValue))
            {
                arr = null;
                foreach (var field in enumType.GetFields(BindingFlags.Static | BindingFlags.Public))
                {
                    var attrs = field.GetCustomAttributes<TAttribute>().ToArray();
                    var enumVal = (Enum) field.GetValue(null);
                    dic.AddSafe(enumType, enumVal, attrs);
                    if (enumVal.Equals(enumValue))
                        arr = attrs;
                }
                return arr;
            }
            return dic.TryGetValue(enumType, enumValue, out arr) ? arr : null;
        }

        /// <summary>
        ///     Returns the single custom attribute of the specified <typeparamref name="TAttribute"/> type that is attached
        ///     to the declaration of the enum value represented by <paramref name="enumValue"/>, or <c>null</c> if there is
        ///     no such attribute.</summary>
        /// <typeparam name="TAttribute">
        ///     The type of the custom attribute to retrieve.</typeparam>
        /// <param name="enumValue">
        ///     The enum value for which to retrieve the custom attribute.</param>
        /// <returns>
        ///     The custom attribute, or <c>null</c> if the enum value does not have a custom attribute of the specified type
        ///     attached to it. If <paramref name="enumValue"/> does not correspond to a declared enum value, or there is more
        ///     than one custom attribute of the same type, an exception is thrown.</returns>
        /// <remarks>
        ///     This method uses <see cref="Ut.GetCustomAttributes{TAttribute}(Enum)"/>, which keeps an internal cache
        ///     forever.</remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="enumValue"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        ///     There is more than one custom attribute of the specified type attached to the enum value declaration.</exception>
        public static TAttribute GetCustomAttribute<TAttribute>(this Enum enumValue) where TAttribute : Attribute
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));
            return GetCustomAttributes<TAttribute>(enumValue).SingleOrDefault();
        }

        /// <summary>Returns true if this value is equal to the default value for this type.</summary>
        public static bool IsDefault<T>(this T val) where T : struct
        {
            return val.Equals(default(T));
        }

        /// <summary>
        ///     Computes a hash value from an array of elements.</summary>
        /// <param name="input">
        ///     The array of elements to hash.</param>
        /// <returns>
        ///     The computed hash value.</returns>
        public static int ArrayHash(params object[] input) => ArrayHash((Array) input);

        /// <summary>
        ///     Computes a hash value from an array of elements.</summary>
        /// <param name="input">
        ///     The array of elements to hash.</param>
        /// <returns>
        ///     The computed hash value.</returns>
        public static int ArrayHash(Array input)
        {
            if (input == null)
                return 0;

            const int b = 378551;
            int a = 63689;
            int hash = input.Length + 1;

            unchecked
            {
                foreach (object t in input)
                {
                    if (t is Array arr)
                        hash = hash * a + ArrayHash(arr);
                    else if (t != null)
                        hash = hash * a + t.GetHashCode();
                    a = a * b;
                }
            }

            return hash;
        }

        /// <summary>
        ///     Generates a sequence of floating-point numbers within a specified range.</summary>
        /// <param name="start">
        ///     The first value to produce.</param>
        /// <param name="end">
        ///     The last value to produce.</param>
        /// <param name="step">
        ///     The difference between the values to generate, except that the difference between the last and second-last may
        ///     be smaller if the interval between <paramref name="start"/> and <paramref name="end"/> isn’t exactly divisible
        ///     by <paramref name="step"/>.</param>
        public static IEnumerable<double> Range(double start, double end, double step)
        {
            // Instead of repeatedly incrementing a double in a for loop, which
            // causes rounding errors to accumulate, calculate each value directly.
            var steps = (int) ((end - start) / step);
            for (int i = 0; i < steps; i++)
                yield return start + i * step;
            yield return end;
        }
    }
}
