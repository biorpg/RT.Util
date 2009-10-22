﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;
using System.Xml.Linq;

namespace RT.Util
{
    public static class EggsML
    {
        internal static char[] specialChars = new char[] { '~', '@', '#', '$', '%', '^', '&', '*', '_', '=', '+', '/', '\\', '[', ']', '{', '}', '<', '>', '|', '`', '"' };

        public static EggsNode Parse(string input)
        {
            if (input == null || input.Contains('\0'))
                throw new ArgumentException("Argument cannot be null or contain null characters.", "input");

            var curTag = new EggsTag('\0', 0);
            if (input.Length == 0)
                return curTag;

            var index = 0;
            var stack = new Stack<EggsTag>();

            while (input.Length > 0)
            {
                var pos = input.IndexOf(ch => specialChars.Contains(ch));

                if (pos == -1)
                {
                    curTag.Add(input);
                    break;
                }

                if (pos < input.Length - 1 && input[pos + 1] == input[pos])
                {
                    curTag.Add(input.Substring(0, pos + 1));
                    input = input.Substring(pos + 2);
                    index += pos + 2;
                    continue;
                }

                if (pos > 0)
                {
                    curTag.Add(input.Substring(0, pos));
                    input = input.Substring(pos);
                    index += pos;
                    continue;
                }

                switch (input[0])
                {
                    case '`':
                        input = input.Substring(1);
                        index++;
                        continue;

                    case '"':
                        pos = input.IndexOf('"', 1);
                        if (pos == -1)
                            throw new EggsMLParseException(@"Closing '""' missing", index + 1);
                        while (pos < input.Length - 1 && input[pos + 1] == '"')
                        {
                            pos = input.IndexOf('"', pos + 2);
                            if (pos == -1)
                                throw new EggsMLParseException(@"Closing '""' missing", index + 1);
                        }
                        curTag.Add(input.Substring(1, pos).Replace("\"\"", "\""));
                        input = input.Substring(pos + 1);
                        index += pos + 1;
                        continue;

                    case '|':
                        curTag.Children.Add(new List<EggsNode>());
                        input = input.Substring(1);
                        index++;
                        continue;

                    default:
                        bool last = input.Length == 1;
                        // Are we opening a new tag?
                        if ((alwaysOpens(input[0]) || input[0] != opposite(curTag.Tag) || (!last && input[1] == '|')) && !alwaysCloses(input[0]))
                        {
                            stack.Push(curTag);
                            var i = (!last && input[1] == '|') ? 2 : 1;
                            index += i;
                            curTag = new EggsTag(input[0], index);
                            input = input.Substring(i);
                            continue;
                        }
                        // Are we closing a tag?
                        else if (input[0] == opposite(curTag.Tag))
                        {
                            var prevTag = stack.Pop();
                            prevTag.Add(curTag);
                            curTag = prevTag;
                            input = input.Substring(1);
                            index++;
                            continue;
                        }
                        else if (alwaysCloses(input[0]))
                            throw new EggsMLParseException(@"Tag '{0}' unexpected".Fmt(input[0]), index + 1);
                        throw new EggsMLParseException(@"Character '{0}' unexpected".Fmt(input[0]), index + 1);
                }
            }

            if (stack.Count > 0)
                throw new EggsMLParseException(@"Closing '{0}' missing".Fmt(opposite(curTag.Tag)), curTag.Index);

            return curTag;
        }

        internal static char opposite(char p)
        {
            if (p == '[') return ']';
            if (p == '<') return '>';
            if (p == '{') return '}';
            return p;
        }

        internal static bool alwaysOpens(char p) { return p == '[' || p == '<' || p == '{'; }
        private static bool alwaysCloses(char p) { return p == ']' || p == '>' || p == '}'; }
    }

