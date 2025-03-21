﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using FrostySdk;
using Microsoft.Win32;
using Frosty.Controls;
using Frosty.Core;
using FrostySdk.IO;
using FrostySdk.Managers;
using System.Linq;

namespace FrostyModManager.Windows
{
    /// <summary>
    /// Interaction logic for PrelaunchWindow2.xaml
    /// </summary>
    public partial class PrelaunchWindow2 : FrostyDockableWindow
    {
        private List<FrostyConfiguration> configs = new List<FrostyConfiguration>();
        private FrostyConfiguration defaultConfig = null;

        Config ini = new Config();

        public PrelaunchWindow2()
        {
            InitializeComponent();
        }

        private void LaunchConfig(string profile /*Config config, string filename*/)
        {
            //App.configFilename = filename;
            //Config.Load(config); // Load game config

            // load profiles
            if (!ProfilesLibrary.Initialize(profile))
            {
                FrostyMessageBox.Show("There was an error when trying to load game using specified profile.", "Frosty Mod Manager");
                Close();
                return;
            }

            //if (!ProfilesLibrary.Initialize(Config.Get<string>("Init", "Profile", "")))
            //{
            //    FrostyMessageBox.Show("There was an error when trying to load game using specified profile.", "Frosty Editor");
            //    Close();
            //    return;
            //}

            if (ProfilesLibrary.RequiresKey && ProfilesLibrary.DataVersion == (int)ProfileVersion.Fifa19)
            {
                byte[] keyData = null;
                if (!File.Exists(ProfilesLibrary.CacheName + ".key"))
                {
                    // prompt for encryption key
                    KeyPromptWindow keyPromptWin = new KeyPromptWindow();
                    if (keyPromptWin.ShowDialog() == false)
                    {
                        FrostyMessageBox.Show("Encryption key not entered. Unable to load profile.", "Frosty Editor");
                        return;
                    }

                    keyData = keyPromptWin.EncryptionKey;
                    using (NativeWriter writer = new NativeWriter(new FileStream(ProfilesLibrary.CacheName + ".key", FileMode.Create)))
                        writer.Write(keyData);
                }
                else
                {
                    // otherwise just read the key from file
                    keyData = NativeReader.ReadInStream(new FileStream(ProfilesLibrary.CacheName + ".key", FileMode.Open, FileAccess.Read));
                }

                // add primary encryption key
                byte[] key = new byte[0x10];
                Array.Copy(keyData, key, 0x10);
                KeyManager.Instance.AddKey("Key1", key);

                if (keyData.Length > 0x10)
                {
                    // add additional encryption keys
                    key = new byte[0x10];
                    Array.Copy(keyData, 0x10, key, 0, 0x10);
                    KeyManager.Instance.AddKey("Key2", key);

                    key = new byte[0x4000];
                    Array.Copy(keyData, 0x20, key, 0, 0x4000);
                    KeyManager.Instance.AddKey("Key3", key);
                }
            }

            // launch Mod Manager
            SplashWindow splashWin = new SplashWindow();
            App.Current.MainWindow = splashWin;
            splashWin.Show();
        }

        private static bool ValidateWorkingDirContent()
        {
            var dir = Directory.GetCurrentDirectory();

            var files = Directory.GetFiles(dir).Select(x => new FileInfo(x)).Select(x => x.Name.ToLower()).ToList();

            if (files.All(x => x != "frostymodmanager.exe"))
            {
                return false;
            }

            if (files.All(x => x != "frostysdk.dll"))
            {
                return false;
            }

            if (files.All(x => x != "frostycore.dll"))
            {
                return false;
            }

            return true;
        }

