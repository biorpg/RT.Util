﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Xml;
using RT.Util.Dialogs;
using RT.Util.ExtensionMethods;
using RT.Util.Xml;

namespace RT.Util
{
    /// <summary>
    /// Provides convenience methods for loading and saving application settings.
    /// </summary>
    public static class SettingsUtil
    {
        /// <summary>
        /// Specifies what to do in case of operation failing.
        /// </summary>
        public enum OnFailure
        {
            /// <summary>Just ignore the failure: no exceptions thrown, no dialogs shown</summary>
            DoNothing,
            /// <summary>Throw an exception in case of failure</summary>
            Throw,
            /// <summary>Ask the user to retry or to skip operation. No exceptions thrown.</summary>
            ShowRetryOnly,
            /// <summary>Ask the user to retry, skip operation or cancel. <see cref="CancelException"/> thrown on cancel.</summary>
            ShowRetryWithCancel,
        }

        /// <summary>
        /// Indicates that the user chose to cancel the current operation.
        /// </summary>
        public sealed class CancelException : RTException
        {
            /// <summary>Creates an exception instance with the specified message.</summary>
            public CancelException()
                : base("User chose to cancel the operation")
            { }
        }

        /// <summary>
        /// Loads settings into the specified class, or, if not available, creates
        /// a new instance of the class.
        /// </summary>
        /// <param name="settings">Destination - the settings class will be placed here</param>
        /// <param name="filename">The name of the file to load the settings from</param>
        private static void LoadSettings<TSettings>(out TSettings settings, string filename) where TSettings : new()
        {
            if (!File.Exists(filename))
            {
                settings = new TSettings();
            }
            else
            {
                try
                {
                    settings = deserialize<TSettings>(filename);
                }
                catch (XmlException) { settings = new TSettings(); }
                catch (IOException) { settings = new TSettings(); }
                catch (SerializationException) { settings = new TSettings(); }
            }
        }

        /// <summary>
        /// Loads settings into the specified class, or, if not available, creates
        /// a new instance of the class.
        /// </summary>
        /// <remarks>
        /// The type <typeparamref name="TSettings"/> must have the <see cref="SettingsAttribute"/>specified,
        /// otherwise an exception will be thrown.
        /// </remarks>
        /// <param name="settings">Destination - the settings class will be placed here.</param>
        /// <typeparam name="TSettings">The type of the settings class.</typeparam>
        public static void LoadSettings<TSettings>(out TSettings settings) where TSettings : new()
        {
            var type = typeof(TSettings);
            var attr = type.GetCustomAttributes<SettingsAttribute>(false).FirstOrDefault();
            if (attr == null)
                throw new ArgumentException("In order to use this overload of LoadSettings on type {0}, the type must have a {1} on it".Fmt(type.FullName, typeof(SettingsAttribute).FullName), "TSettings");
            LoadSettings(out settings, attr.GetFileName());
        }

        /// <summary>
        /// Saves the specified settings class into the appropriate location.
        /// </summary>
        /// <param name="settings">The settings class to be saved</param>
        /// <param name="settingsType">The type of the settings object.</param>
        /// <param name="filename">The name of the file to load the settings from</param>
        /// <param name="onFailure">Specifies how failures should be handled</param>
        private static void SaveSettings(object settings, Type settingsType, string filename, OnFailure onFailure)
        {
            if (onFailure == OnFailure.Throw)
            {
                serialize(settings, settingsType, filename);
            }
            else if (onFailure == OnFailure.DoNothing)
            {
                try { serialize(settings, settingsType, filename); }
                catch { }
            }
            else
            {
                while (true)
                {
                    try
                    {
                        serialize(settings, settingsType, filename);
                        break;
                    }
                    catch (Exception e)
                    {
                        var choices = new List<string>() { "Try &again", "&Don't save settings" };
                        if (onFailure == OnFailure.ShowRetryWithCancel)
                            choices.Add("&Cancel");
                        int choice = DlgMessage.ShowWarning("Program settings could not be saved.\n({0})\n\nWould you like to try again?".Fmt(e.Message), choices.ToArray());
                        if (choice == 1)
                            return;
                        if (choice == 2)
                            throw new CancelException();
                    }
                };
            }
        }

