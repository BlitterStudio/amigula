using Amigula.AmigulaDBDataSetTableAdapters;
using Amigula.Helpers;
using Amigula.Properties;
using HtmlAgilityPack;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

[assembly: CLSCompliant(true)]

namespace Amigula
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : IDisposable
    {
        private static readonly ProgressBar ProgBar = new ProgressBar();
        private static List<string> _uaeConfigViewSource;
        private readonly GamesTableAdapter _amigulaDbDataSetGamesTableAdapter = new GamesTableAdapter();
        private readonly GenresTableAdapter _amigulaDbDataSetGenresTableAdapter = new GenresTableAdapter();
        private readonly PublishersTableAdapter _amigulaDbDataSetPublishersTableAdapter = new PublishersTableAdapter();
        private AmigulaDBDataSet _amigulaDbDataSet;
        private CollectionViewSource _gamesViewSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            if (!EnsureSingleInstance())
            {
                Close();
                return;
            }

            RestoreSettingsForMainWindowPosition();
            CenterMainWindowIfNoSettingsFound();

            SetWindowTitleText();

            InitializeComponent();
            InitializeDataSet();
            InitializeViewSource();

            InitialChecks();

            RefreshUaEconfigs();
            BindUaeConfigItemSource();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MainWindow"/> class.
        /// </summary>
        ~MainWindow()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            // free managed resources
            _amigulaDbDataSetGamesTableAdapter?.Dispose();

            _amigulaDbDataSetGenresTableAdapter?.Dispose();

            _amigulaDbDataSetPublishersTableAdapter?.Dispose();
        }

        ///
        /// Checks the file exists or not.
        ///
        /// The URL of the remote file.
        /// True : If the file exits, False if file not exists
        private static bool RemoteFileExists(string url)
        {
            try
            {
                //Creating the HttpWebRequest
                var request = WebRequest.Create(url) as HttpWebRequest;
                //Setting the Request method HEAD, you can also use GET too.
                if (request != null)
                {
                    request.Method = "HEAD";
                    //Getting the Web Response.
                    var response = request.GetResponse() as HttpWebResponse;
                    //Returns TRUE if the Status code == 200
                    return response != null && (response.StatusCode == HttpStatusCode.OK);
                }
            }
            catch
            {
                //Any exception will return false.
                return false;
            }
            return false;
        }

        /// <summary>
        /// Binds the uae configuration item source.
        /// </summary>
        private void BindUaeConfigItemSource()
        {
            try
            {
                comboUAEconfig.ItemsSource = _uaeConfigViewSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to populate the combobox with the UAE config files:\n\n" +
                    ex.Message, "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Centers the main window if no settings found.
        /// </summary>
        private void CenterMainWindowIfNoSettingsFound()
        {
            //if (int.Parse(Top.ToString(CultureInfo.InvariantCulture)) == 0 && int.Parse(Left.ToString(CultureInfo.InvariantCulture)) == 0)
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        /// <summary>
        /// Initializes the data set.
        /// </summary>
        private void InitializeDataSet()
        {
            _amigulaDbDataSet = ((AmigulaDBDataSet)(FindResource("AmigulaDBDataSet")));
        }

        /// <summary>
        /// Initializes the game counter.
        /// </summary>
        private void InitializeGameCounter()
        {
            // ReSharper disable once UnusedVariable
            IDisposable numberOfGamesChanged = Observable.FromEventPattern<EventArgs>(GamesListView, "LayoutUpdated")
                .Subscribe(games => UpdateNoOfGames());
        }

        /// <summary>
        /// Initializes the seach field.
        /// </summary>
        private void InitializeSeachField()
        {
            // ReSharper disable once UnusedVariable
            IDisposable gameFilterChanged = Observable.FromEventPattern<EventArgs>(tboxFilterGames, "TextChanged")
                .Select(searched => ((TextBox)searched.Sender).Text)
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(tboxFilterGames)
                .Subscribe(FilterListItems);
        }

        /// <summary>
        /// Initializes the selected game monitoring.
        /// </summary>
        private void InitializeSelectedGameMonitoring()
        {
            // ReSharper disable once UnusedVariable
            IDisposable gameSelectionChanged = Observable.FromEventPattern<EventArgs>(GamesListView, "SelectionChanged")
                .Select(selected => ((ListView)selected.Sender).SelectedItem)
                .Subscribe(ShowGameMedia);
        }

        /// <summary>
        /// Initializes the view source.
        /// </summary>
        private void InitializeViewSource()
        {
            _gamesViewSource = ((CollectionViewSource)(FindResource("GamesViewSource")));
        }

        /// <summary>
        /// Populates the game list by filename.
        /// </summary>
        private void PopulateGameListByFilename()
        {
            try
            {
                _amigulaDbDataSetGamesTableAdapter.FillByAllFiles(_amigulaDbDataSet.Games);
                viewMenu_Show_AllFiles.IsChecked = true;
                viewMenu_Show_GameTitles.IsChecked = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception occured while trying to read from the database:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Populates the game list by title.
        /// </summary>
        private void PopulateGameListByTitle()
        {
            try
            {
                _amigulaDbDataSetGamesTableAdapter.FillByTitle(_amigulaDbDataSet.Games);
                viewMenu_Show_GameTitles.IsChecked = true;
                viewMenu_Show_AllFiles.IsChecked = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception occured while trying to read from the database:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Populates the genre list.
        /// </summary>
        private void PopulateGenreList()
        {
            _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
        }

        /// <summary>
        /// Populates the publisher list.
        /// </summary>
        private void PopulatePublisherList()
        {
            _amigulaDbDataSetPublishersTableAdapter.Fill(_amigulaDbDataSet.Publishers);
        }

        /// <summary>
        /// Places the main window.
        /// </summary>
        private void RestoreSettingsForMainWindowPosition()
        {
            try
            {
                Top = Settings.Default.Top;
                Left = Settings.Default.Left;
                Height = Settings.Default.Height;
                Width = Settings.Default.Width;
                WindowState = Settings.Default.WindowSetting;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception has occured while trying to position the window!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Sets the window title text.
        /// </summary>
        private void SetWindowTitleText()
        {
            Title = Assembly.GetExecutingAssembly().GetName().Name + " v" +
                    Assembly.GetExecutingAssembly().GetName().Version;
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.ShowTitlesOption)
            {
                PopulateGameListByTitle();
            }
            else
            {
                PopulateGameListByFilename();
            }

            PopulateGenreList();
            PopulatePublisherList();

            _gamesViewSource.View.MoveCurrentToFirst();
            ShowGameMedia(GamesListView.SelectedItem);

            InitializeSeachField();
            InitializeSelectedGameMonitoring();
            InitializeGameCounter();
        }

        #region Functions

        private static string[] AmigaForeverKeys
        {
            // These are the keys we're interested in, if Amiga Forever is installed
            // AmigaFiles: where the WinUAE configuration files will be
            // Path: where AmigaForever binaries (WinUAE) is installed
            get
            {
                var afKeys = new[] { "AmigaFiles", "Path" };
                return afKeys;
            }
        }

        private static string RegistryRootKey
        {
            get
            {
                // Detect whether we're running on a 64-bit OS, change the registry scope accordingly
                string rootKey = OSBitCheck.Is64BitOperatingSystem()
                    ? "SOFTWARE\\Wow6432Node\\CLoanto\\Amiga Forever"
                    : "SOFTWARE\\CLoanto\\Amiga Forever";
                return rootKey;
            }
        }

        private static HashSet<string> ValidFilenameExtensions
        {
            get
            {
                var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".zip",
                    ".adz",
                    ".adf",
                    ".dms",
                    ".ipf"
                };
                return extensions;
            }
        }

        private IObservable<EventPattern<EventArgs>> CancelClicked
        {
            get
            {
                // Set the Cancel click event as an observable so we can monitor it
                IObservable<EventPattern<EventArgs>> cancelClicked = Observable.FromEventPattern<EventArgs>(btnCancel,
                    "Click");
                return cancelClicked;
            }
        }

        private DataRowView SelectedGameRowView
        {
            get
            {
                var oDataRowView = GamesListView.SelectedItem as DataRowView;
                return oDataRowView;
            }
        }

        /// <summary>
        ///     Add a new Screenshot to a game
        /// </summary>
        /// <param name="screenshotFilename">The full path and filename for the screenshot</param>
        /// <param name="gameTitle">The game's title</param>
        private static void AddGameScreenshot(string screenshotFilename, string gameTitle)
        {
            int n;

            // Get the first letter of the game, to get the subfolder from that.
            // if the first letter is a number, the subfolder should be set to "0"
            var gameSubFolder = int.TryParse(gameTitle.Substring(0, 1), out n) ? "0\\" : gameTitle.Substring(0, 1) + "\\";

            CopyScreenshotToFolder(screenshotFilename, gameTitle, gameSubFolder);
        }

        /// <summary>
        ///     Shows a warning that an application is not defined/selected in Preferences.
        /// </summary>
        /// <param name="messageText">The text to be displayed in the messagebox</param>
        /// <param name="appHandler">The type of application missing from Preferences</param>
        private static void AppNotDefined(string messageText, string appHandler)
        {
            // Show a warning that an application is not defined/selected in Preferences.
            // After that, allow the user to set the path to the application and save it in the Settings.
            MessageBoxResult result = MessageBox.Show(messageText, "No Application Specified", MessageBoxButton.YesNo,
                                                      MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            var selectFile = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                DefaultExt = ".exe",
                Filter = "Executable files (*.exe)|*.exe"
            };

            bool? appResult = selectFile.ShowDialog();

            // Process open file dialog box results
            if (appResult != true) return;
            // Select file
            if (appHandler == "Emulator")
            {
                Settings.Default.EmulatorPath = selectFile.FileName;
            }
            if (appHandler == "MusicPlayer")
            {
                Settings.Default.MusicPlayerPath = selectFile.FileName;
            }

            SaveDefaultSettings();
        }

        /// <summary>
        /// Brings the main window to foreground.
        /// </summary>
        /// <param name="runningProcess">The running process.</param>
        private static void BringMainWindowToForeground(Process runningProcess)
        {
            SafeNativeMethods.SetForegroundWindow(runningProcess.MainWindowHandle);
        }

        private static void CheckIfUaeConfigsExist()
        {
            try
            {
                IEnumerable<string> configFiles = Directory.EnumerateFiles(Settings.Default.UAEConfigsPath, "*.uae");
                _uaeConfigViewSource = configFiles.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to locate the UAE config files:\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Cleanup the selected game title from the list, based on the filename
        /// </summary>
        /// <param name="currentgame">The selected game title to cleanup</param>
        /// <param name="cleanupType">The type of cleanup to attempt, possible values are "URL", "Path", "Screenshot"</param>
        /// <returns></returns>
        private static string CleanGameTitle(object currentgame, string cleanupType)
        {
            // Determine the current game Title, based on the selected item in the list
            // We have to strip out the proper title from any extra strings contained there
            // such as version, year, etc.
            // cleanupType parameter is specified to determine what type of cleanup is required
            // possible values are:
            // cleanupType = "URL" - cleanup for URL usage (replace spaces with "%20")
            // cleanupType = "Path" - cleanup for full path usage (for example launching title in WinUAE)
            // cleanupType = "Screenshot" - cleanup for Screenshot usage (replace spaces with underscores)

            var oDataRowView = currentgame as DataRowView;
            var selectedGame = "";
            if (oDataRowView == null) return selectedGame;
            switch (cleanupType)
            {
                case "Screenshot":
                    {
                        selectedGame = oDataRowView.Row["Title"] as string;
                        int n;
                        string gameSubFolder = null;

                        // Get the first letter of the game, to get the subfolder from that.
                        // if the first letter is a number, the subfolder should be set to "0"
                        if (selectedGame != null && int.TryParse(selectedGame.Substring(0, 1), out n)) gameSubFolder = "0\\";
                        else if (selectedGame != null) gameSubFolder = selectedGame.Substring(0, 1) + "\\";

                        // Use RegEx to clean up anything in () or []
                        if (selectedGame != null)
                        {
                            selectedGame = Regex.Replace(selectedGame, @"[\[(].+?[\])]", "");

                            // if there's version information (e.g. v1.2) in the filename exclude it as well
                            //if (selectedGame.IndexOf(" v", StringComparison.OrdinalIgnoreCase) > 1 &&
                            //    int.TryParse(selectedGame.Substring(selectedGame.IndexOf(" v", StringComparison.OrdinalIgnoreCase) + 2, 1), out n))
                            if (Regex.IsMatch(selectedGame, @"\sv(\d{1})"))
                            {
                                selectedGame = selectedGame.Substring(0,
                                    selectedGame.IndexOf(" v",
                                        StringComparison
                                            .OrdinalIgnoreCase));
                            }

                            // now try to match the filename to the title selected, adding ".png" at the end
                            // this is far from perfect, needs improvement!
                            if (selectedGame.Length > 0)
                                selectedGame = Regex.Replace(selectedGame, " $", "").Replace(" ", "_") + ".png";
                            // join the subfolder and game filename together before returning it
                            selectedGame = gameSubFolder + selectedGame;
                        }
                        break;
                    }
                case "Path":
                    {
                        // prepare the string for passing it to WinUAE as a parameter
                        // a configuration file must be passed to WinUAE besides the actual filename
                        var selectedGamePath = oDataRowView.Row["PathToFile"] as string;
                        var selectedUaeConfig = oDataRowView.Row["UAEconfig"] as string;

                        // new variable to hold a list of all the game disks, with full path-filenames
                        var gameDisksFullPath = IdentifyGameDisks(selectedGamePath);

                        // variable to hold the "diskimageX=" values in the UAE config, one for each disk found
                        var diskImageX = new SortedList<int, string>();

                        // If there are more than 1 disks for this game
                        if (gameDisksFullPath.Count > 1)
                        {
                            // then for each disk found, we need to add an entry in the UAE config file to pass it to the DiskSwapper
                            for (var i = 0; i < gameDisksFullPath.Count; i++)
                            {
                                // replace any entry of diskimageX=* (where X=number and *=anything)
                                diskImageX[i] = "diskimage" + i + "=.*";
                                // text to be placed in the UAE config for the DiskSwapper
                                gameDisksFullPath[i] = "diskimage" + i + "=" + gameDisksFullPath[i];
                            }
                            // cleanup any extra entries of diskimageX in the config file
                            for (var i = gameDisksFullPath.Count; i < 20; i++)
                            {
                                diskImageX[i] = "diskimage" + i + "=.*";
                                gameDisksFullPath[i] = "diskimage" + i + "=";
                            }
                            // open the UAE config, check if it contains any entries for "diskimage="
                            // if it does, replace them with the current disks of the selected game
                            // if it doesn't, append those lines to the config file
                            if (selectedUaeConfig == "default")
                                FilesHelper.ReplaceInFile("configs\\" + selectedUaeConfig + ".uae", diskImageX,
                                    gameDisksFullPath);
                            else if (selectedUaeConfig != null)
                                FilesHelper.ReplaceInFile(
                                    Path.Combine(Settings.Default.UAEConfigsPath, selectedUaeConfig) + ".uae",
                                    diskImageX, gameDisksFullPath);
                        }

                        // finally, pass it over as a parameter to UAE below
                        // if the config file doesn't exist, WinUAE should still startup with the full GUI so it should be safe no to check for it
                        if (selectedUaeConfig == "default")
                            selectedGame =
                                $"-f \"{Path.Combine(Environment.CurrentDirectory, "configs\\" + selectedUaeConfig + ".uae")}\"" +
                                $" -0 \"{selectedGamePath}\"";
                        else if (selectedUaeConfig != null)
                            selectedGame =
                                $"-f \"{Path.Combine(Environment.CurrentDirectory, Path.Combine(Settings.Default.UAEConfigsPath, selectedUaeConfig) + ".uae")}\" -s use_gui=no -0 \"{selectedGamePath}\"";
                        break;
                    }
                case "URL":
                    {
                        // prepare the string for passing it to a URL as a parameter
                        // Replace any spaces with "%20" and try to clean up the title
                        selectedGame = oDataRowView.Row["Title"] as string;
                        // Use RegEx to clean up anything in () or []
                        if (selectedGame != null)
                        {
                            selectedGame = Regex.Replace(selectedGame, @"[\[(].+?[\])]", "");
                            // if there's version information (e.g. v1.2) in the filename exclude it as well
                            if (Regex.IsMatch(selectedGame, @"\sv(\d{1})"))
                            {
                                selectedGame = selectedGame.Substring(0,
                                    selectedGame.IndexOf(" v",
                                        StringComparison
                                            .OrdinalIgnoreCase));
                            }
                            if (selectedGame.Length > 0) selectedGame = selectedGame.TrimEnd(' ').Replace(" ", "%20");
                        }
                        break;
                    }
            }
            return selectedGame;
        }

        private static void CleanupUaeConfigContents()
        {
            for (int i = 0; i < _uaeConfigViewSource.Count; i++)
            {
                _uaeConfigViewSource[i] = Path.GetFileNameWithoutExtension(_uaeConfigViewSource[i]);
            }
        }

        /// <summary>
        /// Confirms the user action.
        /// </summary>
        /// <param name="img">The img.</param>
        /// <returns></returns>
        private static bool ConfirmUserAction(string img)
        {
            MessageBoxResult result =
                MessageBox.Show("Are you sure? This will DELETE the following file from the Screenshots folder:\n\n" +
                                img, "Please confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        private static void CopyScreenshotToFolder(string screenshotFilename, string gameTitle, string gameSubFolder)
        {
            try
            {
                if (!Directory.Exists(Path.Combine(Settings.Default.ScreenshotsPath, gameSubFolder)))
                    Directory.CreateDirectory(Path.Combine(Settings.Default.ScreenshotsPath, gameSubFolder));
                else
                {
                    if (
                        !File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                            gameSubFolder + gameTitle.Replace(" ", "_") + ".png")))
                    {
                        File.Copy(screenshotFilename,
                            Path.Combine(Settings.Default.ScreenshotsPath,
                                gameSubFolder + gameTitle.Replace(" ", "_") + ".png"));
                    }
                    else if (
                        !File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                            gameSubFolder + gameTitle.Replace(" ", "_") + "_1.png")))
                    {
                        File.Copy(screenshotFilename,
                            Path.Combine(Settings.Default.ScreenshotsPath,
                                gameSubFolder + gameTitle.Replace(" ", "_") + "_1.png"));
                    }
                    else if (
                        !File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                            gameSubFolder + gameTitle.Replace(" ", "_") + "_2.png")))
                    {
                        File.Copy(screenshotFilename,
                            Path.Combine(Settings.Default.ScreenshotsPath,
                                gameSubFolder + gameTitle.Replace(" ", "_") + "_2.png"));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to copy the screenshot into the Screenshots folder!\n\n" +
                    ex.Message, "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Make sure there is no other instance of the application running
        /// </summary>
        /// <returns></returns>
        private static bool EnsureSingleInstance()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process runningProcess = (from process in Process.GetProcesses()
                                      where
                                          process.Id != currentProcess.Id &&
                                          process.ProcessName.Equals(
                                              currentProcess.ProcessName,
                                              StringComparison.Ordinal)
                                      select process).FirstOrDefault();

            if (runningProcess == null) return true;

            ShowMainWindow(runningProcess);
            BringMainWindowToForeground(runningProcess);
            return false;
        }

        /// <summary>
        ///     Check if Amiga Forever is installed
        /// </summary>
        /// <returns></returns>
        private static void GetAmigaForeverRegistry()
        {
            var rootKey = RegistryRootKey;
            var afKeys = AmigaForeverKeys;

            ReadKeysFromRegistry(rootKey, afKeys);
        }

        private static string GetFetchedGenre(HtmlDocument document)
        {
            // XPath for Genre: //table[@width='100%']/tr[12]/td/table/tr[2]/td[2]/a
            string fetchedGenre =
                document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[13]/td[1]/table/tr[2]/td[2]/a")
                    .InnerText;
            return fetchedGenre;
        }

        private static string GetFetchedPublisher(HtmlDocument document)
        {
            // XPath for Publisher: //table[@width='100%']/tr[2]/td[4]/a
            string fetchedPublisher = "";
            if (document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[2]/td[4]/a") != null)
                fetchedPublisher = document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[2]/td[4]/a").InnerText;
            return fetchedPublisher;
        }

        private static string GetFetchedYear(HtmlDocument document)
        {
            // XPath for Year: //table[@width='100%']/tr[1]/td[2]/a
            string fetchedYear = "";
            if (document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[1]/td[2]/a") != null)
                fetchedYear = document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[1]/td[2]/a").InnerText;
            //MessageBox.Show("The game's Year is: " + fetchedYear);
            return fetchedYear;
        }

        private static string GetGameUrl(string gamelink)
        {
            string gameurl = gamelink.Substring(gamelink.IndexOf("http", StringComparison.Ordinal),
                gamelink.IndexOf(",", StringComparison.Ordinal) - gamelink.IndexOf("http", StringComparison.Ordinal));
            return gameurl;
        }

        /// <summary>
        ///     Get the game Year from the filename if it exists
        /// </summary>
        /// <param name="selectedGamePath">The selected game filename</param>
        /// <returns></returns>
        private static int GetGameYear(string selectedGamePath)
        {
            // Try to get the game Year from the filename
            // e.g. gameTitle (1988) (Psygnosis).zip should return 1988 as gameYear
            int gameYear = 1900; // default year if no other is found

            if (Regex.IsMatch(selectedGamePath, @"\((\d{4})\)"))
                int.TryParse(Regex.Replace(Regex.Match(selectedGamePath, @"\((\d{4})\)").Value, @"\(|\)", ""),
                             out gameYear);
            return gameYear;
        }

        /// <summary>
        ///     Detect the number of disks for a game based on the filename format
        /// </summary>
        /// <param name="selectedGamePath">The selected game filename</param>
        /// <returns></returns>
        private static SortedList<int, string> IdentifyGameDisks(string selectedGamePath)
        {
            // If the game consists of more than 1 Disk, then the first disk should be passed to WinUAE as usual,
            // but the rest of them should go in the DiskSwapper feature of WinUAE. To do that, the config file must be
            // edited and lines diskimage0-19=<path to filename> must be appended/edited.

            // Checks to be done for possible versions of multi-disk games:
            // 1. <game> Disk1.zip, <game> Disk2.zip etc.
            // 2. <game> Disk01.zip, <game> Disk02.zip etc.
            // 3. <game> (Disk 1 of 2).zip, <game> (Disk 2 of 2).zip etc.
            // 4. <game> (Disk 01 of 11).zip, <game> (Disk 02 of 11).zip etc.
            // 5. <game>-1.zip, <game>-2.zip etc.
            int n = 0;
            if (Regex.IsMatch(selectedGamePath, @"Disk(\d{1})\....$") &&
                int.TryParse(
                    selectedGamePath.Substring(
                        selectedGamePath.IndexOf("Disk", StringComparison.OrdinalIgnoreCase) + 4, 1), out n))
            {
                // case 1. <game> Disk1.zip, <game> Disk2.zip etc.
                //MessageBox.Show("Found case 1. <game> Disk1.zip, <game> Disk2.zip etc.\n\nSelected game was Disk " + n.ToString());
                var gameDisksFullPath = new SortedList<int, string>();
                n = 0;
                int diskNumber = 1;
                do
                {
                    //gameDisksFullPath[n] = selectedGamePath.Replace("Disk1","Disk"+n);
                    gameDisksFullPath[n] = Regex.Replace(selectedGamePath, @"Disk(\d{1})\.", "Disk" + diskNumber + ".");
                    n++;
                    diskNumber++;
                } while (File.Exists(Regex.Replace(selectedGamePath, @"Disk(\d{1})\.", "Disk" + diskNumber + ".")));
                return gameDisksFullPath;
            }
            if (Regex.IsMatch(selectedGamePath, @"Disk(\d{2})\....$") &&
                int.TryParse(
                    selectedGamePath.Substring(
                        selectedGamePath.IndexOf("Disk", StringComparison.OrdinalIgnoreCase) + 4, 2), out n))
            {
                // case 2. <game> Disk01.zip, <game> Disk02.zip etc.
                //MessageBox.Show("Found case 1. <game> Disk1.zip, <game> Disk2.zip etc.\n\nSelected game was Disk " + n.ToString());
                var gameDisksFullPath = new SortedList<int, string>();
                n = 0;
                int diskNumber = 1;
                do
                {
                    //gameDisksFullPath[n] = selectedGamePath.Replace("Disk1","Disk"+n);
                    gameDisksFullPath[n] = Regex.Replace(selectedGamePath, @"Disk(\d{2})\.",
                                                         "Disk" + diskNumber.ToString(CultureInfo.InvariantCulture) + ".");
                    n++;
                    diskNumber++;
                } while (
                    File.Exists(Regex.Replace(selectedGamePath, @"Disk(\d{2})\.", "Disk" + diskNumber.ToString(CultureInfo.InvariantCulture) + ".")));
                return gameDisksFullPath;
            }
            if (Regex.IsMatch(selectedGamePath, @"Disk\s(\d{1})\sof") &&
                int.TryParse(
                    selectedGamePath.Substring(
                        selectedGamePath.IndexOf("Disk ", StringComparison.OrdinalIgnoreCase) + 5, 1), out n))
            {
                // case 3. <game> (Disk 1 of 2).zip, <game> (Disk 2 of 2).zip etc.
                //MessageBox.Show("case 2. <game> (Disk 1 of 2).zip, <game> (Disk 2 of 2).zip etc.");
                var gameDisksFullPath = new SortedList<int, string>();
                n = 0;
                int diskNumber = 1;
                do
                {
                    gameDisksFullPath[n] = Regex.Replace(selectedGamePath, @"Disk\s(\d{1})\sof",
                                                         "Disk " + diskNumber + " of");
                    n++;
                    diskNumber++;
                } while (File.Exists(Regex.Replace(selectedGamePath, @"Disk\s(\d{1})\sof", "Disk " + diskNumber + " of")));
                return gameDisksFullPath;
            }
            if (Regex.IsMatch(selectedGamePath, @"Disk\s(\d{2})\sof\s(\d{2})") &&
                int.TryParse(
                    selectedGamePath.Substring(
                        selectedGamePath.IndexOf("Disk ", StringComparison.OrdinalIgnoreCase) + 5, 2), out n))
            {
                // case 4. <game> (Disk 01 of 11).zip, <game> (Disk 02 of 11).zip etc.
                //MessageBox.Show("case 3. <game> (Disk 01 of 11).zip, <game> (Disk 02 of 11).zip etc.");
                var gameDisksFullPath = new SortedList<int, string>();
                n = 0;
                int diskNumber = 1;
                do
                {
                    gameDisksFullPath[n] = Regex.Replace(selectedGamePath, @"Disk\s(\d{2})\sof",
                                                         "Disk " + diskNumber.ToString("00") + " of");
                    n++;
                    diskNumber++;
                } while (
                    File.Exists(Regex.Replace(selectedGamePath, @"Disk\s(\d{2})\sof",
                                              "Disk " + diskNumber.ToString("00") + " of")));
                return gameDisksFullPath;
            }
            if (Regex.IsMatch(selectedGamePath, @"-(\d{1})\....$"))
            {
                // case 5. <game>-1.zip, <game>-2.zip etc.
                //MessageBox.Show("case 4. <game>-1.zip, <game>-2.zip etc.");
                var gameDisksFullPath = new SortedList<int, string>();
                n = 0;
                int diskNumber = 1;
                do
                {
                    gameDisksFullPath[n] = Regex.Replace(selectedGamePath, @"-(\d{1})\.", "-" + diskNumber + ".");
                    n++;
                    diskNumber++;
                } while (File.Exists(Regex.Replace(selectedGamePath, @"-(\d{1})\.", "-" + diskNumber + ".")));
                return gameDisksFullPath;
            }
            else
            {
                // if all else fails, return the one disked game back
                var gameDisksFullPath = new SortedList<int, string> {[n] = selectedGamePath};
                return gameDisksFullPath;
            }
        }

        private static void IncreaseTimesPlayedCounter(DataRowView oDataRowView)
        {
            oDataRowView.Row["TimesPlayed"] = (int)oDataRowView.Row["TimesPlayed"] + 1;
        }

        /// <summary>
        ///     Perfom initial checks and configuration validation
        /// </summary>
        private static void InitialChecks()
        {
            // If there's no emulator set in Preferences, check for AmigaForever first
            // If that is not found, check for WinUAE and if that is not found either, show a warning
            if (string.IsNullOrEmpty(Settings.Default.EmulatorPath)
                // correct is OR not AND here, but I guess second check is not needed
                //&& String.IsNullOrEmpty(Settings.Default.UAEConfigsPath)
                )
            {
                // Check if Amiga Forever is installed
                GetAmigaForeverRegistry();

                // If AmigaForever was not found, do a secondary check for WinUAE itself
                if (string.IsNullOrEmpty(Settings.Default.EmulatorPath) ||
                    string.IsNullOrEmpty(Settings.Default.UAEConfigsPath))
                {
                    if (
                        Directory.Exists(
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinUAE")))
                    {
                        // WinUAE was found in Program Files, check if Configurations exists under Public Documents or the WinUAE folder
                        Settings.Default.EmulatorPath =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                         "WinUAE\\WinUAE.exe");
                        if (
                            Directory.Exists(
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                                             "Amiga Files\\WinUAE\\Configurations")))
                            Settings.Default.UAEConfigsPath =
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                                             "Amiga Files\\WinUAE\\Configurations");
                        else if (
                            Directory.Exists(
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                             "WinUAE\\Configurations")))
                            Settings.Default.UAEConfigsPath =
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                             "WinUAE\\Configurations");
                        Settings.Default.Save();
                    }
                    else
                        // Do a secondary check in case our operating system is Windows XP 32-bit (and WinUAE is under Program Files)
                        if (
                            Directory.Exists(
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinUAE")))
                        {
                            // WinUAE was found in Program Files, check if Configurations exists under Public Documents or the WinUAE folder
                            Settings.Default.EmulatorPath =
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                             "WinUAE\\WinUAE.exe");
                            if (
                                Directory.Exists(
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                                                 "Amiga Files\\WinUAE\\Configurations")))
                                Settings.Default.UAEConfigsPath =
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                                                 "Amiga Files\\WinUAE\\Configurations");
                            else if (
                                Directory.Exists(
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                                 "WinUAE\\Configurations")))
                                Settings.Default.UAEConfigsPath =
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                                 "WinUAE\\Configurations");
                            Settings.Default.Save();
                        }
                        else
                            AppNotDefined(
                                "The path to WinUAE is not defined in the preferences and I couldn't locate it automatically!\n\nWithout WinUAE, you can't run any games.\n\nDo you want to select the path now?",
                                "Emulator");
                }
            }
            else
            {
                var emulatorSettingsPath = Settings.Default.EmulatorPath;
                var emulatorDirName = Path.GetDirectoryName(emulatorSettingsPath);
                if (emulatorDirName != null)
                {
                    var tmpPath = Path.Combine(emulatorDirName, "Configurations");
                    if (Directory.Exists(tmpPath))
                        Settings.Default.UAEConfigsPath = tmpPath;
                }
            }

            // If there's no Music player set in Preferences and Deliplayer is found installed, prompt the user to pick that directly.
            // Otherwise, default back to the bundled player
            if (string.IsNullOrEmpty(Settings.Default.MusicPlayerPath))
            {
                if (
                    Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                                  "Deliplayer2")))
                {
                    MessageBoxResult result =
                        MessageBox.Show(
                            "I found Deliplayer2 installed in your system.\n\nWould you like to use it for music playback instead of the bundled XMPlay?",
                            "Deliplayer2 found", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        Settings.Default.MusicPlayerPath =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                         "Deliplayer2\\DeliPlayer.exe");
                        SaveDefaultSettings();
                    }
                    else
                    {
                        Settings.Default.MusicPlayerPath = ".\\xmplay\\xmplay.exe";
                        SaveDefaultSettings();
                    }
                }
                else
                {
                    Settings.Default.MusicPlayerPath = ".\\xmplay\\xmplay.exe";
                    SaveDefaultSettings();
                }
            }

            // If GameBase Amiga folder is found in the default location (C:\GameBase\GameBase Amiga), use it automatically for Screenshots, Music, etc.
            if (string.IsNullOrEmpty(Settings.Default.ScreenshotsPath) ||
                (string.IsNullOrEmpty(Settings.Default.MusicPath)))
            {
                if (Directory.Exists("C:\\GameBase\\GameBase Amiga"))
                {
                    var result =
                        MessageBox.Show(
                            "I found GameBase installed in your system.\n\nWould you like to use the Screenshots and Music paths from it?",
                            "GameBase found", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        Settings.Default.ScreenshotsPath = "C:\\GameBase\\GameBase Amiga\\Screenshots";
                        Settings.Default.MusicPath = "C:\\GameBase\\GameBase Amiga\\Music";
                        SaveDefaultSettings();
                    }
                }
            }
            // Check if www.youtube.com is reachable, enable Longplay feature if it is, disable it otherwise
            Settings.Default.ShowLongplayVideos = RemoteFileExists("http://www.youtube.com");
        }

        private static void InsertDefaultValueInUaeConfigs()
        {
            _uaeConfigViewSource.Insert(0, "default");
        }

        private static BitmapImage LoadGameScreenshot(string gameImageFile)
        {
            // initialize a new image source
            var gameScreenshot = new BitmapImage();
            try
            {
                gameScreenshot.BeginInit();
                gameScreenshot.CacheOption = BitmapCacheOption.OnLoad;
                gameScreenshot.UriSource =
                    new Uri(Path.Combine(Settings.Default.ScreenshotsPath, gameImageFile));
                gameScreenshot.EndInit();

                return gameScreenshot;
            }

            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to display the game's image:\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return new BitmapImage();
        }

        /// <summary>
        ///     Search for the selected title in various websites
        /// </summary>
        /// <param name="currentgame">The currently selected title</param>
        /// <param name="urlSite">The website to lookup the title on, possible values are "HOL, "LemonAmiga"</param>
        private static void LookupUrl(object currentgame, string urlSite)
        {
            // Search for the selected game in various Amiga websites
            // Valid parameters for URLsite are:
            // HOL - search for the game in HOL
            // LemonAmiga - search for the game in LemonAmiga
            var gameTitleforUrl = CleanGameTitle(currentgame, "URL");
            if (string.IsNullOrEmpty(gameTitleforUrl)) return;
            switch (urlSite)
            {
                case "HOL":
                    {
                        const string targetUrl = @"http://hol.abime.net/hol_search.php?find=";
                        Process.Start(targetUrl + gameTitleforUrl);
                        break;
                    }
                case "LemonAmiga":
                    {
                        const string targetUrl = @"http://www.lemonamiga.com/games/list.php?list_letter=";
                        Process.Start(targetUrl + gameTitleforUrl);
                        break;
                    }
            }
        }

        /// <summary>
        ///     Play the current game's music if found, using the music player configured in Preferences
        /// </summary>
        /// <param name="currentgame">The currently selected game title</param>
        private static void PlayGameMusic(object currentgame)
        {
            // Display the music found for the selected game (if found)
            if (!string.IsNullOrEmpty(Settings.Default.MusicPlayerPath))
            {
                if (File.Exists(Settings.Default.MusicPlayerPath))
                {
                    // Need to check if file exists first
                    string gameMusicFile = CleanGameTitle(currentgame, "Screenshot")
                        .Replace("_", " ")
                        .Replace(".png", ".zip");
                    if (string.IsNullOrEmpty(gameMusicFile)) return;
                    // check if the filename exists first, otherwise there's nothing to do
                    if (!File.Exists(Path.Combine(Settings.Default.MusicPath, gameMusicFile))) return;
                    //MessageBox.Show("About to launch:\n" + Properties.Settings.Default.MusicPlayerPath + " " + "\"" + Properties.Settings.Default.MusicPath + "\\" + gameMusicFile +"\"");
                    // launch the Music Player to listen to it
                    try
                    {
                        Process.Start(Settings.Default.MusicPlayerPath,
                            "\"" + Path.Combine(Settings.Default.MusicPath, gameMusicFile) + "\"");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "An Exception has occured while trying to launch the music player with the music file:\n\n" +
                            ex.Message, "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                    MessageBox.Show(
                        "I couldn't find the specified music player:\n\n" + Settings.Default.MusicPlayerPath +
                        "\n\nplease check your path in Preferences!", "Music Player not found", MessageBoxButton.OK,
                        MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(
                    "Sorry, but you have no music player set-up in Preferences!\nWithout one, it's not possible to play the game's music...",
                    "Music Player not specified", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void ReadKeysFromRegistry(string rootKey, string[] afKeys)
        {
            var patRegistry = Registry.LocalMachine.OpenSubKey(rootKey);
            if (patRegistry != null)
                foreach (string subKeyName in patRegistry.GetSubKeyNames())
                {
                    //MessageBox.Show("Key: " + subKeyName.ToString() + "\nValue: ");
                    patRegistry = Registry.LocalMachine.OpenSubKey(rootKey + "\\" + subKeyName);
                    foreach (string afKey in afKeys)
                    {
                        //MessageBox.Show("Key: " + afKey.ToString() + "\nValue: " + patRegistry.GetValue(afKey).ToString());
                        if (patRegistry != null && (afKey == "AmigaFiles" && patRegistry.GetValue(afKey) != null))
                        {
                            Settings.Default.UAEConfigsPath = Path.Combine(patRegistry.GetValue(afKey).ToString(),
                                "WinUAE\\Configurations");
                            Settings.Default.Save();
                        }
                        if (patRegistry == null || (afKey != "Path" || patRegistry.GetValue(afKey) == null)) continue;
                        Settings.Default.EmulatorPath = Path.Combine(patRegistry.GetValue(afKey).ToString(),
                            "WinUAE\\winuae.exe");
                        Settings.Default.Save();
                    }
                }
            patRegistry?.Close();
        }

        /// <summary>
        ///     Scan the configs folder for UAE config files and populate the ViewSource for the combobox
        /// </summary>
        private static void RefreshUaEconfigs()
        {
            if (!string.IsNullOrEmpty(Settings.Default.UAEConfigsPath))
            {
                CheckIfUaeConfigsExist();
                CleanupUaeConfigContents();
                InsertDefaultValueInUaeConfigs();
            }
            else
                MessageBox.Show(
                    "It looks like the WinUAE Configurations folder could not be located in your system!\nPlease make sure you have one available at least under the WinUAE folder in Program Files.",
                    "No Configurations found", MessageBoxButton.OK, MessageBoxImage.Stop);
        }

        private static void SaveDefaultSettings()
        {
            Settings.Default.Save();
        }

        private static void SetProgressbarProperties()
        {
            ProgBar.Height = 15;
            ProgBar.Width = 100;
            ProgBar.IsIndeterminate = true;
        }

        private static void ShowMainWindow(Process runningProcess)
        {
            const int swShowmaximized = 3;
            SafeNativeMethods.ShowWindow(runningProcess.MainWindowHandle, swShowmaximized);
        }

        private static void UpdateDateLastPlayed(DataRowView oDataRowView)
        {
            oDataRowView.Row["DateLastPlayed"] = DateTime.Now;
        }

        private void AddGamesRow(string x, AmigulaDBDataSet.GenresRow gameGenre, AmigulaDBDataSet.PublishersRow gamePublisher)
        {
            try
            {
                // Check if the path to file already exists in the database, skip inserting it if it does
                if (_amigulaDbDataSetGamesTableAdapter.FileExists(x) != 0) return;
                if (_amigulaDbDataSet == null) return;
                if (x != null)
                    _amigulaDbDataSet.Games.AddGamesRow(
                        Regex.Replace(Path.GetFileNameWithoutExtension(x),
                            @"Disk\s(\d{1})\sof\s(\d{1})|Disk-(\d{1})|Disk(\d{1})$|Disk(\d{2})$|Disk[A-Za-z]$|-(\d{1})$|[\[(].+?[\])]|_",
                            ""), x, "default", IdentifyGameDisks(x).Count,
                        GetGameYear(x), 0, DateTime.Today, 0, gameGenre,
                        gamePublisher, "");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while entering the games in the database:\n\n" +
                    ex.Message, "An exception has occured", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddGenreInDatabase(string fetchedGenre)
        {
            try
            {
                _amigulaDbDataSetGenresTableAdapter.InsertGenre(fetchedGenre);
                _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to add a new Genre\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPublisherToDatabase(string fetchedPublisher)
        {
            try
            {
                _amigulaDbDataSetPublishersTableAdapter.InsertPublisher(fetchedPublisher);
                _amigulaDbDataSetPublishersTableAdapter.Fill(_amigulaDbDataSet.Publishers);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to add a new Publisher\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Delete the game's specified Screenshot
        /// </summary>
        /// <param name="p">The number identifying the screenshot to delete</param>
        private void DeleteGameScreenshot(int p)
        {
            var img = GetImagePath(p);
            if (!ConfirmUserAction(img)) return;

            EmptyImagePlaceholder(p);
            File.Delete(img);
        }

        /// <summary>
        /// Display Longplay videos for selected game from Youtube
        /// </summary>
        private void DisplayLongplay()
        {
            if (!Settings.Default.ShowLongplayVideos) return;
            // Load longplay video
            if (SelectedGameRowView == null) return;
            var longplayTitle = SelectedGameRowView.Row["Title"] as string;
            var videos = YoutubeHelper.LoadVideosKey("Amiga Longplay " + longplayTitle);
            if (videos == null || !videos.Any()) return;
            var video = new Uri(YoutubeHelper.GetEmbedUrlFromLink(videos[0].EmbedUrl), UriKind.Absolute);
            wbLongplay.Source = video;
        }

        private bool DoesGenreExistInDatabase(string fetchedGenre)
        {
            var genreExists = _amigulaDbDataSet.Genres.AsEnumerable()
                .Any(row => fetchedGenre == row.Field<string>("Genre_label"));
            return genreExists;
        }

        private bool DoesPublisherExistInDatabase(string fetchedPublisher)
        {
            bool publisherExists = _amigulaDbDataSet.Publishers.AsEnumerable()
                .Any(
                    row =>
                        fetchedPublisher == row.Field<string>("Publisher_Label"));
            return publisherExists;
        }

        /// <summary>
        /// Empties the image placeholder.
        /// </summary>
        /// <param name="p">The p.</param>
        private void EmptyImagePlaceholder(int p)
        {
            switch (p)
            {
                case 1:
                    imgScreenshot.Source = null;
                    break;

                case 2:
                    imgScreenshot2.Source = null;
                    break;

                case 3:
                    imgScreenshot3.Source = null;
                    break;
            }
        }

        /// <summary>
        ///     Fill the listview while respecting the Favorite options
        /// </summary>
        private void FillListView()
        {
            // Save the currently selected item so we can restore it after refreshing the listview
            var tmpSelectedValue = GamesListView.SelectedValue;
            if (viewMenu_Favorites_ShowOnly.IsChecked)
                PopulateGameListWithFavoritesOnly(tmpSelectedValue);
            else
                PopulateGameList(tmpSelectedValue);
        }

        /// <summary>
        ///     Filter the list of games in the interface based on the search pattern specified
        /// </summary>
        /// <param name="searchFilter">The search pattern to use</param>
        private void FilterListItems(string searchFilter)
        {
            if (searchFilter == "Search for Game") return;
            // Filter the list dynamically when the user enters something in the Filter textbox
            var cv = (BindingListCollectionView)CollectionViewSource.GetDefaultView(_amigulaDbDataSet.Games);
            try
            {
                cv.CustomFilter = string.Format(CultureInfo.InvariantCulture, "TITLE LIKE '%{0}%'",
                    searchFilter.Replace("'", "''"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception has occured while trying to filter the games:\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        private string GetImagePath(int p)
        {
            string img = "";
            try
            {
                switch (p)
                {
                    case 1:
                        img = new Uri(imgScreenshot.Source.ToString()).LocalPath;
                        break;

                    case 2:
                        img = new Uri(imgScreenshot2.Source.ToString()).LocalPath;
                        break;

                    case 3:
                        img = new Uri(imgScreenshot3.Source.ToString()).LocalPath;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception has occured while trying get the screenshot's path!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return img;
        }

        private void GetScreenshotsIfNeeded(HtmlDocument document, string gameTitle, HtmlWeb webGet, string gameurl)
        {
            // XPath for title image: /html/body/div[2]/table[2]/tr/tr/td/a/img
            if (imgScreenshot.Source == null)
            {
                //MessageBox.Show("No picture found in slot 1");
                try
                {
                    string remoteScreenshot = null;
                    if (document.DocumentNode.SelectSingleNode("//table[2]/tr/td/a/img") != null)
                    {
                        remoteScreenshot =
                            document.DocumentNode.SelectSingleNode("//table[2]/tr/td/a/img").Attributes[0].Value;
                    }
                    else if (document.DocumentNode.SelectSingleNode("//table[2]/tr/tr/td/a/img") != null)
                    {
                        remoteScreenshot =
                            document.DocumentNode.SelectSingleNode("//table[2]/tr/tr/td/a/img").Attributes[0]
                                .Value;
                    }
                    // Download the image locally to the user's Temp folder
                    if (!string.IsNullOrEmpty(remoteScreenshot))
                    {
                        var webClient = new WebClient();
                        webClient.DownloadFile(remoteScreenshot, Path.Combine(Path.GetTempPath(), "sshot.png"));
                        AddGameScreenshot(Path.Combine(Path.GetTempPath(), "sshot.png"), gameTitle);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "An exception has occured while trying to add a new Screenshot\n\n" + ex.Message,
                        "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (imgScreenshot2.Source == null || imgScreenshot3.Source == null)
            {
                try
                {
                    document = webGet.Load(gameurl + @"/screenshot");

                    if (imgScreenshot2.Source == null)
                    {
                        string remoteScreenshot = null;

                        if (
                            document.DocumentNode.SelectSingleNode(
                                "//div[@align='center']/table[5]/tr[2]/td/a/img") != null)
                        {
                            remoteScreenshot =
                                document.DocumentNode.SelectSingleNode(
                                    "//div[@align='center']/table[5]/tr[2]/td/a/img").Attributes[0].Value;
                        }
                        // Download the image locally to the user's Temp folder
                        if (!string.IsNullOrEmpty(remoteScreenshot))
                        {
                            var webClient = new WebClient();
                            webClient.DownloadFile(
                                remoteScreenshot.Replace("pic_preview", "pic_full").Replace(".jpg", ".png"),
                                Path.Combine(Path.GetTempPath(), "sshot.png"));
                            AddGameScreenshot(Path.Combine(Path.GetTempPath(), "sshot.png"), gameTitle);
                        }
                    }

                    if (imgScreenshot3.Source == null)
                    {
                        string remoteScreenshot = null;

                        if (
                            document.DocumentNode.SelectSingleNode(
                                "//div[@align='center']/table[5]/tr[2]/td[2]/a/img") != null)
                        {
                            remoteScreenshot =
                                document.DocumentNode.SelectSingleNode(
                                    "//div[@align='center']/table[5]/tr[2]/td[2]/a/img").Attributes[0].Value;
                        }
                        // Download the image locally to the user's Temp folder
                        if (!string.IsNullOrEmpty(remoteScreenshot))
                        {
                            var webClient = new WebClient();
                            webClient.DownloadFile(
                                remoteScreenshot.Replace("pic_preview", "pic_full").Replace(".jpg", ".png"),
                                Path.Combine(Path.GetTempPath(), "sshot.png"));
                            AddGameScreenshot(Path.Combine(Path.GetTempPath(), "sshot.png"), gameTitle);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "An exception has occured while trying to add a new Screenshot\n\n" + ex.Message,
                        "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        ///     Launch the currently selected game in WinUAE
        /// </summary>
        private void LaunchInUae()
        {
            if (string.IsNullOrEmpty(Settings.Default.EmulatorPath))
            {
                // if there WinUAE path is empty, then display a warning message and allow the user to select it now
                AppNotDefined(
                    "There is no emulator defined in the preferences!\nWithout one, you can't run any games.\n\nDo you want to select the path to one now?",
                    "Emulator");
            }
            else
            {
                var gamePath = CleanGameTitle(GamesListView.SelectedItem, "Path");
                if (string.IsNullOrEmpty(gamePath) == false)
                    LaunchUaeWithConfigAndGame(gamePath);
                else
                    MessageBox.Show(
                        "Sorry, the selected game\n" + gamePath +
                        " \nwas not found!\nPlease check the path and verify the file actually exists there!",
                        "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchUaeWithConfigAndGame(string gamePath)
        {
            // Launch WinUAE from selected path giving it the selected config and game as a parameter
            Process.Start(Settings.Default.EmulatorPath, gamePath);
            var oDataRowView = SelectedGameRowView;
            if (oDataRowView == null) return;
            IncreaseTimesPlayedCounter(oDataRowView);
            UpdateDateLastPlayed(oDataRowView);
            UpdateTimesPlayedAndDateLastPlayedInTable(oDataRowView);
        }

        /// <summary>
        ///     Mark a game as Favorite
        /// </summary>
        private void MarkAsFavorite()
        {
            var oDataRowView = GamesListView.SelectedItem as DataRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                if (oDataRowView != null)
                {
                    oDataRowView.Row["Favorite"] = 1;
                    _amigulaDbDataSetGamesTableAdapter.UpdateFavoriteStatus(1, oDataRowView.Row["Title"] as string);
                }
                FillListView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to set this game as a Favorite!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateGameList(object tmpSelectedValue)
        {
            if (viewMenu_Show_GameTitles.IsChecked)
                _amigulaDbDataSetGamesTableAdapter.FillByTitle(_amigulaDbDataSet.Games);
            else
                _amigulaDbDataSetGamesTableAdapter.FillByAllFiles(_amigulaDbDataSet.Games);
            _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
            _amigulaDbDataSetPublishersTableAdapter.Fill(_amigulaDbDataSet.Publishers);
            GamesListView.SelectedValue = tmpSelectedValue;
        }

        private void PopulateGameListWithFavoritesOnly(object tmpSelectedValue)
        {
            if (viewMenu_Show_GameTitles.IsChecked)
                _amigulaDbDataSetGamesTableAdapter.FillByTitleFavoritesOnly(_amigulaDbDataSet.Games);
            else
                _amigulaDbDataSetGamesTableAdapter.FillByAllFilesFavoritesOnly(_amigulaDbDataSet.Games);
            _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
            _amigulaDbDataSetPublishersTableAdapter.Fill(_amigulaDbDataSet.Publishers);
            GamesListView.SelectedValue = tmpSelectedValue;
        }

        /// <summary>
        ///     Process all files (recursively) in the directory specified for any games and add them to the database
        /// </summary>
        /// <param name="targetDirectory">The target directory to scan for games</param>
        private void ProcessDirectory(string targetDirectory)
        {
            // Process all files in the directory passed in, recurse on any directories
            // that are found, and process the files they contain.

            // Empty current DataSet to avoid duplicate entries
            _amigulaDbDataSet.Clear();

            // Process the list of files found in the directory.
            var extensions = ValidFilenameExtensions;

            SetProgressbarProperties();
            statusBar.Items.Add(ProgBar);

            // Show the Cancel button to allow the user to abort the process
            btnCancel.Visibility = Visibility.Visible;

            var cancelFileScanning = CancelClicked;

            var gameGenre = _amigulaDbDataSet.Genres.FirstOrDefault();
            var gamePublisher = _amigulaDbDataSet.Publishers.FirstOrDefault();

            // ReSharper disable once UnusedVariable
            IDisposable files = Directory.EnumerateFiles(targetDirectory, "*.*", SearchOption.AllDirectories)
                                         .Where(s => extensions.Contains(Path.GetExtension(s)))
                                         .ToObservable(TaskPoolScheduler.Default)
                                         .TakeUntil(cancelFileScanning)
                                         .Do(x =>
                                             {
                                                 AddGamesRow(x, gameGenre, gamePublisher);
                                             })
                                         .TakeLast(1)
                                         .Do(_ =>
                                             {
                                                 UpdateGamesDatabase();
                                             })
                                         .ObserveOnDispatcher()
                                         .Subscribe(y => { },
                                                    () =>
                                                    {
                                                        statusBar.Items.Remove(ProgBar);
                                                        btnCancel.Visibility = Visibility.Collapsed;
                                                        FillListView();
                                                    });
        }

        /// <summary>
        ///     Remove current game from the Database
        /// </summary>
        private void RemoveGameFromDb()
        {
            var oDataRowView = GamesListView.SelectedItem as DataRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                if (viewMenu_Show_GameTitles.IsChecked)
                {
                    if (oDataRowView != null)
                        _amigulaDbDataSetGamesTableAdapter.DeleteBasedOnTitle(oDataRowView.Row["Title"] as string);
                }
                else
                {
                    if (oDataRowView != null)
                        _amigulaDbDataSetGamesTableAdapter.DeleteQuery((long)oDataRowView.Row["ID"],
                            oDataRowView["PathToFile"] as string);
                }
                FillListView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to remove this game from the Database!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveFetchedInformationInDatabase(string fetchedGenre, string fetchedPublisher, string fetchedYear,
                    DataRowView oDataRowView)
        {
            try
            {
                // get the ID for the Genre label
                EnumerableRowCollection<int> genreId = from row in _amigulaDbDataSet.Genres.AsEnumerable()
                                                       where
                                                           row.Field<string>("Genre_Label") == fetchedGenre
                                                       select row.Field<int>("Genre_ID");

                // get the ID for the Publisher label
                EnumerableRowCollection<int> publisherId =
                    from row in _amigulaDbDataSet.Publishers.AsEnumerable()
                    where row.Field<string>("Publisher_Label") == fetchedPublisher
                    select row.Field<int>("Publisher_ID");

                _amigulaDbDataSetGamesTableAdapter.UpdateMetadata(genreId.First(), int.Parse(fetchedYear),
                    publisherId.First(),
                    oDataRowView.Row["Notes"] as string,
                    oDataRowView.Row["Title"] as string);
                FillListView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to update the metadata!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDefaultScreenshotPlaceholders()
        {
            imgScreenshot.Source =
                new BitmapImage(
                    new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"images\Screenshot_placeholder.png")));
            imgScreenshot2.Source =
                new BitmapImage(
                    new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"images\Screenshot_placeholder.png")));
            imgScreenshot3.Source =
                new BitmapImage(
                    new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"images\Screenshot_placeholder.png")));
        }

        /// <summary>
        ///     Show the game's media files (such as screenshots) in the interface's placeholders
        /// </summary>
        /// <param name="currentgame">The currently selected game title</param>
        private void ShowGameMedia(object currentgame)
        {
            // Display the screenshot for the selected game
            const int opacity = 1;
            const double defaultOpacity = 0.25;

            if (!string.IsNullOrEmpty(Settings.Default.ScreenshotsPath))
            {
                // call cleanGameTitle to cleanup the title and add the png extension to it
                var gameImageFile = CleanGameTitle(currentgame, "Screenshot");

                imgScreenshot.Opacity = defaultOpacity;
                imgScreenshot2.Opacity = defaultOpacity;
                imgScreenshot3.Opacity = defaultOpacity;

                ShowDefaultScreenshotPlaceholders();
                if (!string.IsNullOrEmpty(gameImageFile))
                {
                    // check if the filename exists first, otherwise there's nothing to display
                    if (File.Exists(Path.Combine(Settings.Default.ScreenshotsPath, gameImageFile)))
                    {
                        var gameScreenshot = LoadGameScreenshot(gameImageFile);

                        // assign our image source to the placeholder
                        imgScreenshot.Source = gameScreenshot;
                        imgScreenshot.Opacity = opacity;
                        // resize the container
                        //gridImgContainer.Height = 256;
                    }

                    // check if the filename exists first, otherwise there's nothing to display
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "_1.png"))))
                    {
                        var gameScreenshot = LoadGameScreenshot(gameImageFile.Replace(".png", "_1.png"));

                        // assign our image source to the placeholder
                        imgScreenshot2.Source = gameScreenshot;
                        imgScreenshot2.Opacity = opacity;
                        // resize the container
                        //gridImgContainer.Height = 512;
                    }

                    // check if the filename exists first, otherwise there's nothing to display
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "_2.png"))))
                    {
                        var gameScreenshot = LoadGameScreenshot(gameImageFile.Replace(".png", "_2.png"));

                        // assign our image source to the placeholder
                        imgScreenshot3.Source = gameScreenshot;
                        imgScreenshot3.Opacity = opacity;
                        // resize the container
                        //gridImgContainer.Height = 768;
                    }

                    // fix for some filenames ending with "_.png" in GameBase!
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "_.png"))))
                    {
                        // initialize a new image source
                        var gameScreenshot = LoadGameScreenshot(gameImageFile.Replace(".png", "_.png"));

                        // assign our image source to the placeholder
                        imgScreenshot.Source = gameScreenshot;
                        imgScreenshot.Opacity = opacity;
                        // resize the container
                        //gridImgContainer.Height = 256;
                    }

                    // fix for some filenames ending with "_.png" in GameBase!
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "__1.png"))))
                    {
                        // initialize a new image source
                        var gameScreenshot = LoadGameScreenshot(gameImageFile.Replace(".png", "__1.png"));

                        // assign our image source to the placeholder
                        imgScreenshot2.Source = gameScreenshot;
                        imgScreenshot2.Opacity = opacity;
                        // resize the container
                        //gridImgContainer.Height = 512;
                    }

                    // fix for some filenames ending with "_.png" in GameBase!
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "__2.png"))))
                    {
                        var gameScreenshot = LoadGameScreenshot(gameImageFile.Replace(".png", "__2.png"));

                        // assign our image source to the placeholder
                        imgScreenshot3.Source = gameScreenshot;
                        imgScreenshot3.Opacity = opacity;
                        // resize the container
                        //gridImgContainer.Height = 768;
                    }
                }
            }
            // for music files, replace the underscores with spaces and the extension with .ZIP
            // this area should be improved!
            // Display the screenshot for the selected game
            if (!string.IsNullOrEmpty(Settings.Default.MusicPath))
            {
                // call cleanGameTitle to cleanup the title and add the png extension to it
                string gameMusicFile = CleanGameTitle(currentgame, "Screenshot")
                    .Replace("_", " ")
                    .Replace(".png", ".zip");
                if (!string.IsNullOrEmpty(gameMusicFile))
                {
                    // check if the filename exists first, otherwise there's nothing to do
                    if (File.Exists(Path.Combine(Settings.Default.MusicPath, gameMusicFile)))
                    {
                        btnPlayMusic.IsEnabled = true;
                        btnPlayMusic.Opacity = opacity;
                    }
                    else
                    {
                        btnPlayMusic.IsEnabled = false;
                        btnPlayMusic.Opacity = defaultOpacity;
                    }
                }
            }

            // If the Longplay tab is selected when the selected game changes, load the new video
            if (tabLongplay.IsSelected)
            {
                DisplayLongplay();
            }
        }

        /// <summary>
        ///     Unmark a game from Favorites
        /// </summary>
        private void UnmarkFromFavorites()
        {
            var oDataRowView = GamesListView.SelectedItem as DataRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                if (oDataRowView != null)
                {
                    oDataRowView.Row["Favorite"] = 0;
                    _amigulaDbDataSetGamesTableAdapter.UpdateFavoriteStatus(0, oDataRowView.Row["Title"] as string);
                }
                FillListView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to set this game as a Favorite!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Update selected Game metadata from Hall of Light website
        /// </summary>
        /// <param name="currentgame"></param>
        private void UpdateGameMetadata(object currentgame)
        {
            if (GamesListView.SelectedIndex <= -1) return;

            const string targetUrl = @"http://hol.abime.net/hol_search.php?find=";
            string gameTitleforUrl = CleanGameTitle(currentgame, "URL");
            var oDataRowView = currentgame as DataRowView;

            // open a web connection to HOL, get all the links for the selected title in order to find the game's unique ID
            var webGet = new HtmlWeb();
            HtmlDocument document;

            string gamelink = null;
            string gameTitle = null;

            bool tryagain = true;
            while (tryagain)
            {
                document = webGet.Load(targetUrl + gameTitleforUrl);
                var linksOnPage = from lnks in document.DocumentNode.Descendants()
                                  where lnks.Name == "a" &&
                                        lnks.Attributes["href"] != null &&
                                        lnks.InnerText.Trim().Length > 0
                                  select new
                                  {
                                      Url = lnks.Attributes["href"].Value,
                                      Text = lnks.InnerText
                                  };
                // Now we have to check which of the parsed links contains the link to the game's unique ID page
                // It should look like the following example, for "SWIV":
                // { Url = "http://hol.abime.net/2240", Text = "SWIV" }

                // Since our title still has "%20" instead of spaces, we need to replace those
                gameTitle = gameTitleforUrl.Replace("%20", " ").TrimEnd(' ');

                try
                {
                    gamelink =
                        linksOnPage.First(
                            entry =>
                                entry.ToString().IndexOf("Text = " + gameTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ToString();
                    tryagain = false;
                }
                // if the title was not found in the search at all, we need to handle this.
                // Display a message to the user, allow them to refine the title searched with another one and try again
                catch (Exception)
                {
                    var inputBoxDialog = new InputBox(ref gameTitle);
                    bool? result = inputBoxDialog.ShowDialog();
                    //MessageBox.Show("The result received was: " + result.ToString() + "\nThe current text is: " + inputBoxDialog.TextValue);
                    gameTitle = inputBoxDialog.TextValue;
                    if (result == true)
                    {
                        tryagain = true;
                        gameTitleforUrl = gameTitle.Replace(" ", "%20");
                    }
                    else tryagain = false;
                }
            }

            if (gamelink == null) return;

            var gameurl = GetGameUrl(gamelink);
            document = webGet.Load(gameurl);

            var fetchedYear = GetFetchedYear(document);
            var fetchedPublisher = GetFetchedPublisher(document);

            var publisherExists = DoesPublisherExistInDatabase(fetchedPublisher);
            if (!publisherExists)
            {
                AddPublisherToDatabase(fetchedPublisher);
            }

            var fetchedGenre = GetFetchedGenre(document);

            var genreExists = DoesGenreExistInDatabase(fetchedGenre);
            if (!genreExists)
            {
                AddGenreInDatabase(fetchedGenre);
            }

            GetScreenshotsIfNeeded(document, gameTitle, webGet, gameurl);

            // Save the fetched information in the database
            if (GamesListView.SelectedIndex <= -1) return;
            SaveFetchedInformationInDatabase(fetchedGenre, fetchedPublisher, fetchedYear, oDataRowView);
        }

        private void UpdateGamesDatabase()
        {
            try
            {
                _amigulaDbDataSetGamesTableAdapter.Update(_amigulaDbDataSet.Games);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to save the changes in the database:\n\n" +
                    ex.Message, "An exception has occured", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Updates the Status Bar test with the total number of games shown
        /// </summary>
        private void UpdateNoOfGames()
        {
            // if the game Listview is not empty, count the entries and display the number in the statusbar
            txtStatusText.Text = GamesListView.Items.Count.ToString(CultureInfo.CurrentCulture) + " games found";
        }

        private void UpdateTimesPlayedAndDateLastPlayedInTable(DataRowView oDataRowView)
        {
            try
            {
                _amigulaDbDataSetGamesTableAdapter.UpdateTimesPlayed((int)oDataRowView.Row["TimesPlayed"],
                    (DateTime)
                        oDataRowView.Row["DateLastPlayed"],
                    oDataRowView.Row["Title"] as string);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Sorry, an exception has occured while trying to update the database statistics!\n\n" +
                    ex.Message);
            }
        }

        #endregion Functions

        #region Events

        private static void SaveGameScreenshot(DataRowView oDataRowView, string[] files)
        {
            try
            {
                if (oDataRowView != null)
                {
                    var gameTitle = oDataRowView.Row["Title"] as string;
                    foreach (string file in files)
                    {
                        AddGameScreenshot(file, gameTitle);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to set this game as a Favorite!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Save selected Genre value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbboxGenre_DropDownClosed(object sender, EventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            SaveGameGenre();
        }

        /// <summary>
        ///     Save the contents of the Publisher combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbboxPublisher_DropDownClosed(object sender, EventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            SaveGamePublisher();
        }

        /// <summary>
        ///     Save selected value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboUAEconfig_DropDownClosed(object sender, EventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            SaveSelectedUaeConfig();
        }

        /// <summary>
        ///     When the DropDown is opened, it should be populated with all the available UAE configurations in the "config" folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboUAEconfig_DropDownOpened(object sender, EventArgs e)
        {
            RefreshUaEconfigs();
        }

        /// <summary>
        ///     Handle the DoubleClick event in the ListView (usually launch the selected game)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gamesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LaunchInUae();
        }

        private void GetCurrentWindowSettings()
        {
            Settings.Default.Top = Top;
            Settings.Default.Left = Left;
            Settings.Default.Height = Height;
            Settings.Default.Width = Width;
            Settings.Default.WindowSetting = WindowState;
        }

        private void imgScreenshot_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        /// <summary>
        ///     Drag and drop files on image placeholder 1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void imgScreenshot_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            // Note that you can have more than one file.
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            var oDataRowView = SelectedGameRowView;

            if (GamesListView.SelectedIndex <= -1) return;

            SaveGameScreenshot(oDataRowView, files);
            ShowGameMedia(GamesListView.SelectedItem);
        }

        private void imgScreenshot2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        /// <summary>
        ///     Drag and drop files on image placeholder 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void imgScreenshot2_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            // Note that you can have more than one file.
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            var oDataRowView = SelectedGameRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            SaveGameScreenshot(oDataRowView, files);
            ShowGameMedia(GamesListView.SelectedItem);
        }

        private void imgScreenshot3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        /// <summary>
        ///     Drag and drop files on image placeholder 3
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void imgScreenshot3_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            // Note that you can have more than one file.
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            var oDataRowView = SelectedGameRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            SaveGameScreenshot(oDataRowView, files);
            ShowGameMedia(GamesListView.SelectedItem);
        }

        /// <summary>
        ///     Set selected game as Favorite
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemFavorite_Click(object sender, RoutedEventArgs e)
        {
            MarkAsFavorite();
        }

        /// <summary>
        ///     Launch selected game in WinUAE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemLaunchInWinUAE_Click(object sender, RoutedEventArgs e)
        {
            LaunchInUae();
        }

        /// <summary>
        ///     Remove a game from the database based on Title
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemRemoveGame_Click(object sender, RoutedEventArgs e)
        {
            RemoveGameFromDb();
        }

        /// <summary>
        ///     Open selected game's containing folder in Explorer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemShowInExplorer_Click(object sender, RoutedEventArgs e)
        {
            var oDataRowView = SelectedGameRowView;
            var pathToFile = oDataRowView?.Row?["PathToFile"] as string;
            var dirNameForFile = Path.GetDirectoryName(pathToFile);
            if (pathToFile == null) return;
            if (dirNameForFile != null) Process.Start(dirNameForFile);
        }

        /// <summary>
        ///     Remove a game from the Favorites
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemUnmarkFromFavorites_Click(object sender, RoutedEventArgs e)
        {
            UnmarkFromFavorites();
        }

        /// <summary>
        ///     Save the window position and state before closing down the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_Closing(object sender, CancelEventArgs e)
        {
            GetCurrentWindowSettings();
            SaveDefaultSettings();
        }

        private void SaveGameGenre()
        {
            try
            {
                _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Genre_ID = (int)cmbboxGenre.SelectedValue;
                _amigulaDbDataSetGamesTableAdapter.UpdateGenre(
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Genre_ID,
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to save the selected genre!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveGameNotes()
        {
            try
            {
                _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Notes = tboxNotes.Text;
                _amigulaDbDataSetGamesTableAdapter.UpdateNotes(
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Notes,
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception has occured while trying to save the Notes!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveGamePublisher()
        {
            try
            {
                _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Publisher_ID =
                    (int)cmbboxPublisher.SelectedValue;
                _amigulaDbDataSetGamesTableAdapter.UpdatePublisher(
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Publisher_ID,
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to save the selected Publisher!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveGameYear()
        {
            try
            {
                _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Year = int.Parse(tboxYear.Text);
                _amigulaDbDataSetGamesTableAdapter.UpdateYear(
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Year,
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception has occured while trying to save the Year!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSelectedUaeConfig()
        {
            try
            {
                _amigulaDbDataSet.Games[GamesListView.SelectedIndex].UAEconfig =
                    comboUAEconfig.SelectedValue.ToString();
                _amigulaDbDataSetGamesTableAdapter.UpdateUAEconfig(
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].UAEconfig,
                    _amigulaDbDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to save the selected UAE config!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// The tab Longplay is clicked, so the Youtube video should be loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabLongplay_Clicked(object sender, MouseButtonEventArgs e)
        {
            DisplayLongplay();
        }

        /// <summary>
        ///     Clear the textbox if the user clicks on it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tboxFilterGames_GotFocus(object sender, RoutedEventArgs e)
        {
            // Clear the textbox only if the user hadn't already typed something there
            if (tboxFilterGames.Text != "Search for Game") return;
            tboxFilterGames.Clear();
            tboxFilterGames.Foreground.Opacity = 1;
            tboxFilterGames.FontStyle = FontStyles.Normal;
        }

        /// <summary>
        ///     Restore the textbox if the user clicks away from it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tboxFilterGames_LostFocus(object sender, RoutedEventArgs e)
        {
            // Only if the user hasn't typed anything there, or the textbox is empty
            if (!string.IsNullOrEmpty(tboxFilterGames.Text)) return;
            tboxFilterGames.Text = "Search for Game";
            tboxFilterGames.Foreground.Opacity = 0.5;
            tboxFilterGames.FontStyle = FontStyles.Italic;
        }

        /// <summary>
        ///     Save the contents of the Notes textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tboxNotes_LostFocus(object sender, RoutedEventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            SaveGameNotes();
        }

        /// <summary>
        ///     Save the current game's Year of release
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tboxYear_LostFocus(object sender, RoutedEventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            SaveGameYear();
        }

        #endregion Events

        #region Buttons

        /// <summary>
        ///     Cancels a scanning operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // no action necessary here, this is monitored as an Observable
        }

        /// <summary>
        ///     Edit selected UAE configuration in WinUAE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEditUAEconfig_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(comboUAEconfig.SelectedValue.ToString())) return;
            if (comboUAEconfig.SelectedValue.ToString() == "default")
            {
                try
                {
                    Process.Start(Path.Combine(Environment.CurrentDirectory,
                        "configs\\" + comboUAEconfig.SelectedValue + ".uae"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "An exception has occured while trying to launch WinUAE!\nPlease check your paths in Preferences!\n\n" +
                        ex.Message, "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                try
                {
                    Process.Start(
                        Path.Combine(Settings.Default.UAEConfigsPath, comboUAEconfig.SelectedValue.ToString()) +
                        ".uae");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "An exception has occured while trying to launch WinUAE!\nPlease check your paths in Preferences!\n\n" +
                        ex.Message, "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        ///     Update the selected game's metadata from the web
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFetch_Click(object sender, RoutedEventArgs e)
        {
            UpdateGameMetadata(GamesListView.SelectedItem);
        }

        /// <summary>
        ///     Search for the selected game title in Amiga Hall of Light website
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHOLsearch_Click(object sender, RoutedEventArgs e)
        {
            // Search for the selected game in HOL
            LookupUrl(GamesListView.SelectedItem, "HOL");
        }

        /// <summary>
        ///     Search for the selected game title in LemonAmiga website
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLemonAmiga_Click(object sender, RoutedEventArgs e)
        {
            // Search for the selected game in LemonAmiga
            //http://www.lemonamiga.com/games/list.php?list_letter=
            LookupUrl(GamesListView.SelectedItem, "LemonAmiga");
        }

        /// <summary>
        ///     Open WinUAE to edit a new configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNewUAEconfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Settings.Default.EmulatorPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to launch WinUAE!\nPlease check your paths in Preferences!\n\n" +
                    ex.Message, "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Play game's music
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPlayMusic_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.Default.MusicPlayerPath))
            {
                PlayGameMusic(GamesListView.SelectedItem);
            }
            else
                AppNotDefined(
                    "There is no music player defined in the preferences!\nWithout one, you can't listen to the game music.\n\nDo you want to select the path to one now?",
                    "MusicPlayer");
        }

        /// <summary>
        ///     Select a random game from the list and scroll down to it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRandom_Click(object sender, RoutedEventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            var rand = new Random();
            GamesListView.SelectedIndex = rand.Next(1, GamesListView.Items.Count);
            GamesListView.ScrollIntoView(GamesListView.SelectedItem);
        }

        /// <summary>
        ///     Favorite toggle checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkbxMarkedFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (chkbxMarkedFavorite.IsChecked == true)
                MarkAsFavorite();
            else
                UnmarkFromFavorites();
        }

        #endregion Buttons

        #region MenuItems

        /// <summary>
        ///     Delete the first screenshot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteScreenshot1_Click(object sender, RoutedEventArgs e)
        {
            DeleteGameScreenshot(1);
        }

        /// <summary>
        ///     Delete the second screenshot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteScreenshot2_Click(object sender, RoutedEventArgs e)
        {
            DeleteGameScreenshot(2);
        }

        /// <summary>
        ///     Delete the third screenshot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteScreenshot3_Click(object sender, RoutedEventArgs e)
        {
            DeleteGameScreenshot(3);
        }

        /// <summary>
        ///     Empty the games database completely
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editMenu_EmptyLib_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show("Are you sure? This will DELETE all the entries from your database\n\n", "Please confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            // Empty the games library DataSet and Database
            _amigulaDbDataSet.Clear();
            try
            {
                _amigulaDbDataSetGamesTableAdapter.DeleteAllQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception has occured while trying to empty the database:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Show the preferences Window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editMenu_Prefs_Click(object sender, RoutedEventArgs e)
        {
            var prefs = new Preferences();
            prefs.Show();
        }

        /// <summary>
        ///     (Re)scan the games directory and refresh the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editMenu_RescanGames_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.Default.LibraryPath) == false)
                try
                {
                    // Call the ProcessDirectory method to handle the selected path
                    ProcessDirectory(Settings.Default.LibraryPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An exception has occured while trying to scan the games folder!\n\n" + ex.Message,
                                    "An exception has occured!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            else
            {
                MessageBoxResult result =
                    MessageBox.Show(
                        "You don't have a Games Folder selected in Preferences!\nDo you want to select one now?",
                        "No Games Folder found", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
                var prefs = new Preferences();
                prefs.Show();
            }
        }

        /// <summary>
        ///     Close the application after saving window position and state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileMenu_Close_Click(object sender, RoutedEventArgs e)
        {
            GetCurrentWindowSettings();
            SaveDefaultSettings();
            Close();
        }

        /// <summary>
        ///     Launch a game in WinUAE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileMenu_LaunchInWinUAE_Click(object sender, RoutedEventArgs e)
        {
            LaunchInUae();
        }

        /// <summary>
        ///     Remove selected Game from the Database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileMenu_RemoveGame_Click(object sender, RoutedEventArgs e)
        {
            RemoveGameFromDb();
        }

        /// <summary>
        ///     Display the About information window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpMenu_About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new aboutWindow();
            aboutWindow.ShowDialog();
        }

        /// <summary>
        ///     Open a web browser to the Support page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpMenu_Support_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"http://www.blitterstudio.com/forums/forum/support");
        }

        /// <summary>
        ///     Open a web browser to the official website
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpMenu_VisitWebsite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"http://www.blitterstudio.com/amigula");
        }

        private void viewMenu_Favorites_ShowOnly_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Favorites_ShowOnly.IsChecked = true;
            viewMenu_Favorites_ShowOnTop.IsChecked = false;
            FillListView();
        }

        private void viewMenu_Favorites_ShowOnTop_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Favorites_ShowOnly.IsChecked = false;
            viewMenu_Favorites_ShowOnTop.IsChecked = true;
            FillListView();
        }

        /// <summary>
        ///     Show all files found (even duplicate titles)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewMenu_Show_AllFiles_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.ShowTitlesOption = false;
            viewMenu_Show_GameTitles.IsChecked = false;
            Settings.Default.Save();
            _amigulaDbDataSetGamesTableAdapter.FillByAllFiles(_amigulaDbDataSet.Games);
            _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
        }

        /// <summary>
        ///     Show only game titles (unique)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewMenu_Show_GameTitles_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.ShowTitlesOption = true;
            viewMenu_Show_AllFiles.IsChecked = false;
            Settings.Default.Save();
            _amigulaDbDataSetGamesTableAdapter.FillByTitle(_amigulaDbDataSet.Games);
            _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
        }

        /// <summary>
        ///     Sort games by Most Played status, respect the Show All Files/Unique titles option
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewMenu_Statistics_MostPlayed_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Statistics_None.IsChecked = false;
            viewMenu_Statistics_MostPlayed.IsChecked = true;
            viewMenu_Statistics_NeverPlayed.IsChecked = false;
            viewMenu_Statistics_RecentlyPlayed.IsChecked = false;
            if (viewMenu_Show_GameTitles.IsChecked)
                _amigulaDbDataSetGamesTableAdapter.FillByTitleMostPlayed(_amigulaDbDataSet.Games);
            else
                _amigulaDbDataSetGamesTableAdapter.FillByAllFilesMostPlayed(_amigulaDbDataSet.Games);
            _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
        }

        private void viewMenu_Statistics_NeverPlayed_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Statistics_None.IsChecked = false;
            viewMenu_Statistics_MostPlayed.IsChecked = false;
            viewMenu_Statistics_NeverPlayed.IsChecked = true;
            viewMenu_Statistics_RecentlyPlayed.IsChecked = false;
            if (viewMenu_Show_GameTitles.IsChecked)
                _amigulaDbDataSetGamesTableAdapter.FillByTitleNeverPlayed(_amigulaDbDataSet.Games);
            else
                _amigulaDbDataSetGamesTableAdapter.FillByAllFilesNeverPlayed(_amigulaDbDataSet.Games);
            _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
        }

        private void viewMenu_Statistics_None_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Statistics_None.IsChecked = true;
            viewMenu_Statistics_MostPlayed.IsChecked = false;
            viewMenu_Statistics_NeverPlayed.IsChecked = false;
            viewMenu_Statistics_RecentlyPlayed.IsChecked = false;
            if (viewMenu_Show_GameTitles.IsChecked)
                _amigulaDbDataSetGamesTableAdapter.FillByTitle(_amigulaDbDataSet.Games);
            else
                _amigulaDbDataSetGamesTableAdapter.FillByAllFiles(_amigulaDbDataSet.Games);
            _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
        }

        private void viewMenu_Statistics_RecentlyPlayed_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Statistics_None.IsChecked = false;
            viewMenu_Statistics_MostPlayed.IsChecked = false;
            viewMenu_Statistics_NeverPlayed.IsChecked = false;
            viewMenu_Statistics_RecentlyPlayed.IsChecked = true;
            if (viewMenu_Show_GameTitles.IsChecked)
                _amigulaDbDataSetGamesTableAdapter.FillByTitleRecentlyPlayed(_amigulaDbDataSet.Games);
            else
                _amigulaDbDataSetGamesTableAdapter.FillByAllFilesRecentlyPlayed(_amigulaDbDataSet.Games);
            _amigulaDbDataSetGenresTableAdapter.Fill(_amigulaDbDataSet.Genres);
        }

        #endregion MenuItems
    }

    internal static class SafeNativeMethods
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string moduleName);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule,
                                                     [MarshalAs(UnmanagedType.LPWStr)] string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}