        private static bool ValidateWorkingDirAccess()
        {
            var dir = Directory.GetCurrentDirectory();

            try
            {
                var filePath = Path.Combine(dir, "test.test");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var sw = File.CreateText(filePath);
                sw.WriteLine("test");

                sw.Close();

                var res = File.ReadAllText(filePath);

                if (!res.Contains("test"))
                {
                    return false;
                }

                File.Delete(filePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool ValidateDesktopDirAccess()
        {
            if (!OperatingSystemHelper.IsWine())
            {
                return true;
            }

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            if (string.IsNullOrWhiteSpace(dir))
            {
                return false;
            }

            if (!Directory.Exists(dir))
            {
                return false;
            }

            try
            {
                var filePath = Path.Combine(dir, "frosty.test");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var sw = File.CreateText(filePath);
                sw.WriteLine("test");

                sw.Close();

                var res = File.ReadAllText(filePath);

                if (!res.Contains("test"))
                {
                    return false;
                }

                File.Delete(filePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ValidateWorkingDirContent())
            {
                var message = "Working directory does not match Frosty Mod Manager installation location.\r\n";

                if (OperatingSystemHelper.IsWine())
                {
                    message += "\r\nOn Linux make sure Frosty Mod Manager is run from a directory accessible via a Wine drive.";
                    message += "\r\nIf Wine is run from a Flatpak application, make sure that application has access to the Frosty Mod Manager location (can be set up with Flatseal).";
                }

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
                Close();
                return;
            }

            if (!ValidateWorkingDirAccess())
            {
                var message = "Working directory does not have read and write access.\r\n";

                if (OperatingSystemHelper.IsWine())
                {
                    message += "\r\nOn Linux make sure Frosty Mod Manager is run from a directory accessible via a Wine drive that is not Z.";
                    message += "\r\nIf Wine is run from a Flatpak application, make sure that application has read and write access to the Frosty Mod Manager location (can be set up with Flatseal).";
                }

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
                Close();
                return;
            }

            if (!ValidateDesktopDirAccess())
            {
                var message = "Desktop direcotry cannot be accessed.\r\n";

                message += "\r\nOn Linux make sure application running Wine (like Bottles, Lutris) has access to /home/{user}/Desktop directory.";
                message += "\r\nYou can use Flatseal to add this access to Flatpak applications.";
                message += "\r\nIt is recommended to select 'All user files' for maximum compatibility.";

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
                Close();
                return;
            }

            RefreshConfigurationList();

            RemoveConfigButton.IsEnabled = false;
            LaunchConfigButton.IsEnabled = false;

            //ini.LoadEntries("DefaultSettings.ini");

            string defaultConfigName = Config.Get<string>("DefaultProfile", null);
            //string defaultConfigName = ini.GetEntry("Init", "DefaultConfiguration", "");

            foreach (FrostyConfiguration name in configs)
            {
                if (name.ProfileName == defaultConfigName)
                {
                    defaultConfig = name;
                }
            }

            ConfigList.SelectedItem = defaultConfig;
        }

        private void RefreshConfigurationList()
        {
            configs.Clear();

            foreach (string profile in Config.GameProfiles)
            {
                try
                {
                    configs.Add(new FrostyConfiguration(profile));
                }
                catch (System.IO.FileNotFoundException)
                {
                    Config.RemoveGame(profile); // couldn't find the exe, so remove it from the profile list
                    Config.Save();
                }
            }
            //foreach (string s in Directory.EnumerateFiles("./", "FrostyModManager*.ini"))
            //{
            //    try
            //    {
            //        FrostyConfiguration config = new FrostyConfiguration(s);
            //        configs.Add(config);
            //    }
            //    catch (Exception /*ex*/)
            //    {
            //        //FrostyMessageBox.Show("Couldn't load profile from '" + s + "': \n\n" + ex.ToString());
            //    }
            //}

            ConfigList.ItemsSource = configs;
        }

        private void ConfigList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveConfigButton.IsEnabled = true;
            LaunchConfigButton.IsEnabled = true;
        }

        private void TryShowFlatpakMessage()
        {
            if (Config.Get("FlatpakMessage", OperatingSystemHelper.IsWine()))
            {
                Config.Add("FlatpakMessage", false);

                var message = "If Frosty is run through Flatpak application (Bottles, Lutris, Heroic), then make sure to select 'All user files' in Flatseal for that application.";
                message += "\r\n\r\nOtherwise Frosty Mod Manager might crash.";

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
            }
        }

        private void NewConfigButton_Click(object sender, RoutedEventArgs e)
        {
            TryShowFlatpakMessage();

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "*.exe (Game Executable)|*.exe",
                Title = "Choose Game Executable"
            };

            if (ofd.ShowDialog() == false)
            {
                FrostyMessageBox.Show("No game executable chosen.", "Frosty Mod Manager");
                return;
            }

            FileInfo fi = new FileInfo(ofd.FileName);

            // try to load game profile 
            if (!ProfilesLibrary.HasProfile(fi.Name.Remove(fi.Name.Length - 4)))
            {
                FrostyMessageBox.Show("There was an error when trying to load game using specified profile.", "Frosty Mod Manager");
                return;
            }

            // make sure config doesnt already exist
            foreach (FrostyConfiguration config in configs)
            {
                if (config.ProfileName == fi.Name.Remove(fi.Name.Length - 4))
                {
                    FrostyMessageBox.Show("That game already has a configuration.");
                    return;
                }
            }

            // create
            Config.AddGame(fi.Name.Remove(fi.Name.Length - 4), fi.DirectoryName);
            configs.Add(new FrostyConfiguration(fi.Name.Remove(fi.Name.Length - 4)));
            Config.Save();

            ConfigList.Items.Refresh();
        }

        private async void ConfigList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ConfigList.SelectedIndex == -1)
                return;

            if (ConfigList.SelectedItem is FrostyConfiguration config)
            {
                LaunchConfig(config.ProfileName);
                await Task.Delay(1);
                Close();
            }
            ConfigList.SelectedIndex = -1;
        }