        /// <summary>
        /// Saves the specified settings class into the appropriate location.
        /// </summary>
        /// <remarks>
        /// The type <paramref name="settingsType"/> must have the <see cref="SettingsAttribute"/>specified,
        /// otherwise an exception will be thrown.
        /// </remarks>
        /// <param name="settings">The settings class to be saved</param>
        /// <param name="settingsType">The type of the settings object.</param>
        /// <param name="onFailure">Specifies how failures should be handled</param>
        internal static void SaveSettings(object settings, Type settingsType, OnFailure onFailure)
        {
            var attr = settingsType.GetCustomAttributes<SettingsAttribute>(false).FirstOrDefault();
            if (attr == null)
                throw new ArgumentException("In order to use this overload of SaveSettings on type {0}, the type must have a {1} on it".Fmt(settingsType.FullName, typeof(SettingsAttribute).FullName), "TSettings");
            SaveSettings(settings, settingsType, attr.GetFileName(), onFailure);
        }

        /// <summary>
        /// Saves the specified settings class into the appropriate location.
        /// </summary>
        /// <typeparam name="TSettings">The type of the settings object.</typeparam>
        /// <param name="settings">The settings class to be saved</param>
        /// <param name="filename">The name of the file to load the settings from</param>
        /// <param name="onFailure">Specifies how failures should be handled</param>
        internal static void SaveSettings<TSettings>(TSettings settings, string filename, OnFailure onFailure)
        {
            SaveSettings(settings, typeof(TSettings), filename, onFailure);
        }

        /// <summary>
        /// Saves the specified settings class into the appropriate location.
        /// </summary>
        /// <remarks>
        /// The type <typeparamref name="TSettings"/> must have the <see cref="SettingsAttribute"/>specified,
        /// otherwise an exception will be thrown.
        /// </remarks>
        /// <typeparam name="TSettings">The type of the settings object.</typeparam>
        /// <param name="settings">The settings class to be saved</param>
        /// <param name="onFailure">Specifies how failures should be handled</param>
        public static void SaveSettings<TSettings>(TSettings settings, OnFailure onFailure)
        {
            SaveSettings(settings, typeof(TSettings), onFailure);
        }

        private static void serialize(object settings, Type settingsType, string filename)
        {
            var attr = settingsType.GetCustomAttributes<SettingsAttribute>(false).FirstOrDefault();
            switch (attr.Serializer)
            {
                case SettingsSerializer.XmlClassify:
                    XmlClassify.SaveObjectToXmlFile(settings, settingsType, filename);
                    break;
                case SettingsSerializer.DotNetBinary:
                    var bf = new BinaryFormatter();
                    using (var fs = File.Open(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                        bf.Serialize(fs, settings);
                    break;
                default:
                    throw new InternalErrorException("4968453");
            }
        }

        private static TSettings deserialize<TSettings>(string filename) where TSettings : new()
        {
            var attr = typeof(TSettings).GetCustomAttributes<SettingsAttribute>(false).FirstOrDefault();
            switch (attr.Serializer)
            {
                case SettingsSerializer.XmlClassify:
                    return XmlClassify.LoadObjectFromXmlFile<TSettings>(filename);
                case SettingsSerializer.DotNetBinary:
                    var bf = new BinaryFormatter();
                    using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        return (TSettings) bf.Deserialize(fs);
                default:
                    throw new InternalErrorException("6843184");
            }
        }
    }

    /// <summary>
    /// Provides a base class for settings classes, implementing certain common usage patterns.
    /// See remarks for detailed usage instructions.
    /// </summary>
    /// <remarks>
    /// <para>Derive a class from this one and add the fields you wish to persist. Mark those you don't want stored
    /// with the <see cref="XmlIgnoreAttribute"/>. You must mark the derived class with <see cref="SettingsAttribute"/>
    /// to specify the name for the settings file.</para>
    /// <para>Once the above is done, the settings can be saved by calling <see cref="Save"/>/<see cref="SaveQuiet"/>,
    /// depending on intent. To load, call <see cref="SettingsUtil.LoadSettings&lt;T&gt;(out T)"/>, making sure that the
    /// generic type is the type of your descendant class. For example:
    /// </para>
    /// <code>
    /// static class Program
    /// {
    ///     public static MySettings Settings;
    ///     
    ///     static void Main(string[] args)
    ///     {
    ///         SettingsUtil.LoadSettings(out Settings);
    ///         DoWork();
    ///         Settings.Save();
    ///     }
    /// }
    /// 
    /// [Settings("MyApplicationName")]
    /// class MySettings : SettingsBase
    /// {
    ///     public string SomeSetting = "initial/default value";
    /// }
    /// </code>
    /// 
    /// <para><see cref="SettingsThreadedBase"/> implements an extra method to enable settings to be
    /// queued for a save on a separate thread, to reduce the performance impact of less important saves.</para>
    /// </remarks>
    [Serializable]
    public abstract class SettingsBase
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        [XmlIgnore, NonSerialized]
        protected internal object _lock = new object();
        [XmlIgnore, NonSerialized]
        protected internal Thread _saveThread;
        [XmlIgnore, NonSerialized]
        protected internal SettingsBase _saveObj;
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        /// <summary>
        /// This method is called just before the settings class is written out to disk, allowing any required changes to
        /// be made to the fields. The base implementation does nothing. Note that this may be called on a different thread
        /// than the one invoking a Save* operation (but the same as the thread performing the save immediately after this
        /// method returns).
        /// </summary>
        public virtual void BeforeSave()
        {
        }

        /// <summary>
        /// <para>Saves the settings. Intended to be used whenever it is absolutely vital to save the settings and
        /// bug the user if this fails.</para>
        /// <para>This method is fully compatible with <see cref="SettingsThreadedBase.SaveThreaded"/>,
        /// and will cancel any pending earlier (older) saves.</para>
        /// </summary>
        public virtual void Save()
        {
            // Save must not be interrupted or superseded by a SaveThreaded
            lock (_lock)
            {
                if (_saveThread != null) // this can only ever occur in the Sleep/lock wait phase of the quick save thread
                {
                    _saveThread.Abort();
                    _saveThread = null;
                }
                BeforeSave();
                SettingsUtil.SaveSettings(this, this.GetType(), SettingsUtil.OnFailure.Throw);
            }
        }

        /// <summary>
        /// <para>Saves the settings. Intended to be used whenever it is important to make sure the settings
        /// hit the disk, but the settings are not important enough to bug the user if this fails.</para>
        /// <para>This method is fully compatible with <see cref="SettingsThreadedBase.SaveThreaded"/>,
        /// and will cancel any pending earlier (older) saves.</para>
        /// </summary>
        public virtual void SaveQuiet()
        {
            // SaveQuiet must not be interrupted or superseded by a SaveThreaded
            lock (_lock)
            {
                if (_saveThread != null) // this can only ever occur in the Sleep/lock wait phase of the quick save thread
                {
                    _saveThread.Abort();
                    _saveThread = null;
                }
                BeforeSave();
                SettingsUtil.SaveSettings(this, this.GetType(), SettingsUtil.OnFailure.DoNothing);
            }
        }
    }

