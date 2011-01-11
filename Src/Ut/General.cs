using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>
    /// This class offers some generic static functions which are hard to categorize
    /// under any more specific classes.
    /// </summary>
    public static partial class Ut
    {
        /// <summary>Converts file size in bytes to a string that uses KB, MB, GB or TB.</summary>
        /// <param name="size">The file size in bytes.</param>
        /// <returns>The converted string.</returns>
        public static string SizeToString(long size)
        {
            if (size == 0)
                return "0";
            else if (size < 1024)
                return size.ToString("#,###");
            else if (size < 1024 * 1024)
                return (size / 1024d).ToString("#,###.## KB");
            else if (size < 1024 * 1024 * 1024)
                return (size / 1024d / 1024d).ToString("#,###.## MB");
            else if (size < 1024L * 1024 * 1024 * 1024)
                return (size / 1024d / 1024d / 1024d).ToString("#,###.## GB");
            else
                return (size / 1024d / 1024d / 1024d / 1024d).ToString("#,###.## TB");
        }

        /// <summary>
        /// Returns the smaller of the two IComparable values. If the values are
        /// equal, returns the first one.
        /// </summary>
        public static T Min<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) <= 0 ? val1 : val2;
        }

        /// <summary>
        /// Returns the smaller of the three IComparable values. If two values are
        /// equal, returns the earlier one.
        /// </summary>
        public static T Min<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) <= 0 ? val1 : val2;
            return c1.CompareTo(val3) <= 0 ? c1 : val3;
        }

        /// <summary>
        /// Returns the smallest of all arguments passed in. Uses the Linq .Min
        /// extension method to do the work.
        /// </summary>
        public static T Min<T>(params T[] args) where T : IComparable<T>
        {
            return args.Min();
        }

        /// <summary>
        /// Returns the larger of the two IComparable values. If the values are
        /// equal, returns the first one.
        /// </summary>
        public static T Max<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) >= 0 ? val1 : val2;
        }

        /// <summary>
        /// Returns the larger of the three IComparable values. If two values are
        /// equal, returns the earlier one.
        /// </summary>
        public static T Max<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) >= 0 ? val1 : val2;
            return c1.CompareTo(val3) >= 0 ? c1 : val3;
        }

        /// <summary>
        /// Returns the largest of all arguments passed in. Uses the Linq .Max
        /// extension method to do the work.
        /// </summary>
        public static T Max<T>(params T[] args) where T : IComparable<T>
        {
            return args.Max();
        }

        /// <summary>
        /// Sends the specified sequence of key strokes to the active application.
        /// </summary>
        /// <param name="keys">A collection of objects of type <see cref="Keys"/>, <see cref="char"/>, or Tuple&lt;Keys, bool&gt;.</param>
        /// <exception cref="ArgumentException">
        ///     <list type="bullet">
        ///         <item><description><paramref name="keys"/> was null.</description></item>
        ///         <item><description><paramref name="keys"/> contains an object which is of an unexpected type. Only <see cref="Keys"/> and <see cref="char"/> are accepted.</description></item>
        ///     </list>
        /// </exception>
        public static void SendKeystrokes(IEnumerable<object> keys)
        {
            if (keys == null)
                throw new ArgumentException(@"The input collection cannot be null.", "keys");

            var input = new List<WinAPI.INPUT>();
            foreach (var elem in keys)
            {
                Tuple<Keys, bool> t;
                if ((t = elem as Tuple<Keys, bool>) != null)
                {
                    var keyEvent = new WinAPI.INPUT
                    {
                        Type = WinAPI.INPUT_KEYBOARD,
                        SpecificInput = new WinAPI.MOUSEKEYBDHARDWAREINPUT
                        {
                            Keyboard = new WinAPI.KEYBDINPUT { wVk = (ushort) t.Item1 }
                        }
                    };
                    if (t.Item2)
                        keyEvent.SpecificInput.Keyboard.dwFlags |= WinAPI.KEYEVENTF_KEYUP;
                    input.Add(keyEvent);
                }
                else
                {
                    if (!(elem is Keys || elem is char))
                        throw new ArgumentException(@"The input collection is expected to contain only objects of type Keys, char, or Tuple<Keys, bool>.", "keys");
                    var keyDown = new WinAPI.INPUT
                    {
                        Type = WinAPI.INPUT_KEYBOARD,
                        SpecificInput = new WinAPI.MOUSEKEYBDHARDWAREINPUT
                        {
                            Keyboard = (elem is Keys)
                                ? new WinAPI.KEYBDINPUT { wVk = (ushort) (Keys) elem }
                                : new WinAPI.KEYBDINPUT { wScan = (ushort) (char) elem, dwFlags = WinAPI.KEYEVENTF_UNICODE }
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
        /// Sends the specified key the specified number of times.
        /// </summary>
        /// <param name="key">Key stroke to send.</param>
        /// <param name="times">Number of times to send the <paramref name="key"/>.</param>
        public static void SendKeystrokes(Keys key, int times)
        {
            if (times > 0)
                SendKeystrokes(Enumerable.Repeat((object) key, times));
        }

        /// <summary>
        /// Sends key strokes equivalent to typing the specified text.
        /// </summary>
        public static void SendKeystrokesForText(string text)
        {
            if (!string.IsNullOrEmpty(text))
                SendKeystrokes(text.Cast<object>());
        }

        /// <summary>
        /// Reads the specified file and computes the SHA1 hash function from its contents.
        /// </summary>
        /// <param name="path">Path to the file to compute SHA1 hash function from.</param>
        /// <returns>Result of the SHA1 hash function as a string of hexadecimal digits.</returns>
        public static string Sha1(string path)
        {
            using (var f = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return SHA1.Create().ComputeHash(f).ToHex();
        }

        /// <summary>
        /// Reads the specified file and computes the MD5 hash function from its contents.
        /// </summary>
        /// <param name="path">Path to the file to compute MD5 hash function from.</param>
        /// <returns>Result of the MD5 hash function as a string of hexadecimal digits.</returns>
        public static string Md5(string path)
        {
            using (var f = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return MD5.Create().ComputeHash(f).ToHex();
        }

        /// <summary>
        /// Returns the version of the entry assembly (the .exe file) in a standard format.
        /// </summary>
        public static string VersionOfExe()
        {
            var v = Assembly.GetEntryAssembly().GetName().Version;
            return "{0}.{1}.{2} ({3})".Fmt(v.Major, v.Minor, v.Build, v.Revision); // in our use: v.Build is build#, v.Revision is p4 changelist
        }

        /// <summary>Checks the specified condition and causes the debugger to break if it is false. Throws an <see cref="InternalErrorException"/> afterwards.</summary>
        public static void Assert(bool assertion)
        {
            if (!assertion)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                throw new InternalErrorException("Assertion failure");
            }
        }

        /// <summary>Determines whether the Ctrl key is pressed.</summary>
        public static bool Ctrl { get { return Control.ModifierKeys.HasFlag(Keys.Control); } }
        /// <summary>Determines whether the Alt key is pressed.</summary>
        public static bool Alt { get { return Control.ModifierKeys.HasFlag(Keys.Alt); } }
        /// <summary>Determines whether the Shift key is pressed.</summary>
        public static bool Shift { get { return Control.ModifierKeys.HasFlag(Keys.Shift); } }
    }
}
