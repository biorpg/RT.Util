﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using RT.Util.Collections;
using RT.Util.Xml;

namespace RT.Util
{
    /// <summary>
    /// Use this attribute on a type that contains translations for a form. <see cref="Lingo.TranslateControl"/> will automatically add missing fields to the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class LingoDebugAttribute : Attribute
    {
        /// <summary>
        /// Specifies the relative path from the compiled assembly to the source file of the translation type.
        /// </summary>
        public string RelativePath { get; set; }
    }

    /// <summary>
    /// Static class with helper methods to support multi-language applications.
    /// </summary>
    public static class Lingo
    {
        /// <summary>
        /// Generates a list of menu items for the user to select a language from. The list is generated from the set of available XML files in the application's directory.
        /// </summary>
        /// <typeparam name="Translation">The type in which translations stored.</typeparam>
        /// <param name="filemask">A file mask, e.g. "Project.*.xml", narrowing down which files may be valid translation files. Files that match both this and fileregex will be considered.</param>
        /// <param name="fileregex">A regular expression stating which file names are acceptable for valid translation files. Files that match both filemask and this will be considered.</param>
        /// <param name="thisLanguageGetter">A function taking an instance of the Translation class and returning a string specifying the name of the language (e.g. "English (GB)").</param>
        /// <param name="setLanguage">A callback function to call when the user clicks on a menu item. The first parameter to the callback function is the Translation object for the selected language.
        /// The second parameter is the regular expression match object returned by fileregex for this file's filename. The second parameter is null for the native language, which is generated by
        /// calling the Translation type's constructor rather than by loading a file.</param>
        /// <returns></returns>
        public static IEnumerable<MenuItem> LanguageMenuItems<Translation>(string filemask, string fileregex, Func<Translation, string> thisLanguageGetter, Action<Translation, Match> setLanguage) where Translation : new()
        {
            // Generate the context menu for language selection
            var languageList = new List<Tuple<Translation, Match>>();
            languageList.Add(new Tuple<Translation, Match>(new Translation(), null));
            foreach (var file in new DirectoryInfo(Path.GetDirectoryName(Application.ExecutablePath)).GetFiles(filemask))
            {
                try
                {
                    var match = Regex.Match(file.Name, fileregex);
                    if (!match.Success) continue;
                    var transl = XmlClassify.LoadObjectFromXmlFile<Translation>(file.FullName);
                    languageList.Add(new Tuple<Translation, Match>(transl, match));
                }
                catch { }
            }
            return languageList.OrderBy(tup => thisLanguageGetter(tup.E1)).Select(tup => new MenuItem("&" + thisLanguageGetter(tup.E1), new EventHandler((snd, ev) =>
            {
                var t = (Tuple<Translation, Match>) ((MenuItem) snd).Tag;
                setLanguage(t.E1, t.E2);
            })) { Tag = tup });
        }

        /// <summary>
        /// Generates a list of menu items for the user to select a language from. The list is generated from the set of available XML files in the application's directory.
        /// </summary>
        /// <typeparam name="Translation">The type in which translations stored.</typeparam>
        /// <param name="filemask">A file mask, e.g. "Project.*.xml", narrowing down which files may be valid translation files. Files that match both this and fileregex will be considered.</param>
        /// <param name="fileregex">A regular expression stating which file names are acceptable for valid translation files. Files that match both filemask and this will be considered.</param>
        /// <param name="thisLanguageGetter">A function taking an instance of the Translation class and returning a string specifying the name of the language (e.g. "English (GB)").</param>
        /// <param name="setLanguage">A callback function to call when the user clicks on a menu item. The first parameter to the callback function is the Translation object for the selected language.
        /// The second parameter is the regular expression match object returned by fileregex for this file's filename. The second parameter is null for the native language, which is generated by
        /// calling the Translation type's constructor rather than by loading a file.</param>
        /// <returns></returns>
        public static IEnumerable<ToolStripMenuItem> LanguageToolStripMenuItems<Translation>(string filemask, string fileregex, Func<Translation, string> thisLanguageGetter, Action<Translation, Match> setLanguage) where Translation : new()
        {
            // Generate the context menu for language selection
            var languageList = new List<Tuple<Translation, Match>>();
            languageList.Add(new Tuple<Translation, Match>(new Translation(), null));
            foreach (var file in new DirectoryInfo(Path.GetDirectoryName(Application.ExecutablePath)).GetFiles(filemask))
            {
                try
                {
                    var match = Regex.Match(file.Name, fileregex);
                    if (!match.Success) continue;
                    var transl = XmlClassify.LoadObjectFromXmlFile<Translation>(file.FullName);
                    languageList.Add(new Tuple<Translation, Match>(transl, match));
                }
                catch { }
            }
            return languageList.OrderBy(tup => thisLanguageGetter(tup.E1)).Select(tup => new ToolStripMenuItem("&" + thisLanguageGetter(tup.E1), null, new EventHandler((snd, ev) =>
            {
                var t = (Tuple<Translation, Match>) ((ToolStripMenuItem) snd).Tag;
                setLanguage(t.E1, t.E2);
            })) { Tag = tup });
        }

