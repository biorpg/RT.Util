﻿using System;
using System.Linq;
using RT.Util.ExtensionMethods;

namespace RT.Util.Lingo
{
    /// <summary>Represents a translatable string.</summary>
    public class TrString
    {
        /// <summary>Contains the current translation of this string, or for the original language, the current original text.</summary>
        public string Translation;

        /// <summary>Contains the original text this string was last translated from, or for the original language, the empty string.</summary>
        public string OldEnglish = "";

        /// <summary>Default constructor (required for XmlClassify).</summary>
        public TrString() { }
        /// <summary>Constructs a new translatable string given the specified translation.</summary>
        public TrString(string translation) { Translation = translation; }

        /// <summary>Implicit cast from string to TrString.</summary>
        public static implicit operator TrString(string translation) { return new TrString(translation); }

        /// <summary>Implicit cast from TrString to string.</summary>
        public static implicit operator string(TrString translatable) { return translatable.Translation; }

        /// <summary>Formats a string using <see cref="string.Format(string, object[])"/>.</summary>
        public string Fmt(params object[] args) { return string.Format(Translation, args); }

        /// <summary>Formats a string using <see cref="string.Format(string, object)"/>.</summary>
        public string Fmt(object arg0) { return string.Format(Translation, arg0); }

        /// <summary>Formats a string using <see cref="string.Format(string, object, object)"/>.</summary>
        public string Fmt(object arg0, object arg1) { return string.Format(Translation, arg0, arg1); }

        /// <summary>Formats a string using <see cref="string.Format(string, object, object, object)"/>.</summary>
        public string Fmt(object arg0, object arg1, object arg2) { return string.Format(Translation, arg0, arg1, arg2); }

        /// <summary>Returns the translation.</summary>
        /// <returns>The translation.</returns>
        public override string ToString() { return Translation; }
    }

    /// <summary>Represents a translatable string.</summary>
    public class TrStringNumbers
    {
        /// <summary>Specifies which of the interpolated objects are integers.</summary>
        private bool[] IsNumber;

        /// <summary>Contains the current translation of this string, or for the original language, the current original text.</summary>
        public string[] Translations;

        /// <summary>Contains the original text this string was last translated from, or for the original language, the empty array.</summary>
        public string[] OldEnglish = new string[0];

        /// <summary>Default constructor (required for XmlClassify).</summary>
        public TrStringNumbers() { }

        /// <summary>Constructs a new translatable string with the specified translations.</summary>
        /// <param name="translations">Specifies the translations for this string. The number of elements is expected to be equal to the number
        /// of strings as defined by your native language's NumberSystem, multiplied by the number of elements in <paramref name="isNumber"/> that are true.</param>
        /// <param name="isNumber">Specifies which of the interpolated variables are integers.</param>
        /// <example>
        /// The following example code demonstrates how to instantiate a string that interpolates both a string (file name) and an integer (number of bytes) correctly.
        /// <code>
        /// TrStringNumbers MyString = new TrStringNumbers(new[] { "The file {0} contains {1} byte.", "The file {0} contains {1} bytes." }, new[] { false, true });
        /// </code>
        /// </example>
        public TrStringNumbers(string[] translations, bool[] isNumber) { Translations = translations; IsNumber = isNumber; }

        /// <summary>Constructs a new translatable string with one interpolated integer and no other interpolated arguments, and the specified translations.</summary>
        public TrStringNumbers(params string[] translations) { Translations = translations; IsNumber = new[] { true }; }

        /// <summary>Selects the correct string and interpolates the specified arguments.</summary>
        public string Fmt(NumberSystem ns, params object[] args)
        {
            int n = 0;
            int m = 1;
            for (int i = 0; i < IsNumber.Length; i++)
            {
                if (IsNumber[i])
                {
                    if (!(args[i] is int))
                        throw new ArgumentException("Argument #{0} was expected to be an integer, but a {1} was given.".Fmt(i, args[i].GetType().FullName), "nums");
                    n += ns.GetString((int) args[i]) * m;
                    m *= ns.NumStrings;
                }
            }
            return Translations[n].Fmt(args);
        }
    }
}