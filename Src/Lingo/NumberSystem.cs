﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.Lingo
{
    /// <summary>Abstract class to represent a number system.</summary>
    public abstract class NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public abstract int NumStrings { get; }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public abstract int GetString(int n);
    }

    /// <summary>Number system with only one string for all numbers (e.g. Japanese, Korean, Vietnamese, Turkish).</summary>
    public class OneStringNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 1; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return 0; }
    }

    /// <summary>Number system with a string for 1 and another for all other numbers (e.g. Danish, Dutch, English, Faroese, German, Norwegian, Swedish, Estonian, Finnish, Greek, Hebrew, Italian, Portuguese, Spanish, Esperanto).</summary>
    public class Singular1PluralNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 2; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n != 1 ? 1 : 0; }
    }

    /// <summary>Number system with a string for 0 and 1 and another for all other numbers (e.g. French, Brazilian Portuguese).</summary>
    public class Singular01PluralNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 2; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n > 1 ? 1 : 0; }
    }

    /// <summary>Number system for Latvian: one string for numbers ending in 1 but not 11; one for everything else but 0; and one for 0.</summary>
    public class LatvianNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 3; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n % 10 == 1 && n % 100 != 11 ? 0 : n != 0 ? 1 : 2; }
    }

    /// <summary>Number system for Irish: one string for 1, one for 2, one for all other numbers.</summary>
    public class IrishNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 3; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n == 1 ? 0 : n == 2 ? 1 : 2; }
    }

    /// <summary>Number system for Romanian: one string for 1; one for 0 and for all numbers ending in something between 01 and 19; and one for all other numbers.</summary>
    public class RomanianNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 3; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n == 1 ? 0 : (n == 0 || (n % 100 > 0 && n % 100 < 20)) ? 1 : 2; }
    }

    /// <summary>Number system for Lithuanian: one string for numbers ending in 1 but not 11; one for numbers ending in 2-9 but not 12-19; and one for all other numbers.</summary>
    public class LithuanianNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 3; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n % 10 == 1 && n % 100 != 11 ? 0 : n % 10 >= 2 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2; }
    }

    /// <summary>One string for numbers ending in 1 but not 11; one for numbers ending in 2-4 but not 12-14; and one for all other numbers (e.g. Croatian, Serbian, Russian, Ukrainian).</summary>
    public class SlavicNumberSystem1 : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 3; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n % 10 == 1 && n % 100 != 11 ? 0 : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2; }
    }

    /// <summary>One string for 1, one for 2-4, and one for all other numbers (e.g. Slovak, Czech).</summary>
    public class SlavicNumberSystem2 : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 3; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return (n == 1) ? 0 : (n >= 2 && n <= 4) ? 1 : 2; }
    }

    /// <summary>Number system for Polish: one string for 1; one for numbers ending in 2-4 but not 12-14; and one for all other numbers.</summary>
    public class PolishNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 3; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n == 1 ? 0 : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2; }
    }

    /// <summary>Number system for Slovenian: one string for 1; one for 2; one for 3 and 4; and one for all other numbers.</summary>
    public class SlovenianNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 4; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n % 100 == 1 ? 0 : n % 100 == 2 ? 1 : n % 100 == 3 || n % 100 == 4 ? 2 : 3; }
    }

    /// <summary>Number system for Tagalog (Filipino): one string for numbers ending in 4, 6, or 9; one for all other numbers.</summary>
    public class TagalogNumberSystem : NumberSystem
    {
        /// <summary>Returns the number of strings required by this number system.</summary>
        public override int NumStrings { get { return 2; } }
        /// <summary>Determines which string (numbered from 0) should be used for the number <paramref name="n"/>.</summary>
        public override int GetString(int n) { return n % 10 == 4 || n % 10 == 6 || n % 10 == 9 ? 1 : 0; }
    }
}