        /// <summary>
        /// Translates the text of the specified control and all its sub-controls using the specified translation object.
        /// </summary>
        /// <param name="control">Control whose text is to be translated.</param>
        /// <param name="translation">Object containing the translations. Use [TranslationDebug] attribute on the class you use for this.</param>
        public static void TranslateControl(Control control, object translation)
        {
            translateControl(control, translation);
        }

        private static string translate(string key, object translation, object control)
        {
            var translationType = translation.GetType();

            FieldInfo field = translationType.GetField(key);
            if (field != null)
                return field.GetValue(translation).ToString();

            PropertyInfo property = translationType.GetProperty(key);
            if (property != null)
                return property.GetValue(translation, null).ToString();

            MethodInfo method = translationType.GetMethod(key, new Type[] { typeof(Control) });
            if (method != null)
                return method.Invoke(translation, new object[] { control }).ToString();

            return null;
        }

        private static void translateControl(Control control, object translation)
        {
            if (control == null)
                return;

            if (!string.IsNullOrEmpty(control.Name))
            {
                if (!string.IsNullOrEmpty(control.Text) && (!(control.Tag is string) || ((string) control.Tag != "notranslate")))
                {
                    string translated = translate(control.Name, translation, control);
                    if (translated != null)
                        control.Text = translated;
#if DEBUG
                    else
                        setMissingTranslation(translation, control.Name, control.Text);
#endif
                }
            }

            if (control is ToolStrip)
                foreach (ToolStripItem tsi in ((ToolStrip) control).Items)
                    translateToolStripItem(tsi, translation);
            foreach (Control subcontrol in control.Controls)
                translateControl(subcontrol, translation);
        }

        private static void translateToolStripItem(ToolStripItem tsi, object translation)
        {
            if (!string.IsNullOrEmpty(tsi.Name))
            {
                if (!string.IsNullOrEmpty(tsi.Text) && (!(tsi.Tag is string) || ((string) tsi.Tag != "notranslate")))
                {
                    string translated = translate(tsi.Name, translation, tsi);
                    if (translated != null)
                        tsi.Text = translated;
#if DEBUG
                    else
                        setMissingTranslation(translation, tsi.Name, tsi.Text);
#endif
                }
            }
            if (tsi is ToolStripDropDownItem)
            {
                foreach (ToolStripItem subitem in ((ToolStripDropDownItem) tsi).DropDownItems)
                    translateToolStripItem(subitem, translation);
            }
        }

        private static void setMissingTranslation(object translation, string key, string origText)
        {
            var translationType = translation.GetType();
            var attributes = translationType.GetCustomAttributes(typeof(LingoDebugAttribute), false);
            if (!attributes.Any())
                throw new Exception("Your translation type must have a [LingoDebug(...)] attribute which specifies the relative path from the compiled assembly to the source of that translation type.");

            var translationDebugAttribute = (LingoDebugAttribute) attributes.First();
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), translationDebugAttribute.RelativePath);
            string source = File.ReadAllText(path);
            var match = Regex.Match(source, @"^(\s*)#endregion", RegexOptions.Multiline);
            if (match.Success)
            {
                source = source.Substring(0, match.Index) + match.Groups[1].Value + "public string " + key + " = \"" + origText + "\";\n" + source.Substring(match.Index);
                File.WriteAllText(path, source);
            }
        }
    }
}
