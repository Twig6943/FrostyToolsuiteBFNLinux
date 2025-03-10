﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Frosty.Controls;
using System.IO;
using System.Globalization;
using FrostySdk;
using Microsoft.Win32;
using FrostySdk.IO;
using Frosty.ModSupport;
using FrostyModManager.Controls;
using FrostyModManager.Compression;
using System.Text;
using System.ComponentModel;
using Frosty.Hash;
using System.Threading;
using Frosty.Core.Mod;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk.Managers;
using Frosty.Core.IO;
using FrostyCore;
using Frosty.Core.Controls;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Media;

namespace FrostyModManager
{
    public static class Helpers
    {
        public static DependencyObject FindVisualAncestor(this DependencyObject wpfObject, Predicate<DependencyObject> condition)
        {
            while (wpfObject != null)
            {
                if (condition(wpfObject))
                {
                    return wpfObject;
                }

                wpfObject = VisualTreeHelper.GetParent(wpfObject);
            }

            return null;
        }
    }

    public class ModDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IFrostyMod mod = (IFrostyMod)value;
            if (!mod.HasWarnings)
                return mod.ModDetails.Description;

            string desc = "";
            foreach (string warning in mod.Warnings)
                desc += "(WARNING: " + warning + ")\n";
            desc += "\n";
            desc += mod.ModDetails.Description;
            return desc;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ModPrimaryActionTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ModResourceInfo mri = (ModResourceInfo)value;
            List<ModAction> mods = (List<ModAction>)mri.Mods;
            string modName = (string)parameter;

