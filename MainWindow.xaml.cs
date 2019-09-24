using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using System.Threading;

namespace RemnantSaveManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string defaultBackupFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Remnant\\Saved\\Backups";
        private static string backupDirPath;
        private static string saveDirPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Remnant\\Saved\\SaveGames";
        private List<SaveBackup> listBackups;
        private Boolean suppressLog;
        private FileSystemWatcher saveWatcher;
        private Process gameProcess;

        //private List<RemnantCharacter> activeCharacters;
        private RemnantSave activeSave;

        private SaveAnalyzer activeSaveAnalyzer;
        private List<SaveAnalyzer> backupSaveAnalyzers;

        public MainWindow()
        {
            InitializeComponent();

            System.IO.File.WriteAllText("log.txt", DateTime.Now.ToString()+": Loading...\r\n");

            if (Properties.Settings.Default.BackupFolder.Length == 0)
            {
                Properties.Settings.Default.BackupFolder = defaultBackupFolder;
                Properties.Settings.Default.Save();
                backupDirPath = Properties.Settings.Default.BackupFolder;
            } else if (!Directory.Exists(Properties.Settings.Default.BackupFolder))
            {
                Properties.Settings.Default.BackupFolder = defaultBackupFolder;
                Properties.Settings.Default.Save();
                backupDirPath = Properties.Settings.Default.BackupFolder;
            } else
            {
                backupDirPath = Properties.Settings.Default.BackupFolder;
            }
            txtBackupFolder.Text = backupDirPath;

            saveWatcher = new FileSystemWatcher();
            saveWatcher.Path = saveDirPath;

            // Watch for changes in LastWrite times.
            saveWatcher.NotifyFilter = NotifyFilters.LastWrite;

            // Only watch sav files.
            saveWatcher.Filter = "profile.sav";

            // Add event handlers.
            saveWatcher.Changed += OnSaveFileChanged;
            saveWatcher.Created += OnSaveFileChanged;
            saveWatcher.Deleted += OnSaveFileChanged;
            //watcher.Renamed += OnRenamed;

            listBackups = new List<SaveBackup>();

            suppressLog = false;
            ((MenuItem)dataBackups.ContextMenu.Items[1]).Click += deleteMenuItem_Click;
            //((MenuItem)dataBackups.ContextMenu.Items[2]).Click += infoMenuItem_Click;

            //activeCharacters = new List<RemnantCharacter>();

            activeSaveAnalyzer = new SaveAnalyzer(this)
            {
                ActiveSave = true,
                Title = "Active Save World Analyzer"
            };
            backupSaveAnalyzers = new List<SaveAnalyzer>();

            ((MenuItem)dataBackups.ContextMenu.Items[0]).Click += analyzeMenuItem_Click;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtLog.IsReadOnly = true;
            txtLog.Text = "Version " + typeof(MainWindow).Assembly.GetName().Version;
            logMessage("Loading...");
            logMessage("Current save date: " + File.GetLastWriteTime(saveDirPath + "\\profile.sav").ToString());
            //logMessage("Backups folder: " + backupDirPath);
            //logMessage("Save folder: " + saveDirPath);
            loadBackups();
            bool autoBackup = Properties.Settings.Default.AutoBackup;
            chkAutoBackup.IsChecked = autoBackup;
            txtBackupMins.Text = Properties.Settings.Default.BackupMinutes.ToString();
            txtBackupLimit.Text = Properties.Settings.Default.BackupLimit.ToString();
            /*if (autoBackup)
            {
                saveWatcher.EnableRaisingEvents = true;
            }*/
            saveWatcher.EnableRaisingEvents = true;
            //updateActiveCharacterData();
            activeSave = new RemnantSave(saveDirPath);
            updateCurrentWorldAnalyzer();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                string newGameInfoCheck = GameInfo.CheckForNewGameInfo();
                this.Dispatcher.Invoke(() =>
                {
                    logMessage(newGameInfoCheck);
                });
            }).Start();
        }

        private void loadBackups()
        {
            if (!Directory.Exists(backupDirPath))
            {
                logMessage("Backups folder not found, creating...");
                Directory.CreateDirectory(backupDirPath);
            }
            dataBackups.ItemsSource = null;
            listBackups.Clear();
            Dictionary<long, string> backupNames = getSavedBackupNames();
            Dictionary<long, bool> backupKeeps = getSavedBackupKeeps();
            string[] files = Directory.GetDirectories(backupDirPath);
            SaveBackup activeBackup = null;
            /*DataGridTextColumn col1 = new DataGridTextColumn();
            col1.Header = "Name";
            col1.Binding = new Binding("Name");
            dataBackups.Columns.Add(col1);
            DataGridTextColumn col2 = new DataGridTextColumn();
            col2.Header = "Date";
            col2.Binding = new Binding("SaveDate");
            dataBackups.Columns.Add(col2);
            DataGridCheckBoxColumn col3 = new DataGridCheckBoxColumn();
            col3.Header = "Keep";
            col3.Binding = new Binding("Keep");
            dataBackups.Columns.Add(col3);*/
            for (int i = 0; i < files.Length; i++)
            {
                //string folder = files[i].Replace(backupDirPath + "\\", "");
                if (RemnantSave.ValidSaveFolder(files[i]))
                {
                    //DateTime backupDate = getBackupDateTime(folder);
                    //logMessage("Found save backup dated " + backupDate.ToString());
                    //SaveBackup backup = new SaveBackup(backupDate);
                    SaveBackup backup = new SaveBackup(files[i]);
                    if (backupNames.ContainsKey(backup.SaveDate.Ticks))
                    {
                        backup.Name = backupNames[backup.SaveDate.Ticks];
                    }
                    if (backupKeeps.ContainsKey(backup.SaveDate.Ticks))
                    {
                        backup.Keep = backupKeeps[backup.SaveDate.Ticks];
                    }

                    if (backupActive(backup))
                    {
                        backup.Active = true;
                        activeBackup = backup;
                    }

                    //backup.Save.LoadCharacterData(files[i]);

                    backup.Updated += saveUpdated;

                    listBackups.Add(backup);
                }
            }
            dataBackups.ItemsSource = listBackups;
            logMessage("Backups found: " + listBackups.Count);
            if (listBackups.Count > 0)
            {
                logMessage("Last backup save date: " + listBackups[listBackups.Count - 1].SaveDate.ToString());
            }
            if (activeBackup != null)
            {
                dataBackups.SelectedItem = activeBackup;
            }
            SetActiveSaveIsBackedup(activeBackup != null);
        }

        private void SetActiveSaveIsBackedup(bool backedUp)
        {
            if (backedUp)
            {
                lblStatus.Content = "Backed Up";
                lblStatus.Foreground = Brushes.Green;
                btnBackup.IsEnabled = false;
            }
            else
            {
                lblStatus.Content = "Not Backed Up";
                lblStatus.Foreground = Brushes.Red;
                btnBackup.IsEnabled = true;
            }
        }

        private void saveUpdated(object sender, UpdatedEventArgs args)
        {
            if (args.FieldName.Equals("Name"))
            {
                updateSavedNames();
            }
            else if (args.FieldName.Equals("Keep"))
            {
                updateSavedKeeps();
            }
        }

        private void loadBackups(Boolean verbose)
        {
            Boolean oldVal = suppressLog;
            suppressLog = !verbose;
            loadBackups();
            suppressLog = oldVal;
        }

        /*private Boolean validBackup(String folder)
        {
            if (!File.Exists(backupDirPath + "\\" + folder + "\\profile.sav"))
            {
                return false;
            }
            return true;
        }*/

        private Boolean backupActive(SaveBackup saveBackup)
        {
            if (DateTime.Compare(saveBackup.SaveDate, File.GetLastWriteTime(saveDirPath + "\\profile.sav")) == 0)
            {
                return true;
            }
            return false;
        }

        private DateTime getBackupDateTime(string backupFolder)
        {
            return File.GetLastWriteTime(backupDirPath + "\\" + backupFolder + "\\profile.sav");
        }

        private Boolean alreadyBackedUp()
        {
            DateTime saveDate = File.GetLastWriteTime(saveDirPath + "\\profile.sav");
            for (int i = 0; i < listBackups.Count; i++)
            {
                DateTime backupDate = listBackups.ToArray()[i].SaveDate;
                if (saveDate.Equals(backupDate))
                {
                    return true;
                }
            }
            return false;
        }

        public void logMessage(string msg)
        {
            if (!suppressLog)
            {
                txtLog.Text = txtLog.Text + Environment.NewLine + msg;
                lblLastMessage.Content = msg;
            }
            StreamWriter writer = System.IO.File.AppendText("log.txt");
            writer.WriteLine(DateTime.Now.ToString() + ": " + msg);
            writer.Close();
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            doBackup();
        }

        private void doBackup()
        {
            try
            {
                bool dirExisted = true;
                int existingSaveIndex = -1;
                DateTime saveDate = File.GetLastWriteTime(saveDirPath + "\\profile.sav");
                string backupFolder = backupDirPath + "\\" + saveDate.Ticks;
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                    dirExisted = false;
                } else if (RemnantSave.ValidSaveFolder(backupFolder))
                {
                    for (int i=listBackups.Count-1; i >= 0; i--)
                    {
                        if (listBackups[i].SaveDate.Ticks == saveDate.Ticks)
                        {
                            existingSaveIndex = i;
                            break;
                        }
                    }
                }
                foreach (var file in Directory.GetFiles(saveDirPath))
                    File.Copy(file, backupFolder + "\\" + System.IO.Path.GetFileName(file), true);
                if (RemnantSave.ValidSaveFolder(backupFolder))
                {
                    Dictionary<long, string> backupNames = getSavedBackupNames();
                    Dictionary<long, bool> backupKeeps = getSavedBackupKeeps();
                    SaveBackup backup = new SaveBackup(backupFolder);
                    if (backupNames.ContainsKey(backup.SaveDate.Ticks))
                    {
                        backup.Name = backupNames[backup.SaveDate.Ticks];
                    }
                    if (backupKeeps.ContainsKey(backup.SaveDate.Ticks))
                    {
                        backup.Keep = backupKeeps[backup.SaveDate.Ticks];
                    }
                    foreach (SaveBackup saveBackup in listBackups)
                    {
                        saveBackup.Active = false;
                    }
                    backup.Active = true;
                    //activeSave = backup.Save;
                    backup.Updated += saveUpdated;
                    if (existingSaveIndex > -1)
                    {
                        listBackups[existingSaveIndex] = backup;
                    } else
                    {
                        listBackups.Add(backup);
                    }
                }
                checkBackupLimit();
                dataBackups.Items.Refresh();
                SetActiveSaveIsBackedup(true);
                btnBackup.IsEnabled = false;
                //loadBackups(false);
                logMessage($"Backup completed ({saveDate.ToString()})!");
                /*}
                else
                {
                    logMessage("This save is already backed up.");
                }*/
            }
            catch (IOException ex)
            {
                if (ex.Message.Contains("being used by another process"))
                {
                    logMessage("Save file in use; waiting 0.5 seconds and retrying.");
                    System.Threading.Thread.Sleep(500);
                    doBackup();
                }
            }
        }

        /*private void doBackup()
        {
            doBackup(false);
        }*/

        private Boolean isRemnantRunning()
        {
            Process[] pname = Process.GetProcessesByName("Remnant");
            if (pname.Length == 0)
            {
                return false;
            }
            return true;
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (isRemnantRunning())
            {
                logMessage("Exit the game before restoring a save backup.");
                return;
            }

            if (dataBackups.SelectedItem == null)
            {
                logMessage("Choose a backup to restore from the list!");
                return;
            }

            if (alreadyBackedUp())
            {
                saveWatcher.EnableRaisingEvents = false;
                System.IO.DirectoryInfo di = new DirectoryInfo(saveDirPath);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                SaveBackup selectedBackup = (SaveBackup)dataBackups.SelectedItem;
                foreach (var file in Directory.GetFiles(backupDirPath + "\\" + selectedBackup.SaveDate.Ticks))
                    File.Copy(file, saveDirPath + "\\" + System.IO.Path.GetFileName(file));
                //loadBackups(false);
                foreach (SaveBackup saveBackup in listBackups)
                {
                    saveBackup.Active = false;
                }
                selectedBackup.Active = true;
                //activeSave = selectedBackup.Save;
                updateCurrentWorldAnalyzer();
                dataBackups.Items.Refresh();
                //SetActiveSaveIsBackedup(true);
                btnRestore.IsEnabled = false;
                logMessage("Backup restored!");
                saveWatcher.EnableRaisingEvents = Properties.Settings.Default.AutoBackup;
            }
            else
            {
                logMessage("Backup your current save before restoring another!");
            }
        }

        private void ChkAutoBackup_Click(object sender, RoutedEventArgs e)
        {
            bool autoBackup = chkAutoBackup.IsChecked.HasValue ? chkAutoBackup.IsChecked.Value : false;
            Properties.Settings.Default.AutoBackup = autoBackup;
            Properties.Settings.Default.Save();
            /*if (autoBackup)
            {
                saveWatcher.EnableRaisingEvents = true;
            }
            else
            {
                saveWatcher.EnableRaisingEvents = false;
            }*/
        }

        private void OnSaveFileChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            this.Dispatcher.Invoke(() =>
            {
                //logMessage($"{DateTime.Now.ToString()} File: {e.FullPath} {e.ChangeType}");
                if (Properties.Settings.Default.AutoBackup)
                {
                    //logMessage($"Save: {File.GetLastWriteTime(e.FullPath)}; Last backup: {File.GetLastWriteTime(listBackups[listBackups.Count - 1].Save.SaveFolderPath + "\\profile.sav")}");
                    DateTime latestBackupTime;
                    DateTime newBackupTime;
                    if (listBackups.Count > 0)
                    {
                        latestBackupTime = listBackups[listBackups.Count - 1].SaveDate;
                        newBackupTime = latestBackupTime.AddMinutes(Properties.Settings.Default.BackupMinutes);
                    }
                    else
                    {
                        latestBackupTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        newBackupTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    }
                    if (DateTime.Compare(DateTime.Now, newBackupTime) > 0 || DateTime.Compare(latestBackupTime, File.GetLastWriteTime(e.FullPath)) == 0)
                    {
                        doBackup();
                    }
                    else
                    {
                        lblStatus.Content = "Not Backed Up";
                        lblStatus.Foreground = Brushes.Red;
                        btnBackup.IsEnabled = true;
                        foreach (SaveBackup backup in listBackups)
                        {
                            if (backup.Active) backup.Active = false;
                        }
                        dataBackups.Items.Refresh();
                        /*if (DateTime.Compare(DateTime.Now, newBackupTime) < 1)
                        {
                            logMessage($"Last backup less than {Properties.Settings.Default.BackupMinutes} minutes ago");
                        }
                        if (DateTime.Compare(latestBackupTime, File.GetLastWriteTime(e.FullPath)) != 0)
                        {
                            logMessage("Latest backup and current backup times not equal.");
                        }*/
                    }
                }                
                //updateActiveCharacterData();
                updateCurrentWorldAnalyzer();

                if (gameProcess == null || gameProcess.HasExited)
                {
                    Process[] processes = Process.GetProcessesByName("Remnant");
                    if (processes.Length > 0)
                    {
                        gameProcess = processes[0];
                        gameProcess.EnableRaisingEvents = true;
                        gameProcess.Exited += (s, eargs) =>
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                doBackup();
                            });
                        };
                    }
                }
            });
        }

        private void TxtBackupMins_LostFocus(object sender, RoutedEventArgs e)
        {
            updateBackupMins();
        }

        private void TxtBackupMins_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                updateBackupMins();
            }
        }

        private void updateBackupMins()
        {
            string txt = txtBackupMins.Text;
            int mins;
            bool valid = false;
            if (txt.Length > 0)
            {
                if (int.TryParse(txt, out mins))
                {
                    valid = true;
                }
                else
                {
                    mins = Properties.Settings.Default.BackupMinutes;
                }
            }
            else
            {
                mins = Properties.Settings.Default.BackupMinutes;
            }
            if (mins != Properties.Settings.Default.BackupMinutes)
            {
                Properties.Settings.Default.BackupMinutes = mins;
                Properties.Settings.Default.Save();
            }
            if (!valid)
            {
                txtBackupMins.Text = Properties.Settings.Default.BackupMinutes.ToString();
            }
        }

        private void TxtBackupLimit_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                updateBackupLimit();
            }
        }

        private void TxtBackupLimit_LostFocus(object sender, RoutedEventArgs e)
        {
            updateBackupLimit();
        }

        private void updateBackupLimit()
        {
            string txt = txtBackupLimit.Text;
            int num;
            bool valid = false;
            if (txt.Length > 0)
            {
                if (int.TryParse(txt, out num))
                {
                    valid = true;
                }
                else
                {
                    num = Properties.Settings.Default.BackupLimit;
                }
            }
            else
            {
                num = 0;
            }
            if (num != Properties.Settings.Default.BackupLimit)
            {
                Properties.Settings.Default.BackupLimit = num;
                Properties.Settings.Default.Save();
            }
            if (!valid)
            {
                txtBackupLimit.Text = Properties.Settings.Default.BackupLimit.ToString();
            }
        }

        private void checkBackupLimit()
        {
            if (listBackups.Count > Properties.Settings.Default.BackupLimit)
            {
                List<SaveBackup> removeBackups = new List<SaveBackup>();
                int delNum = listBackups.Count - Properties.Settings.Default.BackupLimit;
                for (int i = 0; i < listBackups.Count && delNum > 0; i++)
                {
                    if (!listBackups[i].Keep && !listBackups[i].Active)
                    {
                        logMessage("Deleting excess backup " + listBackups[i].Name + " (" + listBackups[i].SaveDate + ")");
                        Directory.Delete(backupDirPath + "\\" + listBackups[i].SaveDate.Ticks, true);
                        removeBackups.Add(listBackups[i]);
                        delNum--;
                    }
                }

                for (int i=0; i < removeBackups.Count; i++)
                {
                    listBackups.Remove(removeBackups[i]);
                }
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@backupDirPath);
        }

        private Dictionary<long, string> getSavedBackupNames()
        {
            Dictionary<long, string> names = new Dictionary<long, string>();
            string savedString = Properties.Settings.Default.BackupName;
            string[] savedNames = savedString.Split(',');
            for (int i = 0; i < savedNames.Length; i++)
            {
                string[] vals = savedNames[i].Split('=');
                if (vals.Length == 2)
                {
                    names.Add(long.Parse(vals[0]), System.Net.WebUtility.UrlDecode(vals[1]));
                }
            }
            return names;
        }

        private Dictionary<long, bool> getSavedBackupKeeps()
        {
            Dictionary<long, bool> keeps = new Dictionary<long, bool>();
            string savedString = Properties.Settings.Default.BackupKeep;
            string[] savedKeeps = savedString.Split(',');
            for (int i = 0; i < savedKeeps.Length; i++)
            {
                string[] vals = savedKeeps[i].Split('=');
                if (vals.Length == 2)
                {
                    keeps.Add(long.Parse(vals[0]), bool.Parse(vals[1]));
                }
            }
            return keeps;
        }

        private void DataBackups_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Column.Header.ToString().Equals("SaveDate") || e.Column.Header.ToString().Equals("Active")) e.Cancel = true;
        }

        private void DataBackups_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }

        private void updateSavedNames()
        {
            List<string> savedNames = new List<string>();
            for (int i = 0; i < listBackups.Count; i++)
            {
                SaveBackup s = listBackups[i];
                if (!s.Name.Equals(s.SaveDate.Ticks.ToString()))
                {
                    savedNames.Add(s.SaveDate.Ticks + "=" + System.Net.WebUtility.UrlEncode(s.Name));
                }
                else
                {
                }
            }
            if (savedNames.Count > 0)
            {
                Properties.Settings.Default.BackupName = string.Join(",", savedNames.ToArray());
            }
            else
            {
                Properties.Settings.Default.BackupName = "";
            }
            Properties.Settings.Default.Save();
        }

        private void updateSavedKeeps()
        {
            List<string> savedKeeps = new List<string>();
            for (int i = 0; i < listBackups.Count; i++)
            {
                SaveBackup s = listBackups[i];
                if (s.Keep)
                {
                    savedKeeps.Add(s.SaveDate.Ticks + "=True");
                }
            }
            if (savedKeeps.Count > 0)
            {
                Properties.Settings.Default.BackupKeep = string.Join(",", savedKeeps.ToArray());
            }
            else
            {
                Properties.Settings.Default.BackupKeep = "";
            }
            Properties.Settings.Default.Save();
        }

        private void DataBackups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MenuItem analyzeMenu = ((MenuItem)dataBackups.ContextMenu.Items[0]);
            MenuItem deleteMenu = ((MenuItem)dataBackups.ContextMenu.Items[1]);
            //MenuItem infoMenu = ((MenuItem)dataBackups.ContextMenu.Items[2]);
            if (e.AddedItems.Count > 0)
            {
                SaveBackup selectedBackup = (SaveBackup)(dataBackups.SelectedItem);
                if (backupActive(selectedBackup))
                {
                    btnRestore.IsEnabled = false;
                }
                else
                {
                    btnRestore.IsEnabled = true;
                }

                analyzeMenu.IsEnabled = true;
                deleteMenu.IsEnabled = true;
                //analyzeMenu.Items.Clear();
                //analyzeMenu.Click -= analyzeMenuItem_Click;
                //analyzeMenu.ToolTip = null;
                //infoMenu.IsEnabled = true;
            }
            else
            {
                analyzeMenu.IsEnabled = false;
                deleteMenu.IsEnabled = false;
                //infoMenu.IsEnabled = true;
                btnRestore.IsEnabled = false;
            }
        }

        private void analyzeMenuItem_Click(object sender, System.EventArgs e)
        {
            SaveBackup saveBackup = (SaveBackup)dataBackups.SelectedItem;
            logMessage("Showing backup save (" + saveBackup.Name + ") world analyzer...");
            SaveAnalyzer analyzer = new SaveAnalyzer(this);
            analyzer.Title = "Backup Save ("+saveBackup.Name+") World Analyzer";
            analyzer.Closing += Backup_Analyzer_Closing;
            List<RemnantCharacter> chars = saveBackup.Save.Characters;
            for (int i = 0; i < chars.Count; i++)
            {
                chars[i].LoadWorldData(i);
            }
            analyzer.LoadData(chars);
            backupSaveAnalyzers.Add(analyzer);
            analyzer.Show();
        }

        private void Backup_Analyzer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            backupSaveAnalyzers.Remove((SaveAnalyzer)sender);
        }

        private void deleteMenuItem_Click(object sender, System.EventArgs e)
        {
            SaveBackup save = (SaveBackup)dataBackups.SelectedItem;
            var confirmResult = MessageBox.Show("Are you sure to delete backup \"" + save.Name + "\" (" + save.SaveDate.ToString() + ")?",
                                     "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (confirmResult == MessageBoxResult.Yes)
            {
                if (save.Keep)
                {
                    confirmResult = MessageBox.Show("This backup is marked for keeping. Are you SURE to delete backup \"" + save.Name + "\" (" + save.SaveDate.ToString() + ")?",
                                     "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                    if (confirmResult != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                if (save.Active)
                {
                    SetActiveSaveIsBackedup(false);
                }
                Directory.Delete(backupDirPath + "\\" + save.SaveDate.Ticks, true);
                listBackups.Remove(save);
                dataBackups.Items.Refresh();
                logMessage("Backup \"" + save.Name + "\" (" + save.SaveDate + ") deleted.");
            }
        }

        private void BtnAnalyzeCurrent_Click(object sender, RoutedEventArgs e)
        {
            logMessage("Showing current save world analyzer...");
            activeSaveAnalyzer.Show();
        }

        private void updateCurrentWorldAnalyzer()
        {
            //activeCharacters = RemnantCharacter.GetCharactersFromSave(saveDirPath);//getAllSaveData(saveDirPath);
            activeSave.UpdateCharacters();
            /*for (int i = 0; i < activeSave.Characters.Count; i++)
            {
                Console.WriteLine(activeSave.Characters[i]);
            }*/
            activeSaveAnalyzer.LoadData(activeSave.Characters);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /*activeSaveAnalyzer.ActiveSave = false;
            activeSaveAnalyzer.Close();
            for (int i = backupSaveAnalyzers.Count - 1; i > -1; i--)
            {
                backupSaveAnalyzers[i].Close();
            }*/
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            System.Environment.Exit(1);
        }

        private void BtnBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            openFolderDialog.SelectedPath = backupDirPath;
            //openFolderDialog.RootFolder = Environment.SpecialFolder.LocalApplicationData;
            openFolderDialog.Description = "Select the folder where you want your backup saves kept.";
            System.Windows.Forms.DialogResult result = openFolderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string folderName = openFolderDialog.SelectedPath;
                if (folderName.Equals(saveDirPath))
                {
                    MessageBox.Show("Please select a folder other than the game's save folder.",
                                     "Invalid Folder", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                    return;
                }
                if (folderName.Equals(backupDirPath))
                {
                    return;
                }
                if (listBackups.Count > 0)
                {
                    var confirmResult = MessageBox.Show("Do you want to move your backups to this new folder?",
                                     "Move Backups", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    if (confirmResult == MessageBoxResult.Yes)
                    {
                        List<String> backupFiles = Directory.GetDirectories(backupDirPath).ToList();
                        foreach (string file in backupFiles)
                        {
                            if (File.Exists(file+@"\profile.sav"))
                            {
                                string subFolderName = file.Substring(file.LastIndexOf(@"\"));
                                Directory.Move(file, folderName + subFolderName);
                            }
                        }
                    }
                }
                txtBackupFolder.Text = folderName;
                backupDirPath = folderName;
                Properties.Settings.Default.BackupFolder = folderName;
                Properties.Settings.Default.Save();
                loadBackups();
            }
        }

        private void DataBackups_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.Equals("Save")) {
                e.Cancel = true;
            }
        }
    }
}