    /// <summary>
    /// Like <see cref="SettingsBase"/>, but implements an additional save method.
    /// </summary>
    public abstract class SettingsThreadedBase : SettingsBase
    {
        /// <summary>
        /// Must return a deep clone of this class. This will be used to create a snapshot of the settings
        /// at the time when <see cref="SaveThreaded"/> is called.
        /// </summary>
        protected abstract SettingsThreadedBase CloneForSaveThreaded();

        /// <summary>
        /// <para>Saves the settings. Intended for frequent use at any point where it would make sense to
        /// commit settings, but would not make sense to bug the user about any failures. This method
        /// is like <see cref="SettingsBase.SaveQuiet"/>, except that the actual save occurs slightly later on a separate
        /// thread. The method returns as soon as <see cref="CloneForSaveThreaded"/> returns.</para>
        /// <para>Note that this method is NOT guaranteed to save settings, but it usually will. Make sure
        /// you call <see cref="SettingsBase.Save"/> when you want to guarantee a save, especially just before the
        /// program terminates.</para>
        /// </summary>
        public virtual void SaveThreaded()
        {
            lock (_lock)
            {
                _saveObj = CloneForSaveThreaded();
                if (_saveObj == null)
                    throw new InvalidOperationException("CloneForSaveThreaded returned null.");
                if (_saveThread == null)
                {
                    _saveThread = new Thread(saveThreadFunc);
                    _saveThread.IsBackground = true;
                    _saveThread.Start();
                }
            }
        }

        private void saveThreadFunc()
        {
            Thread.Sleep(2000);
            lock (_lock)
            {
                _saveObj.BeforeSave();
                SettingsUtil.SaveSettings(_saveObj, _saveObj.GetType(), SettingsUtil.OnFailure.DoNothing);
                _saveThread = null;
            }
        }

    }

    /// <summary>
    /// Determines what the settings in the settings file are logically "attached" to.
    /// </summary>
    public enum SettingsKind
    {
        /// <summary>
        /// These settings are specific to a particular computer.
        /// In normal mode: shared among all user accounts, and do not roam.
        /// In portable mode: separate settings stored for every machine name; user account doesn't matter.
        /// </summary>
        MachineSpecific,