            int index = mods.FindIndex((ModAction a) => a.Name == modName);
            if (index != -1)
            {
                var modAction = mods[index];
                switch (modAction.PrimaryAction)
                {
                    case ModPrimaryActionType.None: return null;
                    case ModPrimaryActionType.Modify: return (index == mri.FirstModToModifyIndex) ? "Resource is initially modified by this mod" : "Resource is replaced by this mod";
                    case ModPrimaryActionType.Add: return (index == mri.FirstModToModifyIndex) ? "Resource is initially added by this mod" : "Resource is replaced by this mod";
                    case ModPrimaryActionType.Merge: return (index == mri.FirstModToModifyIndex) ? "Resource is initially modified by this mod" : "Resource is merged by this mod";
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ModSecondaryActionTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ModResourceInfo mri = (ModResourceInfo)value;
            List<ModAction> mods = (List<ModAction>)mri.Mods;
            string modName = (string)parameter;

            int index = mods.FindIndex((ModAction a) => a.Name == modName);
            if (index != -1)
            {
                var modAction = mods[index];
                if (modAction.SecondaryAction == ModSecondaryActionType.AddToBundle)
                    return "Resource is added to other bundle(s) by this mod";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ModPrimaryActionConverter : IValueConverter
    {
        private static ImageSource blankSource = null;
        private static ImageSource primaryActionModifySource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyModManager;component/Images/PrimaryActionModify.png") as ImageSource;
        private static ImageSource primaryActionReplaceSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyModManager;component/Images/PrimaryActionReplace.png") as ImageSource;
        private static ImageSource primaryActionAddSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyModManager;component/Images/PrimaryActionAdd.png") as ImageSource;
        private static ImageSource primaryActionMergeSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyModManager;component/Images/PrimaryActionMerge.png") as ImageSource;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ModResourceInfo mri = (ModResourceInfo)value;
            List<ModAction> mods = (List<ModAction>)mri.Mods;
            string modName = (string)parameter;

            int index = mods.FindIndex((ModAction a) => a.Name == modName);
            if (index != -1)
            {
                var modAction = mods[index];
                switch (modAction.PrimaryAction)
                {
                    case ModPrimaryActionType.None: return blankSource;
                    case ModPrimaryActionType.Modify: return (index == mri.FirstModToModifyIndex) ? primaryActionModifySource : primaryActionReplaceSource;
                    case ModPrimaryActionType.Add: return (index == mri.FirstModToModifyIndex) ? primaryActionAddSource : primaryActionReplaceSource;
                    case ModPrimaryActionType.Merge: return (index == mri.FirstModToModifyIndex) ? primaryActionModifySource : primaryActionMergeSource;
                }
            }

            return blankSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ModSecondaryActionConverter : IValueConverter
    {
        private static readonly ImageSource BlankSource = null;
        private static readonly ImageSource SecondaryActionAddSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyModManager;component/Images/SecondaryActionAdd.png") as ImageSource;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ModResourceInfo mri = (ModResourceInfo)value;
            List<ModAction> mods = (List<ModAction>)mri.Mods;
            string modName = (string)parameter;

            int index = mods.FindIndex((ModAction a) => a.Name == modName);
            if (index != -1)
            {
                var modAction = mods[index];
                if (modAction.SecondaryAction == ModSecondaryActionType.AddToBundle)
                    return SecondaryActionAddSource;
            }

            return BlankSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public enum ModPrimaryActionType
    {
        None,
        Modify,
        Add,
        Merge
    }
    public enum ModSecondaryActionType
    {
        None,
        AddToBundle
    }

    public class ModAction
    {
        public ModPrimaryActionType PrimaryAction;
        public ModSecondaryActionType SecondaryAction;
        public string Name;
        public string NiceName;
    }

    public class ModResourceInfo
    {
        public string Name { get; }
        public string Type { get; }

        public string ModName
        {
            get
            {
                return mods.LastOrDefault()?.NiceName ?? string.Empty;
            }
        }

        public string Status
        {
            get
            {
                var mod = mods.LastOrDefault();

                if (mod == null)
                {
                    return string.Empty;
                }

                var sb = new StringBuilder();

                switch (mod.PrimaryAction)
                {
                    case ModPrimaryActionType.Add:
                        sb.Append("Add");
                        break;
                    case ModPrimaryActionType.Merge:
                        sb.Append("Merge");
                        break;
                    case ModPrimaryActionType.Modify:
                        sb.Append("Modify");
                        break;
                }

                if (mod.SecondaryAction == ModSecondaryActionType.AddToBundle)
                {
                    sb.Append(", Bundle");
                }

                return sb.ToString();
            }
        }

        public string Conflicts
        {
            get
            {
                return mods.Count > 1 ? "Yes" : "No";
            }
        }

        public IEnumerable<ModAction> Mods => mods;
        public int ModCount => mods.Count;
        public int FirstModToModifyIndex { get; private set; } = -1;

        private readonly int nameHash;
        private List<ModAction> mods = new List<ModAction>();
        private List<int> addBundles = new List<int>();

        public ModResourceInfo(string n, string t)
        {
            Name = n;
            Type = t;
            nameHash = Fnv1.HashString(t + "/" + n);
        }

        public void AddMod(FrostyMod mod, ModPrimaryActionType primaryAction, IEnumerable<int> modAddBundles)
        {
            bool isAdded = false;
            if (modAddBundles != null)
            {
                foreach (int addBundle in modAddBundles)
                {
                    if (!addBundles.Contains(addBundle))
                        isAdded = true;
                    addBundles.Add(addBundle);
                }
            }

            mods.Add(
                new ModAction()
                {
                    Name = mod.Filename,
                    NiceName = mod.ModDetails.Title,
                    PrimaryAction = primaryAction,
                    SecondaryAction = (isAdded) ? ModSecondaryActionType.AddToBundle : ModSecondaryActionType.None
                });
            if (FirstModToModifyIndex == -1)
            {
                if (primaryAction != ModPrimaryActionType.None)
                    FirstModToModifyIndex = mods.Count - 1;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is string s)
            {
                int hash = Fnv1.HashString(s);
                return hash == nameHash;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class PackManifest
    {
        public string name;

        public string managerVersion;
        public int version;
        public List<string> mods;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FrostyWindow
    {
        private List<IFrostyMod> availableMods = new List<IFrostyMod>();
        private List<FrostyPack> packs = new List<FrostyPack>();
        private FrostyPack selectedPack;
        private FileSystem fs;
        private DirectoryInfo modsDir = new DirectoryInfo(Path.Combine("Mods", ProfilesLibrary.ProfileName));

        private static int manifestVersion = 1;

        private Progress<string> DropProgress;

        private IProgress<string> DropProgressReporter => DropProgress;

        private Progress<bool> UpdateConflictsProgress;
        private IProgress<bool> UpdateConflictsProgressReporter => UpdateConflictsProgress;

        private List<ModResourceInfo> ConflictInfos = new List<ModResourceInfo>();
        private int ConflictPage = 0;
        private const int ConflictPageSize = 34;

        public MainWindow()
        {
            InitializeComponent();
            FileLogger.Init();

            TaskbarItemInfo = new System.Windows.Shell.TaskbarItemInfo();

            tabContent.HeaderControl = tabControl;
            availableModsTabContent.HeaderControl = availableModsTabControl;

            DropProgress = new Progress<string>();

            DropProgress.ProgressChanged += (s, arg) =>
            {
                if (string.IsNullOrWhiteSpace(arg))
                {
                    return;
                }

                FrostyWindow_Drop(arg);
            };

            AllowDrop = true;

            UpdateConflictsProgress = new Progress<bool>();

            UpdateConflictsProgress.ProgressChanged += (s, arg) =>
            {
                UpdateConflicts(arg);
            };

            showOnlyReplacementsCheckBox.IsChecked = true;

            if (OperatingSystemHelper.IsWine())
            {
                launchButton.Visibility = Visibility.Collapsed;
                launchButton.IsEnabled = false;
            }
        }

        private void FrostyWindow_FrostyLoaded(object sender, EventArgs e)
        {
            (App.Logger as FrostyLogger).AddBinding(tb, TextBox.TextProperty);

            string gamePath = Config.Get<string>("GamePath", "", ConfigScope.Game);

            if (!ProfilesLibrary.EnableExecution)
            {
                FrostyMessageBox.Show("The selected profile is a read-only profile, and therefore cannot be loaded in the mod manager", "Frosty Mod Manager");
                Closing -= FrostyWindow_Closing;
                Close();
                return;
            }

            fs = new FileSystem(gamePath);

            foreach (FileSystemSource source in ProfilesLibrary.Sources)
            {
                fs.AddSource(source.Path, source.SubDirs);
            }

            fs.Initialize();

            Config.Save();

            Title = "Frosty Mod Manager - " + App.Version + " (" + ProfilesLibrary.DisplayName + ")";

            TypeLibrary.Initialize();
            App.PluginManager.Initialize();

            LoadMenuExtensions();

            if (Directory.Exists(Config.Get<string>("CustomModsDirectory", "")))
            {
                modsDir = new DirectoryInfo(Path.Combine(Config.Get<string>("CustomModsDirectory", ""), ProfilesLibrary.ProfileName));
            }
            else
            {
                App.Logger.Log("Custom Mods Directory does not exist, using default instead");
            }

            FrostyTaskWindow.Show("Loading Mods", "", (logger) =>
            {
                if (!modsDir.Exists)
                {
                    Directory.CreateDirectory(modsDir.FullName);
                }

                int currentMod = 0;
                int totalMods = modsDir.EnumerateFiles().Count();

                // load mods
                Parallel.ForEach(modsDir.EnumerateFiles(), fi =>
                {
                    if (fi.Extension == ".fbmod")
                    {
                        int retCode = 0;
                        using (FileStream stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read))
                            retCode = VerifyMod(stream);

                        if (retCode >= 0)
                        {
                            try
                            {
                                // all good
                                AddMod(fi.FullName, (retCode & 0x8000) != 0 ? 1 : 0);
                            }
                            catch (FrostyModLoadException)
                            {
                                // failed to load for whatever reason
                                File.Delete(fi.FullName);
                                File.Delete(fi.FullName.Replace(".fbmod", "_01.archive"));
                            }
                        }
                        else if (retCode == -3)
                        {
                            // bad mod. delete it
                            File.Delete(fi.FullName);
                            File.Delete(fi.FullName.Replace(".fbmod", "_01.archive"));
                        }
                    }
                    logger.LogProgress(currentMod++ / (float)totalMods * 100d);
                });
                // load collections
                Parallel.ForEach(modsDir.EnumerateFiles(), fi =>
                {
                    if (fi.Extension == ".fbcollection")
                    {
                        AddCollection(fi.FullName, 0);
                    }
                    logger.LogProgress(currentMod++ / (float)totalMods * 100d);
                });
            });
            availableMods = availableMods.OrderBy(o => o.Filename).ToList();
            availableModsList.ItemsSource = availableMods;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(availableModsList.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("ModDetails.Category", null, StringComparison.OrdinalIgnoreCase);
            view.GroupDescriptions.Add(groupDescription);

            foreach (string packName in Config.EnumerateKeys(ConfigScope.Pack))
            //foreach (string profileName in Config.EnumerateKeys("Profiles"))
            {
                string values = Config.Get(packName, "", ConfigScope.Pack);
                //string values = Config.Get<string>("Profiles", profileName, "");
                string[] valuesArray = values.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                FrostyPack pack = new FrostyPack(packName);
                packs.Add(pack);

                for (int i = 0; i < valuesArray.Length; i++)
                {
                    string[] modEnabledPair = valuesArray[i].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    string backupFileName = modEnabledPair[0];
                    bool isEnabled = bool.Parse(modEnabledPair[1]);

                    IFrostyMod mod = availableMods.Find((IFrostyMod a) => a.Filename == modEnabledPair[0]);
                    if (mod == null)
                    {
                        List<IFrostyMod> collections = availableMods.FindAll((IFrostyMod a) => a is FrostyModCollection);
                        foreach (FrostyModCollection collection in collections)
                        {
                            mod = collection.Mods.Find((FrostyMod a) => a.Filename == modEnabledPair[0]);
                            if (mod != null)
                                break;
                        }
                    }

                    pack.AddMod(mod, isEnabled, backupFileName);
                }
            }

            if (packs.Count == 0)
                AddPack("Default");
            packsComboBox.ItemsSource = packs;

            if (App.LaunchGameImmediately)
            {
                int index = packs.FindIndex((FrostyPack a) => a.Name.Equals(App.LaunchProfile, StringComparison.OrdinalIgnoreCase));
                if (index == -1)
                {
                    FrostyMessageBox.Show(string.Format("Unable to find pack with name {0}. Launch request cancelled", App.LaunchProfile), "Frosty Mod Manager");
                    App.LaunchGameImmediately = false;
                }
                else
                {
                    packsComboBox.SelectedIndex = index;
                    launchButton_Click(this, new RoutedEventArgs());
                    return;
                }
            }

            if (toolsMenuItem.Items.Count != 0)
                toolsMenuItem.Items.Add(new Separator());

            MenuItem optionsMenuItem = new MenuItem()
            {
                Header = "Options",
                Icon = new Image() { Source = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyCore;component/Images/Settings.png") as ImageSource },
            };
            optionsMenuItem.Click += optionsMenuItem_Click;
            toolsMenuItem.Items.Add(optionsMenuItem);

            string selectedProfileName = Config.Get<string>("SelectedPack", "", ConfigScope.Game);
            //string selectedProfileName = Config.Get<string>("Application", "SelectedProfile", "");
            int selectedIndex = 0;
            if (selectedProfileName != null)
            {
                selectedIndex = packs.FindIndex((FrostyPack a) => a.Name == selectedProfileName);
                if (selectedIndex == -1)
                    selectedIndex = 0;
            }
            packsComboBox.SelectedIndex = selectedIndex;

            if (Config.Get("CollapseCategories", false))
            {
            }

            LoadedPluginsList.ItemsSource = App.PluginManager.Plugins;

            if (Config.Get("ApplyModOrder", "List") == "List")
            {
                orderComboBox.SelectedIndex = 0;
            }
            else if (Config.Get("ApplyModOrder", "List") == "Priority")
            {
                orderComboBox.SelectedIndex = 1;
            }

            if (Config.Get("FlatpakMessage", OperatingSystemHelper.IsWine()))
            {
                Config.Add("FlatpakMessage", false);

                var message = "If Frosty is run through Flatpak application (Bottles, Lutris, Heroic), then make sure to select 'All user files' in Flatseal for that application.";
                message += "\r\n\r\nOtherwise Frosty Mod Manager might crash.";

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
            }

            GC.Collect();
        }

        private void addProfileButton_Click(object sender, RoutedEventArgs e)
        {
            AddProfileWindow win = new AddProfileWindow();
            win.ShowDialog();

            if (win.DialogResult == true)
            {
                AddPack(win.ProfileName);
            }
        }

        private void packsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedPack = packsComboBox.SelectedItem as FrostyPack;
            appliedModsList.ItemsSource = selectedPack?.AppliedMods;

            if (selectedPack == null)
                return;

            selectedPack.AppliedModsUpdated += SelectedProfile_AppliedModsUpdated;
            selectedPack.Refresh();

            Config.Add("SelectedPack", selectedPack.Name, ConfigScope.Game);
            //Config.Add("Application", "SelectedProfile", selectedProfile.Name);
        }

        private void removeProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (packsComboBox.Items.Count == 1)
            {
                FrostyMessageBox.Show("There must be at least one active pack", "Frosty Mod Manager");
                return;
            }

            if (FrostyMessageBox.Show("Are you sure you want to delete this pack?", "Frosty Mod Manager", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Config.Remove(selectedPack.Name, ConfigScope.Pack);
                //Config.Remove("Profiles", selectedProfile.Name);
                packs.Remove(selectedPack);

                DirectoryInfo di = new DirectoryInfo(fs.BasePath + "ModData\\" + selectedPack.Name);
                if (di.Exists)
                    di.Delete(true);


                packsComboBox.Items.Refresh();
                packsComboBox.SelectedIndex = 0;
            }
        }

        private void packRename_Click(object sender, RoutedEventArgs e)
        {
            AddProfileWindow win = new AddProfileWindow("Rename Pack");
            win.ShowDialog();

            if (win.DialogResult == true)
            {
                string newPackName = win.ProfileName;
                var oldPack = selectedPack;

                FrostyPack existingPack = packs.Find((FrostyPack a) => {
                    return a.Name.CompareTo(newPackName) == 0;
                });

                if (existingPack == null)
                {
                    FrostyPack newPack = new FrostyPack(newPackName);
                    foreach (var mod in oldPack.AppliedMods)
                    {
                        newPack.AppliedMods.Add(mod);
                    }

                    Config.Add(newPackName, ConfigScope.Pack);
                    Config.Remove(oldPack.Name, ConfigScope.Pack);

                    packs.Add(newPack);
                    packs.Remove(oldPack);

                    packsComboBox.Items.Refresh();
                    packsComboBox.SelectedItem = newPack;
                }
                else FrostyMessageBox.Show("A pack with the same name already exists", "Frosty Mod Manager");
            }
        }

        private void packDuplicate_Click(object sender, RoutedEventArgs e)
        {
            AddProfileWindow win = new AddProfileWindow("Duplicate Pack");
            win.ShowDialog();

            if (win.DialogResult == true)
            {
                string newPackName = win.ProfileName;
                var oldPack = selectedPack;

                FrostyPack existingPack = packs.Find((FrostyPack a) => {
                    return a.Name.CompareTo(newPackName) == 0;
                });

                if (existingPack == null)
                {
                    Config.Add(newPackName, ConfigScope.Pack);

                    FrostyPack newPack = new FrostyPack(newPackName);
                    foreach (var mod in oldPack.AppliedMods)
                    {
                        newPack.AppliedMods.Add(mod);
                    }

                    packs.Add(newPack);

                    packsComboBox.Items.Refresh();
                    packsComboBox.SelectedItem = newPack;
                }
                else FrostyMessageBox.Show("A pack with the same name already exists", "Frosty Mod Manager");
            }
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = appliedModsList.SelectedIndex;

            foreach (FrostyAppliedMod mod in appliedModsList.SelectedItems)
                selectedPack.RemoveMod(mod);

            appliedModsList.Items.Refresh();

            appliedModsList.SelectedIndex = selectedIndex;
            updateAppliedModButtons();
        }

        private void upButton_Click(object sender, RoutedEventArgs e)
        {
            if (orderComboBox.SelectedIndex == 0)
            {
                for (int i = 0; i < (Keyboard.IsKeyDown(Key.LeftShift) ? 4 : 1); i++)
                {
                    selectedPack.MoveModsUp(appliedModsList.SelectedItems);
                }

                if (Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    selectedPack.MoveModsTop(appliedModsList.SelectedItems);
                }
            }
            else if (orderComboBox.SelectedIndex == 1)
            {
                for (int i = 0; i < (Keyboard.IsKeyDown(Key.LeftShift) ? 4 : 1); i++)
                {
                    selectedPack.MoveModsDown(appliedModsList.SelectedItems);
                }

                if (Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    selectedPack.MoveModsBottom(appliedModsList.SelectedItems);
                }
            }
            appliedModsList.Items.Refresh();

            updateAppliedModButtons();
        }

        private void downButton_Click(object sender, RoutedEventArgs e)
        {
            if (orderComboBox.SelectedIndex == 0)
            {
                for (int i = 0; i < (Keyboard.IsKeyDown(Key.LeftShift) ? 4 : 1); i++)
                {
                    selectedPack.MoveModsDown(appliedModsList.SelectedItems);
                }

                if (Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    selectedPack.MoveModsBottom(appliedModsList.SelectedItems);
                }
            }
            else if (orderComboBox.SelectedIndex == 1)
            {
                for (int i = 0; i < (Keyboard.IsKeyDown(Key.LeftShift) ? 4 : 1); i++)
                {
                    selectedPack.MoveModsUp(appliedModsList.SelectedItems);
                }

                if (Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    selectedPack.MoveModsTop(appliedModsList.SelectedItems);
                }
            }
            appliedModsList.Items.Refresh();

            updateAppliedModButtons();
        }

        private void installButton_Click(object sender, RoutedEventArgs e)
        {
            Config.Save();

            // initialize
            Frosty.Core.App.FileSystem = new FileSystem(Config.Get<string>("GamePath", "", ConfigScope.Game));
            //FileSystem fs = new FileSystem(Config.Get<string>("Init", "GamePath", ""));
            foreach (FileSystemSource source in ProfilesLibrary.Sources)
                Frosty.Core.App.FileSystem.AddSource(source.Path, source.SubDirs);
            Frosty.Core.App.FileSystem.Initialize();

            // Set selected pack
            App.SelectedPack = selectedPack.Name;

            // get all applied mods
            List<string> modPaths = new List<string>();
            foreach (FrostyAppliedMod mod in selectedPack.AppliedMods)
            {
                if (mod.IsFound && mod.IsEnabled)
                    modPaths.Add(mod.Mod.Filename);
            }

            // combine stored args with launch args
            string additionalArgs = Config.Get<string>("CommandLineArgs", "", ConfigScope.Game) + " ";
            //string additionalArgs = Config.Get<string>("Init", "AdditionalArgs", "") + " ";
            additionalArgs += App.LaunchArgs;

            // setup ability to cancel the process
            CancellationTokenSource cancelToken = new CancellationTokenSource();

            try
            {
                Clipboard.SetDataObject(string.Empty);
            }
            catch
            {

            }

            if (selectedPack.Name.ContainsWhiteSpace())
            {
                FrostyMessageBox.Show("Launching game with a profile name which contains white space, like spacebars, will fail.\r\n", "Mods installation failed");

                return;
            }

            // launch
            int retCode = -6;
            FrostyTaskWindow.Show("Installing mods", "", (logger) =>
            {
                try
                {
                    foreach (var executionAction in App.PluginManager.ExecutionActions)
                    {
                        try
                        {
                            executionAction.PreLaunchAction(logger, PluginManagerType.ModManager, true, cancelToken.Token);
                        }
                        catch (Exception ex)
                        {
                            FileLogger.Info($"Exception on pre-launch action '{executionAction.GetType()}'.");

                            throw ex;
                        }
                    }

                    FrostyModExecutor modExecutor = new FrostyModExecutor();
                    retCode = modExecutor.Install(fs, cancelToken.Token, logger, modsDir.FullName, App.SelectedPack, modPaths.ToArray());

                    foreach (var executionAction in App.PluginManager.ExecutionActions)
                    {
                        executionAction.PostLaunchAction(logger, PluginManagerType.ModManager, true, cancelToken.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    retCode = -1;

                    foreach (var executionAction in App.PluginManager.ExecutionActions)
                    {
                        executionAction.PostLaunchAction(logger, PluginManagerType.ModManager, true, cancelToken.Token);
                    }

                    // process was cancelled
                    App.Logger.Log("Launch Cancelled");
                }

            }, showCancelButton: true, cancelCallback: (logger) => cancelToken.Cancel());

            if (retCode == 0)
            {
                var arguments = $"-dataPath \"ModData/{App.SelectedPack}\"";

                if (!string.IsNullOrWhiteSpace(additionalArgs))
                {
                    arguments += $" {additionalArgs}";
                }

                var clipBoardArgs = arguments;

                StringBuilder sb = new StringBuilder();

                if (OperatingSystemHelper.IsWine())
                {
                    var linuxArguments = $"WINEDLLOVERRIDES=\"winmm=n,b\" %command% {arguments}";

                    clipBoardArgs = linuxArguments;

                    sb.Append("To launch the game with mods use this Launch Options in Steam:\r\n\r\n");
                    sb.Append(linuxArguments);                    
                }
                else
                {
                    sb.Append("To launch the game with mods add these arguments in Steam or EA App to Launch Options:\r\n\r\n");
                    sb.Append(arguments);
                }

                var clipboardSuccess = true;

                for (int i = 0; i < 4; i++)
                {
                    clipboardSuccess = true;

                    try
                    {
                        Clipboard.SetDataObject(clipBoardArgs);
                        
                        clipboardSuccess = true;
                        i = 99;
                        break;
                    }
                    catch (Exception ex)
                    {
                        FileLogger.Info($"Exception on clipboard set. Details: {ex}");
                        clipboardSuccess = false;
                    }
                }

                if (clipboardSuccess)
                {
                    sb.Append("\r\n\r\nLaunch Options were copied to clipboard.");
                }
                else
                {
                    sb.Append("\r\n\r\nError happened while coping options to clipboard. Try to install mods again or write them manually.");
                }

                if (!SymLinkHelper.AreSymLinksSupported)
                {
                    sb.Append("\r\n\r\nWarning:\r\nFrosty could not initialize Symbolic Links, so Hard Links were used for mod installation. Please report this for me to improve Symbolic Links implementation.");
                }

                FrostyMessageBox.Show(sb.ToString(), "Mods installed successfully");
            }
            else if (retCode == -2)
            {
                FrostyMessageBox.Show("Both Hard Link and Symbolic Link methods are unavailable on your system. Please report this issue.\r\nYou will need to manually remove ModData folder from game directory. Your game files might be damaged.\r\n", "Mods installation failed");
            }
            else if (retCode == -3)
            {
                FrostyMessageBox.Show("Frosty Mod Manager is missing access to read and write from game directory.\r\n", "Missing access");
            }
            else if (retCode != -1)
            {
                FrostyMessageBox.Show("Mods installation failed due to unknown error.\r\n", "Mods installation failed");
            }

            GC.Collect();
        }

        private void launchButton_Click(object sender, RoutedEventArgs e)
        {
            Config.Save();
            //Config.Save(App.configFilename);

            // initialize
            Frosty.Core.App.FileSystem = new FileSystem(Config.Get<string>("GamePath", "", ConfigScope.Game));
            //FileSystem fs = new FileSystem(Config.Get<string>("Init", "GamePath", ""));
            foreach (FileSystemSource source in ProfilesLibrary.Sources)
                Frosty.Core.App.FileSystem.AddSource(source.Path, source.SubDirs);
            Frosty.Core.App.FileSystem.Initialize();

            // Set selected pack
            App.SelectedPack = selectedPack.Name;

            // get all applied mods
            List<string> modPaths = new List<string>();
            foreach (FrostyAppliedMod mod in selectedPack.AppliedMods)
            {
                if (mod.IsFound && mod.IsEnabled)
                    modPaths.Add(mod.Mod.Filename);
            }

            // combine stored args with launch args
            string additionalArgs = Config.Get<string>("CommandLineArgs", "", ConfigScope.Game) + " ";
            //string additionalArgs = Config.Get<string>("Init", "AdditionalArgs", "") + " ";
            additionalArgs += App.LaunchArgs;

            // setup ability to cancel the process
            CancellationTokenSource cancelToken = new CancellationTokenSource();

            // launch
            int retCode = 0;
            FrostyTaskWindow.Show("Launching", "", (logger) =>
            {
                try
                {
                    foreach (var executionAction in App.PluginManager.ExecutionActions)
                        executionAction.PreLaunchAction(logger, PluginManagerType.ModManager, false, cancelToken.Token);

                    FrostyModExecutor modExecutor = new FrostyModExecutor();
                    retCode = modExecutor.Run(fs, cancelToken.Token, logger, modsDir.FullName, App.SelectedPack, additionalArgs.Trim(), modPaths.ToArray());

                    foreach (var executionAction in App.PluginManager.ExecutionActions)
                        executionAction.PostLaunchAction(logger, PluginManagerType.ModManager, false, cancelToken.Token);
                }
                catch (OperationCanceledException)
                {
                    retCode = -1;

                    foreach (var executionAction in App.PluginManager.ExecutionActions)
                        executionAction.PostLaunchAction(logger, PluginManagerType.ModManager, false, cancelToken.Token);

                    // process was cancelled
                    App.Logger.Log("Launch Cancelled");
                }

            }, showCancelButton: true, cancelCallback: (logger) => cancelToken.Cancel());

            if (retCode != -1)
                WindowState = WindowState.Minimized;

            // kill the application if launched from the command line
            if (App.LaunchGameImmediately)
                Close();

            GC.Collect();
        }

        private void FrostyWindow_Closing(object sender, CancelEventArgs e)
            => Config.Save();
        //=> Config.Save(App.configFilename);

        private bool AddPack(string packName)
        {
            FrostyPack existingPack = packs.Find((FrostyPack a) =>
            {
                return a.Name.CompareTo(packName) == 0;
            });

            if (existingPack == null)
            {
                FrostyPack pack = new FrostyPack(packName);

                packs.Add(pack);
                packsComboBox.Items.Refresh();
                packsComboBox.SelectedItem = pack;

                Config.Add(pack.Name, "", ConfigScope.Pack);
                //Config.Add("Profiles", profile.Name, "");

                return true;
            }
            else
            {
                FrostyMessageBox.Show("A pack with the same name already exists", "Frosty Mod Manager");

                return false;
            }
        }

        private void enabledCheckBox_Checked(object sender, RoutedEventArgs e) => selectedPack.Refresh();

        private void installModButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "(All supported formats)|*.fbmod;*.rar;*.zip;*.7z;*.daimod" + "|*.fbmod (Frostbite Mod)|*.fbmod" + "|*.rar (Rar File)|*.rar" + "|*.zip (Zip File)|*.zip" + "|*.7z (7z File)|*.7z" + "|*.daimod (DragonAge Mod)|*.daimod",
                Title = "Install Mod",
                Multiselect = true
            };

            if (ofd.ShowDialog() == true)
            {
                InstallMods(ofd.FileNames);
            }

            ICollectionView view = CollectionViewSource.GetDefaultView(availableModsList.ItemsSource);
            view.Refresh();
        }

        private void uninstallModButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (IFrostyMod mod in availableModsList.SelectedItems)
            {
                FileInfo fi = new FileInfo(mod.Path);

                // remove from available list
                availableMods.Remove(mod);

                // remove from current pack
                int idx = selectedPack.AppliedMods.FindIndex((FrostyAppliedMod a) => a.Mod == mod);
                if (idx != -1)
                    selectedPack.AppliedMods.RemoveAt(idx);

                if (mod is FrostyMod && !((FrostyMod)mod).NewFormat)
                {
                    var fiNameWithoutExtension = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length).ToLower();

                    var modFiles = Directory.GetFiles(modsDir.FullName).Select(x => new FileInfo(x)).ToList();
                    var archives = modFiles.Where(x => x.Name.ToLower().StartsWith(fiNameWithoutExtension) && x.Extension.ToLower() == ".archive").ToList();

                    foreach (var archive in archives)
                    {
                        File.Delete(archive.FullName);
                    }
                }

                if (!fi.Exists)
                    continue;

                File.Delete(fi.FullName);

                if (mod is FrostyModCollection && Config.Get<bool>("DeleteCollectionMods", true))
                {
                    foreach (FrostyMod cmod in ((FrostyModCollection)mod).Mods)
                    {
                        fi = new FileInfo(cmod.Path);
                        File.Delete(fi.FullName);
                    }
                }
            }

            availableModsList.SelectedItem = null;
            ICollectionView view = CollectionViewSource.GetDefaultView(availableModsList.ItemsSource);
            view.Refresh();

            selectedPack.Refresh();
            appliedModsList.Items.Refresh();

            FrostyMessageBox.Show("Mod(s) has been successfully uninstalled", "Frosty Mod Manager");
        }

        private int VerifyMod(Stream stream)
        {
            using (DbReader reader = new DbReader(stream, null))
            {
                ulong magic = reader.ReadULong();
                if (magic != FrostyMod.Magic)
                {
                    reader.Position = 0;

                    DbObject modObj = reader.ReadDbObject();
                    if (modObj == null)
                        return -1;

                    if (modObj.GetValue<string>("gameProfile").ToLower() != ProfilesLibrary.ProfileName.ToLower())
                        return -2;

                    if (modObj.GetValue<int>("gameVersion") != fs.Head)
                        return 1;
                }
                else
                {
                    reader.Position = 0;
                    using (FrostyModReader modReader = new FrostyModReader(stream))
                    {
                        if (!modReader.IsValid)
                            return -1;

                        return modReader.GameVersion != fs.Head ? 0x8001 : 0x8000;
                    }
                }
            }

            return 0;
        }

        private FrostyMod AddMod(string modFilename, int format)
        {
            FrostyMod mod = null;
            if (format == 1)
            {
                mod = new FrostyMod(modFilename);
            }
            else
            {
                DbObject modObj = null;
                using (DbReader reader = new DbReader(new FileStream(modFilename, FileMode.Open, FileAccess.Read), null))
                    modObj = reader.ReadDbObject();

                mod = new FrostyMod(modFilename, modObj);
            }

            if (mod.GameVersion != fs.Head)
            {
                mod.AddWarning("Mod was designed for a different game version");
            }

            lock (availableMods)
            {
                availableMods.Add(mod);
            }

            return mod;
        }

        private FrostyModCollection AddCollection(string collectionFilename, int format)
        {
            FrostyModCollection collection = null;
            try
            {
                collection = new FrostyModCollection(collectionFilename);
            }
            catch (Exception e)
            {
                FrostyMessageBox.Show(e.Message);
                return null;
            }

            foreach (FrostyMod mod in collection.Mods)
            {
                lock (availableMods)
                {
                    int index = availableMods.FindIndex((IFrostyMod a) => a.Filename == mod.Filename);
                    if (index != -1)
                    {
                        availableMods.RemoveAt(index);
                    }
                }
            }

            lock (availableMods) availableMods.Add(collection);

            return collection;
        }

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
            => Close();

        private void availableModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((ListView)e.OriginalSource != availableModsList)
                availableModsList.SelectedIndex = -1;

            IFrostyMod mod = ((ListView)e.OriginalSource).SelectedItem as IFrostyMod;
            if (mod == null)
            {
                uninstallModButton.IsEnabled = false;
                addModButton.IsEnabled = false;

                if (tabControl.SelectedIndex == 1)
                    tabControl.SelectedIndex = 0;

                modDescTabItem.Visibility = Visibility.Collapsed;
                modDescTabItem.Content = null;

                return;
            }

            FrostyModDescription modDescPanel = new FrostyModDescription { Mod = mod };
            modDescPanel.ScreenshotClicked += ModDescPanel_ScreenshotClicked;

            modDescTabItem.Content = modDescPanel;
            modDescTabItem.Visibility = Visibility.Visible;

            uninstallModButton.IsEnabled = true;
            addModButton.IsEnabled = true;
        }

        private void ModDescPanel_ScreenshotClicked(object sender, ScreenshotButtonEventArgs e)
        {
            imagePanel.Visibility = Visibility.Visible;
            screenshotImage.Source = e.Screenshot;
        }

        private void largeScreenshot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            imagePanel.Visibility = Visibility.Collapsed;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            FileLogger.Info($"Default drag and drop effect: {e.Effects}");

            e.Effects = DragDropEffects.Copy;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                var dataObj = e.Data.GetData(DataFormats.FileDrop, true);

                if (dataObj == null)
                {
                    FileLogger.Info("Drop data is null.");
                    return;
                }

                var filenames = (string[])dataObj;

                if (filenames.Length <= 0)
                {
                    FileLogger.Info("Drop data is empty.");
                    return;
                }

                foreach (var filename in filenames)
                {
                    FileLogger.Info($"Drop file received '{filename}'.");
                }

                var report = string.Join("?", filenames);

                DropProgressReporter.Report(report);
            }
            else if (e.Data.GetFormats().Any(f => f == "FileContents"))
            {
                FileLogger.Info("Cannot import mod files that have not been extracted.");

                if (OperatingSystemHelper.IsWine())
                {
                    return;
                }

                SystemSounds.Hand.Play();

                FrostyMessageBox.Show("Cannot import mod files that have not been extracted", "Frosty Mod Manager");
            }
        }

        private void FrostyWindow_Drop(string joinedFileNames)
        {
            FileLogger.Info("Adding mods from drag and drop.");

            var fileNames = joinedFileNames.Split('?');

            InstallMods(fileNames);

            ICollectionView view = CollectionViewSource.GetDefaultView(availableModsList.ItemsSource);
            view.Refresh();

            FileLogger.Info("Drag and drop mods added.");
        }

        private void InstallMods(string[] filenames)
        {
            IFrostyMod lastInstalledMod = null;
            List<ImportErrorInfo> errors = new List<ImportErrorInfo>();

            PackManifest packManifest = null;

            FrostyTaskWindow.Show("Installing Mods", "", (logger) =>
            {
                List<string> collections = new List<string>();

                foreach (string filename in filenames)
                {
                    FileInfo fi = new FileInfo(filename);
                    logger.Log(fi.Name);

                    try
                    {
                        FileLogger.Info($"Loading mod '{filename}.'");

                        if (IsCompressed(fi.Name))
                        {
                            FileLogger.Info("Mod is compressed.");

                            List<string> mods = new List<string>();
                            List<int> format = new List<int>();
                            List<string> archives = new List<string>();
                            int fbpacks = 0;

                            // create decompressor
                            IDecompressor decompressor = null;
                            var extension = fi.Extension.ToLower();
                            if (extension == ".rar") decompressor = new RarDecompressor();
                            else if (extension == ".zip" || extension == ".fbpack") decompressor = new ZipDecompressor();
                            else if (extension == ".7z") decompressor = new SevenZipDecompressor();

                            try
                            {
                                // search out fbmods in archive
                                decompressor.OpenArchive(filename);
                                foreach (CompressedFileInfo compressedFi in decompressor.EnumerateFiles())
                                {

                                    if (compressedFi.Extension.ToLower() == ".fbpack")
                                    {
                                        //create temp file
                                        DirectoryInfo tempdir = new DirectoryInfo($"temp/");
                                        FileInfo tempfile = new FileInfo(tempdir + compressedFi.Filename);

                                        tempdir.Create();
                                        decompressor.DecompressToFile(tempfile.FullName);

                                        //install temp file
                                        Dispatcher.Invoke(() =>
                                        {
                                            InstallMods(new string[] { tempfile.FullName });
                                        });

                                        //delete temp files
                                        if (tempfile.Exists) tempfile.Delete();
                                        if (tempdir.Exists) tempdir.Delete();

                                        fbpacks++;
                                    }
                                    else if (compressedFi.Extension.ToLower() == ".fbcollection")
                                    {
                                        collections.Add(compressedFi.Filename);
                                    }
                                    else if (compressedFi.Extension.ToLower() == ".fbmod")
                                    {
                                        string modFilename = compressedFi.Filename;
                                        byte[] buffer = decompressor.DecompressToMemory();

                                        using (MemoryStream ms = new MemoryStream(buffer))
                                        {
                                            int retCode = VerifyMod(ms);
                                            if (retCode >= 0)
                                            {
                                                if ((retCode & 1) != 0)
                                                {
                                                    // continue with import (warning)
                                                    errors.Add(new ImportErrorInfo() { filename = modFilename, error = "Mod was designed for a different game version, it may or may not work.", isWarning = true });
                                                }

                                                // add mod
                                                mods.Add(compressedFi.Filename);
                                                format.Add((retCode & 0x8000) != 0 ? 1 : 0);
                                            }
                                            // ignore RetCode -1 here
                                            else if (retCode == -2)
                                            {
                                                errors.Add(new ImportErrorInfo() { filename = modFilename, error = "Mod was not designed for this game." });
                                            }
                                        }
                                    }
                                    else if (compressedFi.Extension.ToLower() == ".archive")
                                    {
                                        archives.Add(compressedFi.Filename);
                                    }
                                    else if (compressedFi.Filename.ToLower() == "manifest.json")
                                    {
                                        using (StreamReader reader = new StreamReader(compressedFi.Stream))
                                        {
                                            packManifest = JsonConvert.DeserializeObject<PackManifest>(reader.ReadToEnd());
                                        }
                                    }
                                }
                                decompressor.CloseArchive();
                            }
                            catch
                            {
                                FileLogger.Info($"Failed to read archive of '{fi.FullName}'.");
                                errors.Add(new ImportErrorInfo() { filename = fi.Name, error = "Failed to read Archive." });
                            }

                            if (mods.Count == 0 && fbpacks == 0)
                            {
                                // no point continuing with this archive
                                errors.Add(new ImportErrorInfo() { filename = fi.Name, error = "Archive contains no installable mods." });
                                continue;
                            }

                            // remove any invalid mods
                            for (int i = 0; i < mods.Count; i++)
                            {
                                string mod = mods[i];
                                if (format[i] == 0)
                                {
                                    // old legacy format requires an archive
                                    if (!archives.Contains(mod.Replace(".fbmod", "_01.archive")))
                                    {
                                        errors.Add(new ImportErrorInfo() { filename = mod, error = "Mod is missing the archive component." });
                                        mods.RemoveAt(i);
                                        i--;
                                        continue;
                                    }
                                }

                                // check for existing mod of same name
                                FrostyMod existingMod = availableMods.Find((IFrostyMod a) => { return a.Filename.ToLower().CompareTo(mod.ToLower()) == 0; }) as FrostyMod;
                                if (existingMod != null)
                                {
                                    availableMods.Remove(existingMod);
                                    foreach (FileInfo archiveFi in modsDir.GetFiles(mod.Replace(".fbmod", "") + "*.archive"))
                                    {
                                        File.Delete(archiveFi.FullName);
                                    }
                                }
                            }

                            // remove unreferenced .archives
                            for (int i = 0; i < archives.Count; i++)
                            {
                                string archive = archives[i];
                                if (!mods.Contains(archive.Replace("_01.archive", ".fbmod")))
                                {
                                    archives.RemoveAt(i);
                                    i--;
                                }
                            }

                            if (mods.Count > 0)
                            {
                                // now actually decompress files
                                decompressor.OpenArchive(filename);
                                foreach (CompressedFileInfo compressedFi in decompressor.EnumerateFiles())
                                {
                                    if (mods.Contains(compressedFi.Filename) || archives.Contains(compressedFi.Filename))
                                    {
                                        decompressor.DecompressToFile(Path.Combine(modsDir.FullName, compressedFi.Filename));
                                    }
                                }

                                // and add them to the mod manager
                                for (int i = 0; i < mods.Count; i++)
                                {
                                    fi = new FileInfo(Path.Combine(modsDir.FullName, mods[i]));
                                    lastInstalledMod = AddMod(fi.FullName, format[i]);
                                }
                            }
                        }
                        else if (fi.Extension == ".daimod")
                        {
                            FileLogger.Info("Mod is DAI mod.");

                            // special handling for DAI mod files
                            using (NativeReader reader = new NativeReader(new FileStream(fi.FullName, FileMode.Open)))
                            {
                                string magic = reader.ReadSizedString(8);
                                if (magic != "DAIMODV2")
                                {
                                    errors.Add(new ImportErrorInfo() { filename = fi.Name, error = "File is not a valid DAI Mod." });
                                    continue;
                                }

                                int unk = reader.ReadInt();
                                string name = reader.ReadNullTerminatedString();
                                string xml = reader.ReadNullTerminatedString();
                                string code = reader.ReadNullTerminatedString();

                                int resCount = reader.ReadInt();
                                List<byte[]> resources = new List<byte[]>();
                                List<bool> shouldWrite = new List<bool>();

                                for (int i = 0; i < resCount; i++)
                                {
                                    resources.Add(reader.ReadBytes(reader.ReadInt()));
                                    shouldWrite.Add(true);
                                }

                                string configValues = "";
                                if (code != "")
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        ConfigWindow win = new ConfigWindow(code, resources, shouldWrite);
                                        win.ShowDialog();

                                        configValues = win.GetConfigValues();
                                    });
                                }

                                System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                                xmlDoc.LoadXml(xml);

                                System.Xml.XmlElement elem = xmlDoc["daimod"]["details"];
                                string newDesc = "(Converted from .daimod)\r\n\r\n" + elem["description"].InnerText + "\r\n\r\n" + configValues;

                                DbObject modObject = new DbObject();
                                modObject.AddValue("magic", "FBMODV3");
                                modObject.AddValue("gameProfile", ProfilesLibrary.ProfileName);
                                modObject.AddValue("gameVersion", 0);

                                modObject.AddValue("title", elem["name"].InnerText);
                                modObject.AddValue("author", elem["author"].InnerText);
                                modObject.AddValue("category", "DAI Mods");
                                modObject.AddValue("version", elem["version"].InnerText);
                                modObject.AddValue("description", newDesc);

                                DbObject resourcesList = new DbObject(false);
                                DbObject actionsList = new DbObject(false);
                                DbObject screenshotList = new DbObject(false);
                                long offset = 0;

                                int index = -1;
                                int actualIndex = 0;

                                foreach (System.Xml.XmlElement subElem in xmlDoc["daimod"]["resources"])
                                {
                                    index++;
                                    if (subElem.GetAttribute("action") == "remove")
                                        continue;

                                    int resId = int.Parse(subElem.GetAttribute("resourceId"));
                                    if (shouldWrite[resId] == false)
                                        continue;

                                    int resSize = resources[resId].Length;
                                    string type = subElem.GetAttribute("type");

                                    DbObject resource = new DbObject();
                                    resource.AddValue("name", subElem.GetAttribute("name"));
                                    resource.AddValue("type", type);

                                    resource.AddValue("sha1", new Sha1(subElem.GetAttribute("sha1")));
                                    resource.AddValue("originalSize", 0);
                                    resource.AddValue("compressedSize", resSize);
                                    resource.AddValue("archiveIndex", 1);
                                    resource.AddValue("archiveOffset", offset);
                                    resource.AddValue("shouldInline", false);

                                    string actionString = subElem.GetAttribute("action");
                                    if (type == "ebx" || type == "res")
                                    {
                                        resource.AddValue("uncompressedSize", int.Parse(subElem.GetAttribute("originalSize")));
                                        if (actionString != "add")
                                            resource.AddValue("originalSha1", new Sha1(subElem.GetAttribute("originalSha1")));
                                        actionString = "modify";

                                        if (type == "res")
                                        {
                                            resource.AddValue("resType", uint.Parse(subElem.GetAttribute("resType")));
                                            if (resource.GetValue<uint>("resType") == 0x5C4954A6)
                                                resource.SetValue("shouldInline", true);
                                            resource.AddValue("resRid", (ulong)long.Parse(subElem.GetAttribute("resRid")));

                                            string resMetaString = subElem.GetAttribute("meta");
                                            byte[] resMeta = new byte[resMetaString.Length / 2];
                                            for (int i = 0; i < resMeta.Length; i++)
                                                resMeta[i] = byte.Parse(resMetaString.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);

                                            resource.AddValue("resMeta", resMeta);
                                        }
                                    }
                                    else
                                    {
                                        string chunkid = subElem.GetAttribute("name");
                                        byte[] chunkidBytes = new byte[chunkid.Length / 2];
                                        for (int i = 0; i < chunkidBytes.Length; i++)
                                            chunkidBytes[i] = byte.Parse(chunkid.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);

                                        resource.SetValue("name", (new Guid(chunkidBytes)).ToString());
                                        resource.AddValue("rangeStart", uint.Parse(subElem.GetAttribute("rangeStart")));
                                        resource.AddValue("rangeEnd", uint.Parse(subElem.GetAttribute("rangeEnd")));
                                        resource.AddValue("logicalOffset", uint.Parse(subElem.GetAttribute("logicalOffset")));
                                        resource.AddValue("logicalSize", uint.Parse(subElem.GetAttribute("logicalSize")));
                                        resource.AddValue("h32", int.Parse(subElem.GetAttribute("chunkH32")));

                                        string meta = subElem.GetAttribute("meta");
                                        if (meta != "00")
                                        {
                                            resource.SetValue("firstMip", int.Parse(meta.Substring(20, 8), NumberStyles.HexNumber));
                                        }

                                        // add special chunks bundle
                                        DbObject action = new DbObject();
                                        action.AddValue("resourceId", resourcesList.Count - 1);
                                        action.AddValue("type", "add");
                                        action.AddValue("bundle", "chunks");
                                        actionsList.Add(action);
                                    }

                                    foreach (System.Xml.XmlElement bundleElem in xmlDoc["daimod"]["bundles"])
                                    {
                                        foreach (System.Xml.XmlElement entryElem in bundleElem["entries"])
                                        {
                                            int id = int.Parse(entryElem.GetAttribute("id"));
                                            if (id == index)
                                            {
                                                DbObject action = new DbObject();
                                                action.AddValue("resourceId", actualIndex);
                                                action.AddValue("type", actionString);
                                                action.AddValue("bundle", bundleElem.GetAttribute("name"));
                                                actionsList.Add(action);
                                            }
                                        }
                                    }

                                    resourcesList.Add(resource);
                                    offset += resSize;
                                    actualIndex++;
                                }

                                modObject.AddValue("screenshots", screenshotList);
                                modObject.AddValue("resources", resourcesList);
                                modObject.AddValue("actions", actionsList);

                                using (DbWriter writer = new DbWriter(new FileStream(Path.Combine(modsDir.FullName, fi.Name.Replace(".daimod", ".fbmod")), FileMode.Create)))
                                {
                                    writer.Write(modObject);
                                }
                                using (NativeWriter writer = new NativeWriter(new FileStream(Path.Combine(modsDir.FullName, fi.Name.Replace(".daimod", "_01.archive")), FileMode.Create)))
                                {
                                    for (int i = 0; i < resources.Count; i++)
                                    {
                                        if (shouldWrite[i])
                                            writer.Write(resources[i]);
                                    }
                                }

                                fi = new FileInfo(Path.Combine(modsDir.FullName, fi.Name.Replace(".daimod", ".fbmod")));
                                lastInstalledMod = AddMod(fi.FullName, 0);
                            }
                        }
                        else if (fi.Extension == ".fbcollection")
                        {
                            FileLogger.Info("Mod is fbcolletion.");

                            collections.Add(fi.Name);
                        }
                        else
                        {
                            // dont allow any files without fbmod extension
                            if (fi.Extension.ToLower() != ".fbmod")
                            {
                                if (fi.Extension.ToLower() == ".archive")
                                    continue;

                                FileLogger.Info("Mod is not fbmod.");


                                errors.Add(new ImportErrorInfo() { filename = fi.Name, error = "File is not a valid Frosty Mod." });
                                continue;
                            }

                            FileLogger.Info("Mod is fbmod.");

                            // make sure mod is designed for current profile
                            bool newFormat = false;
                            using (FileStream stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read))
                            {
                                FileLogger.Info("Verifying fbmod.");

                                int retCode = VerifyMod(stream);
                                if ((retCode & 1) != 0)
                                {
                                    // continue with import (warning)
                                    FileLogger.Info("Mod was designed for a different game version, it may or may not work.");
                                    errors.Add(new ImportErrorInfo { filename = fi.Name, error = "Mod was designed for a different game version, it may or may not work.", isWarning = true });
                                }
                                else if (retCode == -1)
                                {
                                    FileLogger.Info("File is not a valid Frosty Mod.");
                                    errors.Add(new ImportErrorInfo { filename = fi.Name, error = "File is not a valid Frosty Mod." });
                                }
                                else if (retCode == -2)
                                {
                                    FileLogger.Info("Mod was not designed for this game.");
                                    errors.Add(new ImportErrorInfo { filename = fi.Name, error = "Mod was not designed for this game." });
                                    continue;
                                }
                                else if (retCode == -3)
                                {
                                    FileLogger.Info("Mod was found to be invalid and cannot be used.");
                                    errors.Add(new ImportErrorInfo { filename = fi.Name, error = "Mod was found to be invalid and cannot be used." });
                                    continue;
                                }

                                if ((retCode & 0x8000) != 0)
                                    newFormat = true;
                            }

                            if (!newFormat)
                            {
                                FileLogger.Info("Validate old format archive file.");

                                var archivePath = ArchiveHelper.GetArchivePath(fi.FullName, out var errorMessage);

                                if (string.IsNullOrWhiteSpace(archivePath))
                                {
                                    FileLogger.Info($"Could not get archive path for mod '{fi.FullName}'. Details: {errorMessage}");
                                    errors.Add(new ImportErrorInfo { filename = fi.Name, error = $"Could not get archive path. Details: {errorMessage}" });
                                    continue;
                                }

                                // make sure mod has archive file
                                if (!File.Exists(archivePath))
                                {
                                    FileLogger.Info($"Missing archive file at '{archivePath}'.");
                                    errors.Add(new ImportErrorInfo { filename = fi.Name, error = $"Mod is missing the archive component at '{archivePath}'." });
                                    continue;
                                }
                            }

                            // check for existing mod of same name
                            FileLogger.Info("Check if mod with same name already exists.");
                            FrostyMod existingMod = availableMods.Find((IFrostyMod a) => a.Filename.ToLower().CompareTo(fi.Name.ToLower()) == 0) as FrostyMod;
                            if (existingMod != null)
                            {
                                availableMods.Remove(existingMod);
                                foreach (FileInfo archiveFi in modsDir.GetFiles(fi.Name.ToLower().Replace(".fbmod", string.Empty) + "_*.archive"))
                                {
                                    File.Delete(archiveFi.FullName);
                                }
                                File.Delete(modsDir.FullName + "/" + fi.Name);
                            }

                            // copy mod over
                            FileLogger.Info($"Copy mod to mods dir '{modsDir.FullName}'.");
                            File.Copy(fi.FullName, Path.Combine(modsDir.FullName, fi.Name));
                            foreach (FileInfo archiveFi in fi.Directory.GetFiles(fi.Name.ToLower().Replace(".fbmod", string.Empty) + "_*.archive"))
                            {
                                File.Copy(archiveFi.FullName, Path.Combine(modsDir.FullName, archiveFi.Name));
                            }

                            // add mod to manager
                            FileLogger.Info("Add mod to manager.");
                            fi = new FileInfo(Path.Combine(modsDir.FullName, fi.Name));
                            lastInstalledMod = AddMod(fi.FullName, newFormat ? 1 : 0);
                        }

                        if (collections.Count > 0)
                        {
                            FileLogger.Info("Finishing collections.");

                            if (filename.ToLower().Contains(".zip"))
                            {
                                FileLogger.Info($"Decompressing zip collection '{filename}'.");

                                // now actually decompress files
                                ZipDecompressor decompressor = new ZipDecompressor();
                                decompressor.OpenArchive(filename);

                                var compressedFiles = decompressor.EnumerateFiles().ToList();

                                FileLogger.Info($"Zip has {compressedFiles.Count} files.");

                                foreach (CompressedFileInfo compressedFi in compressedFiles)
                                {
                                    FileLogger.Info($"Decompresssing file '{compressedFi.Filename}' from zip.");

                                    if (collections.Contains(compressedFi.Filename))
                                    {
                                        decompressor.DecompressToFile(Path.Combine(modsDir.FullName, compressedFi.Filename));
                                    }
                                }
                            }
                            else if (filename.ToLower().Contains(".fbcollection"))
                            {
                                FileLogger.Info($"Collection '{filename}' is a fbcollection.");
                                File.Copy(fi.FullName, Path.Combine(modsDir.FullName, fi.Name));
                            }
                        }
                    }
                    catch (FrostyModLoadException e)
                    {
                        FileLogger.Info($"Exception while loading mod '{fi.Name}'. Details: {e}");
                        errors.Add(new ImportErrorInfo { error = e.Message, filename = fi.Name });
                        File.Delete(fi.FullName);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.Info($"Exception while installing mod '{fi.Name}'. Details: {ex}");
                        errors.Add(new ImportErrorInfo { error = ex.Message, filename = fi.Name });
                    }
                }

                // add collections to the mod manager
                for (int i = 0; i < collections.Count; i++)
                {
                    FileInfo fi = new FileInfo(Path.Combine(modsDir.FullName, collections[i]));
                    lastInstalledMod = AddCollection(fi.FullName, 0);
                }
            });

            ICollectionView view = CollectionViewSource.GetDefaultView(availableModsList.ItemsSource);

            view.Refresh();
            view.GroupDescriptions.Clear();

            PropertyGroupDescription groupDescription = new PropertyGroupDescription("ModDetails.Category");
            view.GroupDescriptions.Add(groupDescription);

            if (lastInstalledMod != null)
            {
                // set description to last installed mod
                tabControl.SelectedIndex = 1;
                availableModsList.SelectedItem = lastInstalledMod;
            }

            if (errors.Count > 0)
            {
                // show error window
                InstallErrorsWindow win = new InstallErrorsWindow(errors);
                win.ShowDialog();
            }

            if (packManifest != null)
            {
                if (AddPack(packManifest.name))
                {
                    foreach (string modName in packManifest.mods)
                    {
                        FrostyMod mod = availableMods.Find((IFrostyMod a) =>
                        {
                            return a.Filename.CompareTo(modName) == 0;
                        }) as FrostyMod;

                        if (mod != null)
                            selectedPack.AddMod(mod);
                    }

                    appliedModsList.Items.Refresh();

                    // focus on tab item
                    appliedModsTabItem.IsSelected = true;

                    FrostyMessageBox.Show("Pack has been successfully imported", "Frosty Mod Manager");
                }
            }
        }

        private bool IsCompressed(string path)
        {
            var extension = Path.GetExtension(path).ToLower();

            if (extension == ".rar")
            {
                FileLogger.Info("File is rar.");
                return true;
            }

            if (extension == ".zip")
            {
                FileLogger.Info("File is zip.");
                return true;
            }

            if (extension == ".7z")
            {
                FileLogger.Info("File is zip.");
                return true;
            }

            if (extension == ".fbpack")
            {
                FileLogger.Info("File is fbpack.");
                return true;
            }

            return false;
        }

        private void launchOptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LaunchOptionsWindow win = new LaunchOptionsWindow();
            win.ShowDialog();
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow win = new AboutWindow();
            win.ShowDialog();
        }