    public abstract class EggsNode
    {
        public static implicit operator EggsNode(string text) { return new EggsText(text); }
        public abstract object ToXml();
    }
    public class EggsTag : EggsNode
    {
        public char Tag;
        public int Index;
        public List<List<EggsNode>> Children = new List<List<EggsNode>> { new List<EggsNode>() };
        public EggsTag(char tag, int index) { Tag = tag; Index = index; }
        public void Add(EggsNode node) { Children[Children.Count - 1].Add(node); }
        public override string ToString()
        {
            if (Children == null || Children.Count == 0)
                throw new ArgumentException("Children must be present.", "Chidren");

            if (Children.Count == 1 && Children[0].Count == 0)
                return EggsML.alwaysOpens(Tag) ? Tag.ToString() + EggsML.opposite(Tag) : Tag.ToString() + '`' + Tag;

            var sb = new StringBuilder();
            if (Tag != '\0')
                sb.Append(Tag);
            var firstChild = stringifyList(Children[0]);
            if (firstChild.Length == 0 || firstChild[0] == Tag || firstChild[0] == '|')
                sb.Append('`');
            sb.Append(firstChild);
            foreach (var childList in Children.Skip(1))
            {
                sb.Append('|');
                var nextChild = stringifyList(childList);
                if (nextChild.StartsWith("|"))
                    sb.Append('`');
                sb.Append(nextChild);
            }
            if (sb[sb.Length - 1] == Tag && sb[sb.Length - 2] != Tag)
                sb.Append('`');
            if (Tag != '\0')
                sb.Append(EggsML.opposite(Tag));
            return sb.ToString();
        }
        private string stringifyList(List<EggsNode> childList)
        {
            if (childList.Count == 0)
                return string.Empty;
            var sb = new StringBuilder();
            var prev = childList[0].ToString();
            if (childList[0] is EggsTag && ((EggsTag) childList[0]).Tag == Tag && !EggsML.alwaysOpens(Tag))
            {
                sb.Append(Tag);
                sb.Append('|');
                sb.Append(prev.Substring(1));
            }
            else
                sb.Append(prev);
            foreach (var child in childList.Skip(1))
            {
                var next = child.ToString();
                if (child is EggsTag && ((EggsTag) child).Tag == Tag && !EggsML.alwaysOpens(Tag))
                    next = Tag.ToString() + "|" + next.Substring(1);
                if (next.Length > 0 && next[0] == prev[prev.Length - 1])
                    sb.Append('`');
                sb.Append(next);
                prev = next;
            }
            return sb.ToString();
        }
        public override object ToXml()
        {
            string tagName;
            switch (Tag)
            {
                case '\0': tagName = "root"; break;
                case '~': tagName = "tilde"; break;
                case '@': tagName = "at"; break;
                case '#': tagName = "hash"; break;
                case '$': tagName = "dollar"; break;
                case '%': tagName = "percent"; break;
                case '^': tagName = "hat"; break;
                case '&': tagName = "and"; break;
                case '*': tagName = "star"; break;
                case '_': tagName = "underscore"; break;
                case '=': tagName = "equals"; break;
                case '+': tagName = "plus"; break;
                case '/': tagName = "slash"; break;
                case '\\': tagName = "backslash"; break;
                case '[': tagName = "square"; break;
                case '{': tagName = "curly"; break;
                case '<': tagName = "angle"; break;
                default:
                    throw new ArgumentException("Unexpected tag character.", "Tag");
            }
            return new XElement(tagName, Children.Count == 1 ? (object) Children[0].Select(ch => ch.ToXml()) : Children.Select(chlist => new XElement("item", chlist.Select(ch => ch.ToXml()))));
        }
    }
    public class EggsText : EggsNode
    {
        public string Text = string.Empty;
        public EggsText(string text) { Text = text; }
        public override string ToString()
        {
            if (Text.Where(ch => EggsML.specialChars.Contains(ch) && ch != '"').Take(3).Count() >= 3)
                return string.Concat("\"", Text.Replace("\"", "\"\""), "\"");
            return new string(Text.SelectMany(ch => EggsML.specialChars.Contains(ch) ? new char[] { ch, ch } : new char[] { ch }).ToArray());
        }
        public override object ToXml() { return Text; }
    }

    [Serializable]
    public class EggsMLParseException : Exception
    {
        public int Index { get; private set; }
        public EggsMLParseException(string message, int index) : base(message) { Index = index; }
        public EggsMLParseException(string message, int index, Exception inner) : base(message, inner) { Index = index; }
    }
}