        /// <summary>
        /// These settings are specific to a particular user.
        /// In normal mode: separate settings for each user account; will roam to other machines if roaming is configured.
        /// In portable mode: always shared; user account and machine name do not matter.
        /// </summary>
        UserSpecific,

        /// <summary>
        /// These settings are specific to a particular combination of user and machine.
        /// In normal mode: separate settings for each user account on each machine; will not roam.
        /// In portable mode: separate settings stored for every machine name; user account doesn't matter.
        /// </summary>
        UserAndMachineSpecific,

        /// <summary>
        /// These settings are intended to be global, with constraints imposed by reality.
        /// In normal mode: shared among all user accounts, and do not roam.
        /// In portable mode: always shared; user account and machine name do not matter.
        /// </summary>
        Global,
    }

    /// <summary>
    /// Determines which serializer the settings are read/written by.
    /// </summary>
    public enum SettingsSerializer
    {
        /// <summary>Use the XmlClassify serializer.</summary>
        XmlClassify,
        /// <summary>Use the .NET binary serializer.</summary>
        DotNetBinary,
    }

    /// <summary>
    /// Describes the intended usage of a "settings" class to <see cref="SettingsUtil"/> methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false), RummageKeepUsersReflectionSafe]
    public sealed class SettingsAttribute : Attribute
    {
        /// <summary>
        /// Creates an instance of this attribute.
        /// </summary>
        /// <param name="appName">The name of the settings file is formed from this <paramref name="appName"/>
        /// according to certain rules. This should be a string equal to the name of the application. Paths and extensions
        /// should be omitted. It is important to specify the same name for settings of different <paramref name="kind"/>,
        /// because this allows their portability to be controlled with the same {name}.IsPortable.txt file.</param>
        /// <param name="kind">Specifies what the settings in this settings class are logically "attached" to.</param>
        /// <param name="serializer">Specifies which serializer to use.</param>
        public SettingsAttribute(string appName, SettingsKind kind, SettingsSerializer serializer = SettingsSerializer.XmlClassify)
        {
            AppName = appName;
            Kind = kind;
            Serializer = serializer;
        }

        /// <summary>
        /// The name of the settings file is formed from this <see cref="AppName"/> according to certain rules.
        /// This should normally be a string equal to the name of the application. Paths and
        /// extensions should be omitted.
        /// </summary>
        public string AppName { get; private set; }

        /// <summary>
        /// Specifies what the settings in this settings class are logically "attached" to.
        /// </summary>
        public SettingsKind Kind { get; private set; }

        /// <summary>
        /// Specifies which serializer is used to read/write the settings file.
        /// </summary>
        public SettingsSerializer Serializer { get; private set; }

        /// <summary>
        /// Returns the file name that should be used to store the settings class marked with this attribute. Note that
        /// the return value may change depending on external factors (the existence of a file). The recommended
        /// approach is to load settings once, and then save them whenever necessary to whichever path is returned
        /// by this function.
        /// </summary>
        public string GetFileName()
        {
            string filename = AppName;
            switch (Kind)
            {
                case SettingsKind.UserSpecific:
                    // AppName.settings.xml is the user-specific machine-independent settings file; also ensures backwards compatibility
                    break;
                case SettingsKind.UserAndMachineSpecific:
                    // AppName.SIRIUS.settings.xml is the user-specific machine-specific settings file
                    filename += "." + Environment.MachineName;
                    break;
                case SettingsKind.MachineSpecific:
                    // AppName.AllUsers.SIRIUS.settings.xml is a rare special case for a portable app
                    filename += ".AllUsers." + Environment.MachineName;
                    break;
                case SettingsKind.Global:
                    // AppName.Global.settings.xml - need a separate name to user-specific, so add the suffix.
                    filename += ".Global";
                    break;
                default:
                    throw new InternalErrorException("unreachable (97628)");
            }
            filename = filename.FilenameCharactersEscape() + ".Settings." + (Serializer == SettingsSerializer.XmlClassify ? "xml" : "bin");

            if (File.Exists(PathUtil.AppPathCombine(AppName + ".IsPortable.txt")))
            {
                return PathUtil.AppPathCombine(filename);
            }
            else
            {
                switch (Kind)
                {
                    case SettingsKind.Global:
                    case SettingsKind.MachineSpecific:
                        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppName, filename);
                    case SettingsKind.UserSpecific:
                        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, filename);
                    case SettingsKind.UserAndMachineSpecific:
                        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName, filename);
                    default:
                        throw new InternalErrorException("unreachable (97629)");
                }
            }
        }
    }
}