        private void modDataMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ManageModDataWindow win = new ManageModDataWindow();
            win.ShowDialog();
        }

        private void appliedModsList_SelectionChanged(object sender, SelectionChangedEventArgs e) => updateAppliedModButtons();

        private void updateAppliedModButtons()
        {
            if (appliedModsList.SelectedItem != null)
            {
                removeButton.IsEnabled = true;

                if (orderComboBox.SelectedIndex == 0)
                {
                    upButton.IsEnabled = appliedModsList.SelectedIndex != 0;
                    downButton.IsEnabled = appliedModsList.SelectedIndex != (appliedModsList.Items.Count - 1);
                }
                else if (orderComboBox.SelectedIndex == 1)
                {
                    upButton.IsEnabled = appliedModsList.SelectedIndex != (appliedModsList.Items.Count - 1);
                    downButton.IsEnabled = appliedModsList.SelectedIndex != 0;
                }
            }
            else
            {
                removeButton.IsEnabled = false;
                upButton.IsEnabled = false;
                downButton.IsEnabled = false;
            }
        }

        private void availableModsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (availableModsList.SelectedIndex == -1)
                return;

            IFrostyMod selectedMod = availableModsList.SelectedItem as IFrostyMod;
            selectedPack.AddMod(selectedMod);
            appliedModsList.Items.Refresh();

            // focus on tab item
            appliedModsTabItem.IsSelected = true;
        }

        public List<Control> AllChildren(DependencyObject parent)
        {
            List<Control> children = new List<Control>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is Control)
                    children.Add(child as Control);
                children.AddRange(AllChildren(child));
            }
            return children;
        }

        private void addModButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (IFrostyMod mod in availableModsList.SelectedItems)
                selectedPack.AddMod(mod);

            foreach (IFrostyMod item in availableModsList.Items)
            {
                DependencyObject container = availableModsList.ItemContainerGenerator.ContainerFromItem(item);
                ListView collectionModsList = AllChildren(container).OfType<ListView>().ToList().FirstOrDefault(x => x.Name.Equals("collectionModsList"));

                if (collectionModsList != null)
                    foreach (IFrostyMod mod in collectionModsList.SelectedItems)
                        selectedPack.AddMod(mod);
            }

            appliedModsList.Items.Refresh();

            // focus on tab item
            appliedModsTabItem.IsSelected = true;
        }

        private void SelectedProfile_AppliedModsUpdated(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedItem == conflictsTabItem)
            {
                UpdateConflictsProgressReporter.Report(false);
            }

            conflictsTabItem.Visibility = Visibility.Visible;
        }

        private void UpdateConflicts(bool reset)
        {
            if (reset)
            {
                ConflictInfos.Clear();
                SetConflictPage(0);

                return;
            }

            if (tabControl.SelectedItem != conflictsTabItem)
            {
                return;
            }

            FileLogger.Info("Start conflicts update.");

            bool onlyShowReplacements = (bool)showOnlyReplacementsCheckBox.IsChecked;

            StringBuilder sb = new StringBuilder();
            List<ModResourceInfo> totalResourceList = new List<ModResourceInfo>();

            //SetNativeEnabled(this, false);
            IsEnabled = false;

            var modal = FrostyTaskWindow.ShowSimple("Updating Actions", "");

            // Iterate through mod resources
            for (int i = 0; i < selectedPack.AppliedMods.Count; i++)
            {
                FrostyAppliedMod appliedMod = selectedPack.AppliedMods[i];
                if (!appliedMod.IsFound || !appliedMod.IsEnabled)
                    continue;


                FrostyMod[] mods;
                if (appliedMod.Mod is FrostyModCollection)
                {
                    mods = (appliedMod.Mod as FrostyModCollection).Mods.ToArray();
                }
                else
                {
                    mods = new FrostyMod[1];
                    mods[0] = appliedMod.Mod as FrostyMod;
                }

                foreach (var mod in mods)
                {
                    if (mod.NewFormat)
                    {
                        foreach (BaseModResource resource in mod.Resources)
                        {
                            if (resource.Type == ModResourceType.Embedded)
                                continue;

                            string resType = resource.Type.ToString().ToLower();
                            string resourceName = resource.Name;

                            if (resource.UserData != "")
                            {
                                string[] arr = resource.UserData.Split(';');
                                resType = arr[0].ToLower();
                                resourceName = arr[1];
                            }

                            int index = totalResourceList.FindIndex((ModResourceInfo a) => a.Equals(resType + "/" + resourceName));

                            if (index == -1)
                            {
                                ModResourceInfo resInfo = new ModResourceInfo(resourceName, resType);
                                totalResourceList.Add(resInfo);
                                index = totalResourceList.Count - 1;
                            }

                            ModPrimaryActionType primaryAction = ModPrimaryActionType.None;
                            if (resource.HasHandler)
                            {
                                if ((uint)resource.Handler == 0xBD9BFB65)
                                    primaryAction = ModPrimaryActionType.Merge;
                                else
                                {
                                    ICustomActionHandler handler = null;
                                    if (resource.Type == ModResourceType.Ebx)
                                        handler = App.PluginManager.GetCustomHandler((uint)resource.Handler);
                                    else if (resource.Type == ModResourceType.Res)
                                        handler = App.PluginManager.GetCustomHandler((ResourceType)(resource as ResResource).ResType);

                                    if (handler.Usage == HandlerUsage.Merge)
                                    {
                                        foreach (string actionString in handler.GetResourceActions(resource.Name, mod.GetResourceData(resource)))
                                        {
                                            string[] arr = actionString.Split(';');
                                            AddResourceAction(totalResourceList, mod, arr[0], arr[1], (ModPrimaryActionType)Enum.Parse(typeof(ModPrimaryActionType), arr[2]));
                                        }
                                        primaryAction = ModPrimaryActionType.Merge;
                                    }
                                    else primaryAction = ModPrimaryActionType.Modify;
                                }
                            }
                            else if (resource.IsAdded) primaryAction = ModPrimaryActionType.Add;
                            else if (resource.IsModified) primaryAction = ModPrimaryActionType.Modify;

                            totalResourceList[index].AddMod(mod, primaryAction, resource.AddedBundles);
                        }
                    }
                }
            }

            if (onlyShowReplacements)
            {
                totalResourceList.RemoveAll(item => item.ModCount <= 1);
            }

            totalResourceList.Sort((ModResourceInfo a, ModResourceInfo b) =>
            {
                int result = a.Type[1].CompareTo(b.Type[1]);
                return result == 0 ? a.Name.CompareTo(b.Name) : result;
            });

            modal.Close();

            //SetNativeEnabled(this, true);
            IsEnabled = true;

            tabControl.SelectedItem = conflictsTabItem;

            ConflictInfos.Clear();
            ConflictInfos.AddRange(totalResourceList);
            SetConflictPage(0);
            conflictsListView.SelectedIndex = 0;

            FileLogger.Info("Finished conflicts update.");
        }

        private void AddResourceAction(List<ModResourceInfo> totalResourceList, FrostyMod mod, string resourceName, string resourceType, ModPrimaryActionType type)
        {
            int index = totalResourceList.FindIndex((ModResourceInfo a) => a.Equals(resourceType + "/" + resourceName));
            if (index == -1)
            {
                ModResourceInfo resInfo = new ModResourceInfo(resourceName, resourceType);
                totalResourceList.Add(resInfo);
                index = totalResourceList.Count - 1;
            }

            totalResourceList[index].AddMod(mod, type, null);
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateConflictsProgressReporter.Report(!conflictsTabItem.IsSelected);
        }

        private void launchConfigurationWindow_Click(object sender, RoutedEventArgs e)
        {
            Config.Save();
            //Config.Save(App.configFilename);

            Windows.PrelaunchWindow2 SelectConfiguration = new Windows.PrelaunchWindow2();
            App.Current.MainWindow = SelectConfiguration;
            SelectConfiguration.Show();
            Close();
        }

        private void logTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tb.IsFocused)
                tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
            tb.ScrollToEnd();
        }

        private void availableModsFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                availableModsFilter_LostFocus(this, new RoutedEventArgs());
        }

        private void availableModsFilter_LostFocus(object sender, RoutedEventArgs e)
        {
            if (availableModsFilterTextBox.Text == "")
            {
                availableModsList.Items.Filter = null;
                return;
            }

            availableModsList.Items.Filter = new Predicate<object>((object a) => ((IFrostyMod)a).ModDetails.Title.ToLower().Contains(availableModsFilterTextBox.Text.ToLower()));
        }

        private void optionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow win = new OptionsWindow();
            win.ShowDialog();
        }

        private void ZipPack(string filename)
        {
            FrostyTaskWindow.Show("Exporting Pack", "", (logger) =>
            {
                try
                {
                    List<string> mods = new List<string>();
                    foreach (FrostyAppliedMod mod in selectedPack.AppliedMods)
                    {
                        if (mod.IsFound && mod.IsEnabled)
                            mods.Add(mod.Mod.Filename);
                    }

                    PackManifest manifest = new PackManifest()
                    {
                        managerVersion = App.Version,
                        version = manifestVersion,
                        name = selectedPack.Name,
                        mods = mods
                    };

                    using (ZipArchive archive = ZipFile.Open(filename, ZipArchiveMode.Create))
                    {
                        foreach (FrostyAppliedMod mod in selectedPack.AppliedMods)
                        {
                            if (mod.Mod is FrostyModCollection)
                                continue;
                            archive.CreateEntryFromFile((mod.Mod as FrostyMod).Path, mod.Mod.Filename);
                        }

                        ZipArchiveEntry manifestEntry = archive.CreateEntry("manifest.json");
                        using (Stream stream = manifestEntry.Open())
                        {
                            byte[] buffer = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(manifest, Formatting.Indented));

                            stream.Write(buffer, 0, buffer.Length);
                        }

                        archive.Dispose();
                    }
                }
                catch
                {
                    File.Delete(filename);
                }
            });
        }

        private void packImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "*.fbpack;*.zip (Fb Pack) |*.fbpack;*.zip",
                Title = "Import Pack",
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
            {
                InstallMods(ofd.FileNames);
            }
        }

        private void packExport_Click(object sender, RoutedEventArgs e)
        {
            FrostySaveFileDialog sfd = new FrostySaveFileDialog("Save Pack As", "*.fbpack (FBPack)|*.fbpack", "FBPack");
            if (sfd.ShowDialog())
            {
                if (File.Exists(sfd.FileName))
                    FrostyMessageBox.Show("A file with the same name already exists", "Frosty Mod Manager");
                else
                    ZipPack(sfd.FileName);
            }
        }

        private void importPackButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "*.fbpack;*.zip (Fb Pack) |*.fbpack;*.zip",
                Title = "Import Pack",
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
            {
                InstallMods(ofd.FileNames);
            }
        }

        private void collectionExport_Click(object sender, RoutedEventArgs e)
        {
            var ew = new Windows.CollectionSettingsWindow(selectedPack.AppliedMods);
            ew.ShowDialog();
        }

        private void collectionModsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListView collectionModsList = (ListView)sender;
            if (collectionModsList.SelectedIndex == -1)
                return;

            availableModsList.SelectedIndex = -1;

            IFrostyMod selectedMod = collectionModsList.SelectedItem as IFrostyMod;
            selectedPack.AddMod(selectedMod);
            appliedModsList.Items.Refresh();

            // focus on tab item
            appliedModsTabItem.IsSelected = true;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ListBox && !e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        private void collectionModsList_LostFocus(object sender, RoutedEventArgs e)
        {
            ((ListView)sender).UnselectAll();
        }

        private void orderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Setter setter = new Setter(DockPanel.DockProperty, Dock.Top);
            switch (orderComboBox.SelectedIndex)
            {
                case 0:
                    setter = new Setter(DockPanel.DockProperty, Dock.Top);
                    Config.Add("ApplyModOrder", "List");
                    break;
                case 1:
                    setter = new Setter(DockPanel.DockProperty, Dock.Bottom);
                    Config.Add("ApplyModOrder", "Priority");
                    break;
            }
            Style style = new Style(typeof(ListBoxItem), FindResource(typeof(ListBoxItem)) as Style);
            style.Setters.Add(setter);
            appliedModsList.ItemContainerStyle = style;

            updateAppliedModButtons();
        }

        private void CopyFullExceptionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Plugin selectedPlugin = (Plugin)LoadedPluginsList.SelectedItem;

            // retrieve the selected plugin's load exception, execute ToString on it, and add the result to the clipboard
            Clipboard.SetText(string.Format("[{0}]\n{1}", new string[]
            {
                DateTime.Now.ToString(),
                selectedPlugin.LoadException.ToString()
            }));
        }

        private void LoadMenuExtensions()
        {
            // Add menu extensions to Mod Manager
            foreach (var menuExtension in App.PluginManager.MenuExtensions)
            {
                MenuItem foundMenuItem = null;
                foreach (MenuItem menuItem in menu.Items)
                {
                    // contains top level menu item already
                    if (menuExtension.TopLevelMenuName.Equals(menuItem.Header as string, StringComparison.OrdinalIgnoreCase))
                    {
                        foundMenuItem = menuItem;
                        break;
                    }
                }

                if (foundMenuItem == null)
                {
                    foundMenuItem = new MenuItem() { Header = menuExtension.TopLevelMenuName };

                    // insert the top-level Menu behind the Help Menu
                    menu.Items.Insert(menu.Items.Count - 1, foundMenuItem);
                }

                if (!string.IsNullOrEmpty(menuExtension.SubLevelMenuName))
                {
                    MenuItem parentMenuItem = null;
                    foreach (var menuItem in foundMenuItem.Items)
                    {
                        if (menuItem is MenuItem item)
                        {
                            if (menuExtension.SubLevelMenuName.Equals(item.Header as string, StringComparison.OrdinalIgnoreCase))
                            {
                                parentMenuItem = foundMenuItem;
                                foundMenuItem = item;
                                break;
                            }
                        }
                    }

                    if (parentMenuItem == null)
                    {
                        parentMenuItem = foundMenuItem;
                        foundMenuItem = new MenuItem { Header = menuExtension.SubLevelMenuName };
                        parentMenuItem.Items.Add(foundMenuItem);
                    }
                }

                MenuItem menuExtItem = new MenuItem
                {
                    Header = menuExtension.MenuItemName,
                    Icon = new Image() { Source = menuExtension.Icon },
                    Command = menuExtension.MenuItemClicked
                };
                foundMenuItem.Items.Add(menuExtItem);
            }
        }

        private void ConflictsHandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var obj = conflictsListView.SelectedItem;

            if (obj == null)
            {
                return;
            }

            var modInfo = obj as ModResourceInfo;

            var sb = new StringBuilder();

            sb.Append($"List of mods altering resource\r\n'{modInfo.Name}'\r\n");

            foreach (var mod in modInfo.Mods)
            {
                sb.Append($"\r\n • {mod.NiceName}"); 
            }

            FrostyMessageBox.Show(sb.ToString(), "Resource conflicts");
        }

        private void showOnlyReplacementsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateConflictsProgressReporter.Report(false);
        }

        private void SetConflictPage(int page)
        {
            if (page < 0)
            {
                page = 0;
            }

            int maxPage = (ConflictInfos.Count / ConflictPageSize) - ((ConflictInfos.Count % ConflictPageSize == 0 && ConflictInfos.Count > 0) ? 1 : 0);

            if (page > maxPage)
            {
                page = maxPage;
            }

            ConflictPage = page;
            var items = ConflictInfos.Skip(ConflictPageSize * page).Take(ConflictPageSize).ToList();

            // TODO: Update UI
            conflictsPrev.IsEnabled = page > 0;
            conflictsNext.IsEnabled = page < maxPage;
            conflictsPageText.Text = $"  Page {page + 1}  ";

            conflictsListView.ItemsSource = items;
        }

        private void conflictsPrev_Click(object sender, RoutedEventArgs e)
        {
            ConflictPage--;
            SetConflictPage(ConflictPage);
        }

        private void conflictsNext_Click(object sender, RoutedEventArgs e)
        {
            ConflictPage++;
            SetConflictPage(ConflictPage);
        }
    }
}