        private void RemoveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrostyMessageBox.Show("Are you sure you want to delete this configuration?", "Frosty Mod Manager", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                FrostyConfiguration selectedItem = ConfigList.SelectedItem as FrostyConfiguration;

                Config.RemoveGame(selectedItem.ProfileName);
                //if (File.Exists(selectedItem.Filename))
                //{
                //    File.Delete(selectedItem.Filename);
                //}

                configs.Remove(selectedItem);
                ConfigList.Items.Refresh();

                ConfigList.SelectedIndex = 0;
                Config.Save();
            }
        }

        private async void LaunchConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigList.SelectedIndex == -1)
                return;

            if (ConfigList.SelectedItem is FrostyConfiguration config)
            {
                LaunchConfig(config.ProfileName);
                await Task.Delay(1);
                Close();
            }
            ConfigList.SelectedIndex = -1;
        }

        private void ScanForGamesButton_Click(object sender, RoutedEventArgs e)
        {
            TryShowFlatpakMessage();

            using (RegistryKey lmKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node"))
            {
                int totalCount = 0;

                IterateSubKeys(lmKey, ref totalCount);
            }

            ConfigList.Items.Refresh();
        }

        private void IterateSubKeys(RegistryKey subKey, ref int totalCount)
        {
            foreach (string subKeyName in subKey.GetSubKeyNames())
            {
                try
                {
                    IterateSubKeys(subKey.OpenSubKey(subKeyName), ref totalCount);
                }
                catch (System.Exception)
                {
                    continue;
                }
            }

            foreach (string subKeyValue in subKey.GetValueNames())
            {
                if (subKeyValue.IndexOf("Install Dir", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    string installDir = subKey.GetValue("Install Dir") as string;
                    if (string.IsNullOrEmpty(installDir))
                        continue;
                    if (!Directory.Exists(installDir))
                        continue;

                    foreach (string filename in Directory.EnumerateFiles(installDir, "*.exe"))
                    {
                        FileInfo fi = new FileInfo(filename);
                        string nameWithoutExt = fi.Name.Replace(fi.Extension, "");

                        if (ProfilesLibrary.HasProfile(nameWithoutExt))
                        {
                            foreach (FrostyConfiguration config in configs)
                            {
                                if (config.ProfileName == fi.Name.Remove(fi.Name.Length - 4))
                                    return;
                            }

                            Config.AddGame(fi.Name.Remove(fi.Name.Length - 4), fi.DirectoryName);
                            configs.Add(new FrostyConfiguration(fi.Name.Remove(fi.Name.Length - 4)));
                            //Config ini = new Config();
                            //ini.AddEntry("Init", "GamePath", fi.DirectoryName);
                            //ini.AddEntry("Init", "Profile", fi.Name.Remove(fi.Name.Length - 4));
                            //string fileName = "FrostyModManager " + ini.GetEntry("Init", "Profile", "") + ".ini";
                            //ini.SaveEntries(fileName);

                            //FrostyConfiguration cfg = new FrostyConfiguration(fileName);
                            //configs.Add(cfg);

                            totalCount++;
                        }
                    }
                }
            }
        }
    }
}
