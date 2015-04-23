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
using System.Xml.Linq;
using Amigula.AmigulaDBDataSetTableAdapters;
using Amigula.Properties;
using HtmlAgilityPack;
using Microsoft.Win32;

[assembly: CLSCompliant(true)]

namespace Amigula
{
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
        internal static extern Boolean ShowWindow(IntPtr hWnd, Int32 nCmdShow);
    }

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow
    {
        // set up some variables globally since we'll use them from several areas
        private static List<string> UAEconfigViewSource;
        // Initialize our progress bar
        private static readonly ProgressBar progBar = new ProgressBar();
        private readonly AmigulaDBDataSet AmigulaDBDataSet;
        private readonly GamesTableAdapter AmigulaDBDataSetGamesTableAdapter = new GamesTableAdapter();
        private readonly GenresTableAdapter AmigulaDBDataSetGenresTableAdapter = new GenresTableAdapter();
        private readonly PublishersTableAdapter AmigulaDBDataSetPublishersTableAdapter = new PublishersTableAdapter();
        private readonly CollectionViewSource gamesViewSource;

        // A list to hold the UAE configs found

        public MainWindow()
        {
            if (!EnsureSingleInstance())
            {
                Close();
                return;
            }

            // initialize the Window position and state
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

            // if no saved position is found, default back to the center of the screen
// ReSharper disable CompareOfFloatsByEqualityOperator
            if (Top == 0 && Left == 0)
// ReSharper restore CompareOfFloatsByEqualityOperator
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // show the version information on the window titlebar
            Title = Assembly.GetExecutingAssembly().GetName().Name + " v" +
                    Assembly.GetExecutingAssembly().GetName().Version;

            InitializeComponent();

            // initialize the DataSet and ViewSource for the Listview
            AmigulaDBDataSet = ((AmigulaDBDataSet) (FindResource("AmigulaDBDataSet")));
            gamesViewSource = ((CollectionViewSource) (FindResource("GamesViewSource")));

            // Perform initial checks and configuration validation
            initialChecks();

            // Retrieve the list of available UAE configs and fill the list with them
            // so that the combobox displays them.
            refreshUAEconfigs();
            try
            {
                comboUAEconfig.ItemsSource = UAEconfigViewSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to populate the combobox with the UAE config files:\n\n" +
                    ex.Message, "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Load data into the Games table
            try
            {
                if (Settings.Default.ShowTitlesOption)
                {
                    AmigulaDBDataSetGamesTableAdapter.FillByTitle(AmigulaDBDataSet.Games);
                    viewMenu_Show_GameTitles.IsChecked = true;
                    viewMenu_Show_AllFiles.IsChecked = false;
                }
                else
                {
                    AmigulaDBDataSetGamesTableAdapter.FillByAllFiles(AmigulaDBDataSet.Games);
                    viewMenu_Show_AllFiles.IsChecked = true;
                    viewMenu_Show_GameTitles.IsChecked = false;
                }
                AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
                AmigulaDBDataSetPublishersTableAdapter.Fill(AmigulaDBDataSet.Publishers);
                // move the view to the first item in the list and show any media for it
                gamesViewSource.View.MoveCurrentToFirst();
                showGameMedia(GamesListView.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception occured while trying to read from the database:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Set the search field as observable for live filtering
            // When text is changed, call filterListItems() asynchronously

#pragma warning disable 168
            IDisposable gameFilterChanged = Observable.FromEventPattern<EventArgs>(tboxFilterGames, "TextChanged")
#pragma warning restore 168

                                                      .Select(searched => ((TextBox) searched.Sender).Text)
                                                      .DistinctUntilChanged()
                                                      .Throttle(TimeSpan.FromMilliseconds(250))
                                                      .ObserveOn(tboxFilterGames)
                                                      .Subscribe(filterListItems);

            // Monitor the selected Game so we can display Screenshot and other info on the fly
#pragma warning disable 168
            IDisposable gameSelectionChanged = Observable.FromEventPattern<EventArgs>(GamesListView, "SelectionChanged")
#pragma warning restore 168
                                                         .Select(selected => ((ListView) selected.Sender).SelectedItem)
                                                         .Subscribe(showGameMedia);

            // Monitor the number of games if the list is refreshed
#pragma warning disable 168
            IDisposable numberOfGamesChanged = Observable.FromEventPattern<EventArgs>(GamesListView, "LayoutUpdated")
#pragma warning restore 168
                                                         .Subscribe(games => updateNoOfGames());

        }

        /// <summary>
        ///     Simple helper methods that turns a link string into a embed string
        ///     for a YouTube item.
        ///     turns
        ///     http://www.youtube.com/watch?v=hV6B7bGZ0_E
        ///     into
        ///     http://www.youtube.com/v/hV6B7bGZ0_E
        /// </summary>
        private static string GetEmbedUrlFromLink(string link)
        {
            try
            {
                //string embedUrl = link.Replace("watch?v=", "v/").Replace("&feature=youtube_gdata", "");
                string embedUrl = link.Replace("watch?v=", "embed/").Replace("&feature=youtube_gdata", "");
                return embedUrl;
            }
            catch
            {
                return link;
            }
        }

        #region Data

        private const string SEARCH = "http://gdata.youtube.com/feeds/api/videos?q={0}&alt=rss&&max-results=1&v=2";

        #endregion

        #region Functions

        private const int SW_SHOWMAXIMIZED = 3;

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
            SafeNativeMethods.ShowWindow(runningProcess.MainWindowHandle, SW_SHOWMAXIMIZED);
            SafeNativeMethods.SetForegroundWindow(runningProcess.MainWindowHandle);

            return false;
        }

        /// <summary>
        ///     Delete the game's specified Screenshot
        /// </summary>
        /// <param name="p">The number identifying the screenshot to delete</param>
        private void deleteGameScreenshot(int p)
        {
            string img = null;
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

            MessageBoxResult result =
                MessageBox.Show("Are you sure? This will DELETE the following file from the Screenshots folder:\n\n" +
                                img, "Please confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            // Delete the screenshot file selected, based on the p value (1-3)
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

            File.Delete(img);
        }

        /// <summary>
        ///     Remove current game from the Database
        /// </summary>
        private void removeGameFromDB()
        {
            var oDataRowView = GamesListView.SelectedItem as DataRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                if (viewMenu_Show_GameTitles.IsChecked)
                {
                    if (oDataRowView != null)
                        AmigulaDBDataSetGamesTableAdapter.DeleteBasedOnTitle(oDataRowView.Row["Title"] as string);
                }
                else
                {
                    if (oDataRowView != null)
                        AmigulaDBDataSetGamesTableAdapter.DeleteQuery((long) oDataRowView.Row["ID"],
                            oDataRowView["PathToFile"] as string);
                }
                fillListView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to remove this game from the Database!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Update selected Game metadata from Hall of Light website
        /// </summary>
        /// <param name="currentgame"></param>
        private void updateGameMetadata(object currentgame)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            const string targetURL = @"http://hol.abime.net/hol_search.php?find=";
            string gameTitleforURL = cleanGameTitle(currentgame, "URL");
            var oDataRowView = currentgame as DataRowView;

            // open a web connection to HOL, get all the links for the selected title in order to find the game's unique ID
            var webGet = new HtmlWeb();
            HtmlDocument document;

            string gamelink = null;
            string gameTitle = null;

            bool tryagain = true;
            while (tryagain)
            {
                document = webGet.Load(targetURL + gameTitleforURL);
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
                gameTitle = gameTitleforURL.Replace("%20", " ").TrimEnd(' ');

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
                    var inputBoxDialog = new inputBox(ref gameTitle);
                    bool? result = inputBoxDialog.ShowDialog();
                    //MessageBox.Show("The result received was: " + result.ToString() + "\nThe current text is: " + inputBoxDialog.TextValue);
                    gameTitle = inputBoxDialog.TextValue;
                    if (result == true)
                    {
                        tryagain = true;
                        gameTitleforURL = gameTitle.Replace(" ", "%20");
                    }
                    else tryagain = false;
                }
            }

            // if we've found a link
            if (gamelink == null) return;
            // open that game's unique ID page
            string gameurl = gamelink.Substring(gamelink.IndexOf("http", StringComparison.Ordinal), gamelink.IndexOf(",", StringComparison.Ordinal) - gamelink.IndexOf("http", StringComparison.Ordinal));
            document = webGet.Load(gameurl);

            // Use XPath to locate the information we're going to fetch:
            // XPath for Year: //table[@width='100%']/tr[1]/td[2]/a
            string fetchedYear = "";
            if (document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[1]/td[2]/a") != null)
                fetchedYear = document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[1]/td[2]/a").InnerText;
            //MessageBox.Show("The game's Year is: " + fetchedYear);

            // XPath for Publisher: //table[@width='100%']/tr[2]/td[4]/a
            string fetchedPublisher = "";
            if (document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[2]/td[4]/a") != null)
                fetchedPublisher = document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[2]/td[4]/a").InnerText;
            bool publisherExists = AmigulaDBDataSet.Publishers.AsEnumerable()
                .Any(
                    row =>
                        fetchedPublisher == row.Field<string>("Publisher_Label"));

            if (!publisherExists)
            {
                try
                {
                    AmigulaDBDataSetPublishersTableAdapter.InsertPublisher(fetchedPublisher);
                    AmigulaDBDataSetPublishersTableAdapter.Fill(AmigulaDBDataSet.Publishers);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "An exception has occured while trying to add a new Publisher\n\n" + ex.Message,
                        "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // XPath for Genre: //table[@width='100%']/tr[12]/td/table/tr[2]/td[2]/a
            string fetchedGenre =
                document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[13]/td[1]/table/tr[2]/td[2]/a")
                    .InnerText;
            bool genreExists = AmigulaDBDataSet.Genres.AsEnumerable()
                .Any(row => fetchedGenre == row.Field<String>("Genre_label"));

            if (!genreExists)
            {
                try
                {
                    AmigulaDBDataSetGenresTableAdapter.InsertGenre(fetchedGenre);
                    AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "An exception has occured while trying to add a new Genre\n\n" + ex.Message,
                        "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // if there's no screenshot found locally, try to fetch one from the website
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
                        addGameScreenshot(Path.Combine(Path.GetTempPath(), "sshot.png"), gameTitle);
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
                            addGameScreenshot(Path.Combine(Path.GetTempPath(), "sshot.png"), gameTitle);
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
                            addGameScreenshot(Path.Combine(Path.GetTempPath(), "sshot.png"), gameTitle);
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

            // Save the fetched information in the database
            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                // get the ID for the Genre label
                EnumerableRowCollection<int> genre_id = from row in AmigulaDBDataSet.Genres.AsEnumerable()
                    where
                        row.Field<string>("Genre_Label") == fetchedGenre
                    select row.Field<int>("Genre_ID");

                // get the ID for the Publisher label
                EnumerableRowCollection<int> publisher_id =
                    from row in AmigulaDBDataSet.Publishers.AsEnumerable()
                    where row.Field<string>("Publisher_Label") == fetchedPublisher
                    select row.Field<int>("Publisher_ID");

                AmigulaDBDataSetGamesTableAdapter.UpdateMetadata(genre_id.First(), int.Parse(fetchedYear),
                    publisher_id.First(),
                    oDataRowView.Row["Notes"] as string,
                    oDataRowView.Row["Title"] as string);
                fillListView();
                //showGameMedia(currentgame);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to update the metadata!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Add a new Screenshot to a game
        /// </summary>
        /// <param name="screenshotFilename">The full path and filename for the screenshot</param>
        /// <param name="gameTitle">The game's title</param>
        private void addGameScreenshot(string screenshotFilename, string gameTitle)
        {
            int n;
            string gameSubFolder;

            // Get the first letter of the game, to get the subfolder from that.
            // if the first letter is a number, the subfolder should be set to "0"
            if (int.TryParse(gameTitle.Substring(0, 1), out n)) gameSubFolder = "0\\";
            else gameSubFolder = gameTitle.Substring(0, 1) + "\\";
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
        ///     Unmark a game from Favorites
        /// </summary>
        private void unmarkFromFavorites()
        {
            var oDataRowView = GamesListView.SelectedItem as DataRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                if (oDataRowView != null)
                {
                    oDataRowView.Row["Favorite"] = 0;
                    AmigulaDBDataSetGamesTableAdapter.UpdateFavoriteStatus(0, oDataRowView.Row["Title"] as string);
                }
                fillListView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to set this game as a Favorite!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Mark a game as Favorite
        /// </summary>
        private void markAsFavorite()
        {
            var oDataRowView = GamesListView.SelectedItem as DataRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                if (oDataRowView != null)
                {
                    oDataRowView.Row["Favorite"] = 1;
                    AmigulaDBDataSetGamesTableAdapter.UpdateFavoriteStatus(1, oDataRowView.Row["Title"] as string);
                }
                fillListView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to set this game as a Favorite!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Fill the listview while respecting the Favorite options
        /// </summary>
        private void fillListView()
        {
            // Save the currently selected item so we can restore it after refreshing the listview
            object tmpSelectedValue = GamesListView.SelectedValue;
            switch (viewMenu_Favorites_ShowOnly.IsChecked)
            {
                case true:
                    {
                        if (viewMenu_Show_GameTitles.IsChecked)
                            AmigulaDBDataSetGamesTableAdapter.FillByTitleFavoritesOnly(AmigulaDBDataSet.Games);
                        else
                            AmigulaDBDataSetGamesTableAdapter.FillByAllFilesFavoritesOnly(AmigulaDBDataSet.Games);
                        AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
                        AmigulaDBDataSetPublishersTableAdapter.Fill(AmigulaDBDataSet.Publishers);
                        GamesListView.SelectedValue = tmpSelectedValue;
                        break;
                    }
                case false:
                    {
                        if (viewMenu_Show_GameTitles.IsChecked)
                            AmigulaDBDataSetGamesTableAdapter.FillByTitle(AmigulaDBDataSet.Games);
                        else
                            AmigulaDBDataSetGamesTableAdapter.FillByAllFiles(AmigulaDBDataSet.Games);
                        AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
                        AmigulaDBDataSetPublishersTableAdapter.Fill(AmigulaDBDataSet.Publishers);
                        GamesListView.SelectedValue = tmpSelectedValue;
                        break;
                    }
            }
        }

        /// <summary>
        ///     Check if Amiga Forever is installed
        /// </summary>
        /// <returns></returns>
        private static void getAmigaForeverRegistry()
        {
            // Detect whether we're running on a 64-bit OS, change the registry scope accordingly
            string rootKey = Is64BitOperatingSystem() ? "SOFTWARE\\Wow6432Node\\CLoanto\\Amiga Forever" : "SOFTWARE\\CLoanto\\Amiga Forever";

            // These are the keys we're interested in, if Amiga Forever is installed
            // AmigaFiles: where the WinUAE configuration files will be
            // Path: where AmigaForever binaries (WinUAE) is installed
            var afKeys = new[] {"AmigaFiles", "Path"};

            //rootKey += "\\" + subKey;
            RegistryKey patRegistry = Registry.LocalMachine.OpenSubKey(rootKey);
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
        private static void refreshUAEconfigs()
        {
            if (!string.IsNullOrEmpty(Settings.Default.UAEConfigsPath))
            {
                try
                {
                    IEnumerable<string> configFiles = Directory.EnumerateFiles(Settings.Default.UAEConfigsPath, "*.uae");
                    UAEconfigViewSource = configFiles.ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "An exception has occured while trying to locate the UAE config files:\n\n" + ex.Message,
                        "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // cleanup the contents and remove the path and extension information
                for (int i = 0; i < UAEconfigViewSource.Count; i++)
                {
                    UAEconfigViewSource[i] = Path.GetFileNameWithoutExtension(UAEconfigViewSource[i]);
                }
                // insert the value "default" as the first in the list
                UAEconfigViewSource.Insert(0, "default");
            }
            else
                MessageBox.Show(
                    "It looks like the WinUAE Configurations folder could not be located in your system!\nPlease make sure you have one available at least under the WinUAE folder in Program Files.",
                    "No Configurations found", MessageBoxButton.OK, MessageBoxImage.Stop);
        }

        /// <summary>
        ///     Perfom initial checks and configuration validation
        /// </summary>
        private static void initialChecks()
        {
            // If there's no emulator set in Preferences, check for AmigaForever first
            // If that is not found, check for WinUAE and if that is not found either, show a warning
            if (string.IsNullOrEmpty(Settings.Default.EmulatorPath)
                // correct is OR not AND here, but I guess second check is not needed
                //&& String.IsNullOrEmpty(Settings.Default.UAEConfigsPath)
                )
            {
                // Check if Amiga Forever is installed
                getAmigaForeverRegistry();

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
            else {
                string tmpPath=Path.Combine(Path.GetDirectoryName(Settings.Default.EmulatorPath), "Configurations");
                if (Directory.Exists(tmpPath))
                    Settings.Default.UAEConfigsPath = tmpPath;
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
                        Settings.Default.Save();
                    }
                    else
                    {
                        Settings.Default.MusicPlayerPath = ".\\xmplay\\xmplay.exe";
                        Settings.Default.Save();
                    }
                }
                else
                {
                    Settings.Default.MusicPlayerPath = ".\\xmplay\\xmplay.exe";
                    Settings.Default.Save();
                }
            }

            // If GameBase Amiga folder is found in the default location (C:\GameBase\GameBase Amiga), use it automatically for Screenshots, Music, etc.
            if (string.IsNullOrEmpty(Settings.Default.ScreenshotsPath) ||
                (string.IsNullOrEmpty(Settings.Default.MusicPath)))
            {
                if (Directory.Exists("C:\\GameBase\\GameBase Amiga"))
                {
                    MessageBoxResult result =
                        MessageBox.Show(
                            "I found GameBase installed in your system.\n\nWould you like to use the Screenshots and Music paths from it?",
                            "GameBase found", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        Settings.Default.ScreenshotsPath = "C:\\GameBase\\GameBase Amiga\\Screenshots";
                        Settings.Default.MusicPath = "C:\\GameBase\\GameBase Amiga\\Music";
                        Settings.Default.Save();
                    }
                }
            }
            // Check if www.youtube.com is reachable, enable Longplay feature if it is, disable it otherwise
            Settings.Default.ShowLongplayVideos = RemoteFileExists("http://www.youtube.com");
        }

        /// <summary>
        ///     Launch the currently selected game in WinUAE
        /// </summary>
        private void launchInUAE()
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
                string gamePath = cleanGameTitle(GamesListView.SelectedItem, "Path");
                if (string.IsNullOrEmpty(gamePath) == false)
                {
                    try
                    {
                        // Launch WinUAE from selected path giving it the selected config and game as a parameter
                        Process.Start(Settings.Default.EmulatorPath, gamePath);
                        var oDataRowView = GamesListView.SelectedItem as DataRowView;
                        if (oDataRowView == null) return;
                        oDataRowView.Row["TimesPlayed"] = (int) oDataRowView.Row["TimesPlayed"] + 1;
                        oDataRowView.Row["DateLastPlayed"] = DateTime.Now;
                        AmigulaDBDataSetGamesTableAdapter.UpdateTimesPlayed((int) oDataRowView.Row["TimesPlayed"],
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
                else
                    MessageBox.Show(
                        "Sorry, the selected game\n" + gamePath +
                        " \nwas not found!\nPlease check the path and verify the file actually exists there!",
                        "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

/*
        /// <summary>
        ///     Replaces text in a file.
        /// </summary>
        /// <param name="filePath">Path of the text file.</param>
        /// <param name="searchText">Text to search for.</param>
        /// <param name="replaceText">Text to replace the search text.</param>
        public static void ReplaceInFile(string filePath, string searchText, string replaceText)
        {
            if (File.Exists(filePath))
            {
                var reader = new StreamReader(filePath);
                string content = reader.ReadToEnd();
                reader.Close();

                content = Regex.Replace(content, searchText, replaceText);

                var writer = new StreamWriter(filePath);
                writer.Write(content);
                writer.Close();
            }
        }
*/

        private static void ReplaceInFile(string filePath, IDictionary<int, string> searchText,
                                         IDictionary<int, string> replaceText)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                var reader = new StreamReader(filePath);
                string content = reader.ReadToEnd();
                reader.Close();

                if (searchText.Count == replaceText.Count)
                {
                    for (int i = 0; i < searchText.Count; i++)
                    {
                        if (Regex.IsMatch(content, searchText[i]))
                            content = Regex.Replace(content, searchText[i], replaceText[i]);
                        else
                            content += "\r\n" + replaceText[i];
                    }
                }
                var writer = new StreamWriter(filePath);
                writer.Write(content);
                writer.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                            "Sorry, an exception has occured while trying to read/write to a file!\n\n" +
                            ex.Message);
            }          
        }

        /// <summary>
        ///     Updates the Status Bar test with the total number of games shown
        /// </summary>
        private void updateNoOfGames()
        {
            // if the game Listview is not empty, count the entries and display the number in the statusbar
            txtStatusText.Text = GamesListView.Items.Count.ToString(CultureInfo.CurrentCulture) + " games found";
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
            //selectFile.FileName = appHandler;

            // Show open file dialog box
            bool? AppResult = selectFile.ShowDialog();

            // Process open file dialog box results 
            if (AppResult != true) return;
            // Select file
            if (appHandler == "Emulator")
            {
                Settings.Default.EmulatorPath = selectFile.FileName;
            }
            if (appHandler == "MusicPlayer")
            {
                Settings.Default.MusicPlayerPath = selectFile.FileName;
            }
            Settings.Default.Save();
        }

        /// <summary>
        ///     Cleanup the selected game title from the list, based on the filename
        /// </summary>
        /// <param name="currentgame">The selected game title to cleanup</param>
        /// <param name="cleanupType">The type of cleanup to attempt, possible values are "URL", "Path", "Screenshot"</param>
        /// <returns></returns>
        private static string cleanGameTitle(object currentgame, string cleanupType)
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
            string selectedGame = "";
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
                    break;
                }
                case "Path":
                {
                    // prepare the string for passing it to WinUAE as a parameter
                    // a configuration file must be passed to WinUAE besides the actual filename
                    var selectedGamePath = oDataRowView.Row["PathToFile"] as string;
                    var selectedUAEconfig = oDataRowView.Row["UAEconfig"] as string;

                    // new variable to hold a list of all the game disks, with full path-filenames
                    SortedList<int, string> gameDisksFullPath = identifyGameDisks(selectedGamePath);

                    // variable to hold the "diskimageX=" values in the UAE config, one for each disk found
                    var diskImageX = new SortedList<int, string>();

                    // If there are more than 1 disks for this game
                    if (gameDisksFullPath.Count > 1)
                    {
                        // then for each disk found, we need to add an entry in the UAE config file to pass it to the DiskSwapper
                        for (int i = 0; i < gameDisksFullPath.Count; i++)
                        {
                            // replace any entry of diskimageX=* (where X=number and *=anything)
                            diskImageX[i] = "diskimage" + i + "=.*";
                            // text to be placed in the UAE config for the DiskSwapper
                            gameDisksFullPath[i] = "diskimage" + i + "=" + gameDisksFullPath[i];
                        }
                        // cleanup any extra entries of diskimageX in the config file
                        for (int i = gameDisksFullPath.Count; i < 20; i++)
                        {
                            diskImageX[i] = "diskimage" + i + "=.*";
                            gameDisksFullPath[i] = "diskimage" + i + "=";
                        }
                        // open the UAE config, check if it contains any entries for "diskimage="
                        // if it does, replace them with the current disks of the selected game
                        // if it doesn't, append those lines to the config file
                        if (selectedUAEconfig == "default")
                            ReplaceInFile("configs\\" + selectedUAEconfig + ".uae", diskImageX,
                                gameDisksFullPath);
                        else
                            ReplaceInFile(
                                Path.Combine(Settings.Default.UAEConfigsPath, selectedUAEconfig) + ".uae",
                                diskImageX, gameDisksFullPath);
                    }

                    // finally, pass it over as a parameter to UAE below
                    // if the config file doesn't exist, WinUAE should still startup with the full GUI so it should be safe no to check for it
                    if (selectedUAEconfig == "default")
                        selectedGame = "-f \"" +
                                       Path.Combine(Environment.CurrentDirectory,
                                           "configs\\" + selectedUAEconfig + ".uae") + "\"" + " -0 \"" +
                                       selectedGamePath + "\"";
                    else
                        selectedGame = "-f \"" +
                                       Path.Combine(Environment.CurrentDirectory,
                                           Path.Combine(Settings.Default.UAEConfigsPath,
                                               selectedUAEconfig) + ".uae") + "\"" + " -0 \"" +
                                       selectedGamePath + "\"";
                    break;
                }
                case "URL":
                {
                    // prepare the string for passing it to a URL as a parameter
                    // Replace any spaces with "%20" and try to clean up the title
                    selectedGame = oDataRowView.Row["Title"] as string;
                    // Use RegEx to clean up anything in () or []
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
                    break;
                }
            }
            return selectedGame;
        }

        /// <summary>
        ///     Get the game Year from the filename if it exists
        /// </summary>
        /// <param name="selectedGamePath">The selected game filename</param>
        /// <returns></returns>
        private static int getGameYear(string selectedGamePath)
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
        private static SortedList<int, string> identifyGameDisks(string selectedGamePath)
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

        /// <summary>
        ///     Search for the selected title in various websites
        /// </summary>
        /// <param name="currentgame">The currently selected title</param>
        /// <param name="URLsite">The website to lookup the title on, possible values are "HOL, "LemonAmiga"</param>
        private static void lookupURL(object currentgame, string URLsite)
        {
            // Search for the selected game in various Amiga websites
            // Valid parameters for URLsite are:
            // HOL - search for the game in HOL
            // LemonAmiga - search for the game in LemonAmiga
            string gameTitleforURL = cleanGameTitle(currentgame, "URL");
            if (String.IsNullOrEmpty(gameTitleforURL) == false)
            {
                switch (URLsite)
                {
                    case "HOL":
                        {
                            const string targetURL = @"http://hol.abime.net/hol_search.php?find=";
                            Process.Start(targetURL + gameTitleforURL);
                            break;
                        }
                    case "LemonAmiga":
                        {
                            const string targetURL = @"http://www.lemonamiga.com/games/list.php?list_letter=";
                            Process.Start(targetURL + gameTitleforURL);
                            break;
                        }
                }
            }
        }

        /// <summary>
        ///     Play the current game's music if found, using the music player configured in Preferences
        /// </summary>
        /// <param name="currentgame">The currently selected game title</param>
        private static void playGameMusic(object currentgame)
        {
            // Display the music found for the selected game (if found)
            if (!string.IsNullOrEmpty(Settings.Default.MusicPlayerPath))
            {
                if (File.Exists(Settings.Default.MusicPlayerPath))
                {
                    // Need to check if file exists first
                    string gameMusicFile = cleanGameTitle(currentgame, "Screenshot")
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

        /// <summary>
        ///     Show the game's media files (such as screenshots) in the interface's placeholders
        /// </summary>
        /// <param name="currentgame">The currently selected game title</param>
        private void showGameMedia(object currentgame)
        {
            // Display the screenshot for the selected game
            if (!string.IsNullOrEmpty(Settings.Default.ScreenshotsPath))
            {
                // call cleanGameTitle to cleanup the title and add the png extension to it
                string gameImageFile = cleanGameTitle(currentgame, "Screenshot");
                imgScreenshot.Opacity = 0.25;
                imgScreenshot2.Opacity = 0.25;
                imgScreenshot3.Opacity = 0.25;
                imgScreenshot.Source =
                    new BitmapImage(
                        new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"images\Screenshot_placeholder.png")));
                imgScreenshot2.Source =
                    new BitmapImage(
                        new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"images\Screenshot_placeholder.png")));
                imgScreenshot3.Source =
                    new BitmapImage(
                        new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"images\Screenshot_placeholder.png")));
                if (!string.IsNullOrEmpty(gameImageFile))
                {
                    // check if the filename exists first, otherwise there's nothing to display
                    if (File.Exists(Path.Combine(Settings.Default.ScreenshotsPath, gameImageFile)))
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
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                "An exception has occured while trying to display the game's image:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        // assign our image source to the placeholder
                        imgScreenshot.Source = gameScreenshot;
                        imgScreenshot.Opacity = 1;
                        // resize the container
                        //gridImgContainer.Height = 256;
                    }

                    // check if the filename exists first, otherwise there's nothing to display
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "_1.png"))))
                    {
                        // initialize a new image source
                        var gameScreenshot = new BitmapImage();
                        try
                        {
                            gameScreenshot.BeginInit();
                            gameScreenshot.CacheOption = BitmapCacheOption.OnLoad;
                            gameScreenshot.UriSource =
                                new Uri(Path.Combine(Settings.Default.ScreenshotsPath,
                                                     gameImageFile.Replace(".png", "_1.png")));
                            gameScreenshot.EndInit();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                "An exception has occured while trying to display the game's image:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        // assign our image source to the placeholder
                        imgScreenshot2.Source = gameScreenshot;
                        imgScreenshot2.Opacity = 1;
                        // resize the container
                        //gridImgContainer.Height = 512;
                    }

                    // check if the filename exists first, otherwise there's nothing to display
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "_2.png"))))
                    {
                        // initialize a new image source
                        var gameScreenshot = new BitmapImage();
                        try
                        {
                            gameScreenshot.BeginInit();
                            gameScreenshot.CacheOption = BitmapCacheOption.OnLoad;
                            gameScreenshot.UriSource =
                                new Uri(Path.Combine(Settings.Default.ScreenshotsPath,
                                                     gameImageFile.Replace(".png", "_2.png")));
                            gameScreenshot.EndInit();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                "An exception has occured while trying to display the game's image:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        // assign our image source to the placeholder
                        imgScreenshot3.Source = gameScreenshot;
                        imgScreenshot3.Opacity = 1;
                        // resize the container
                        //gridImgContainer.Height = 768;
                    }

                    // fix for some filenames ending with "_.png" in GameBase!
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "_.png"))))
                    {
                        // initialize a new image source
                        var gameScreenshot = new BitmapImage();
                        try
                        {
                            gameScreenshot.BeginInit();
                            gameScreenshot.CacheOption = BitmapCacheOption.OnLoad;
                            gameScreenshot.UriSource =
                                new Uri(Path.Combine(Settings.Default.ScreenshotsPath,
                                                     gameImageFile.Replace(".png", "_.png")));
                            gameScreenshot.EndInit();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                "An exception has occured while trying to display the game's image:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        // assign our image source to the placeholder
                        imgScreenshot.Source = gameScreenshot;
                        imgScreenshot.Opacity = 1;
                        // resize the container
                        //gridImgContainer.Height = 256;
                    }

                    // fix for some filenames ending with "_.png" in GameBase!
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "__1.png"))))
                    {
                        // initialize a new image source
                        var gameScreenshot = new BitmapImage();
                        try
                        {
                            gameScreenshot.BeginInit();
                            gameScreenshot.CacheOption = BitmapCacheOption.OnLoad;
                            gameScreenshot.UriSource =
                                new Uri(Path.Combine(Settings.Default.ScreenshotsPath,
                                                     gameImageFile.Replace(".png", "__1.png")));
                            gameScreenshot.EndInit();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                "An exception has occured while trying to display the game's image:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        // assign our image source to the placeholder
                        imgScreenshot2.Source = gameScreenshot;
                        imgScreenshot2.Opacity = 1;
                        // resize the container
                        //gridImgContainer.Height = 512;
                    }

                    // fix for some filenames ending with "_.png" in GameBase!
                    if (
                        File.Exists(Path.Combine(Settings.Default.ScreenshotsPath,
                                                 gameImageFile.Replace(".png", "__2.png"))))
                    {
                        // initialize a new image source
                        var gameScreenshot = new BitmapImage();
                        try
                        {
                            gameScreenshot.BeginInit();
                            gameScreenshot.CacheOption = BitmapCacheOption.OnLoad;
                            gameScreenshot.UriSource =
                                new Uri(Path.Combine(Settings.Default.ScreenshotsPath,
                                                     gameImageFile.Replace(".png", "__2.png")));
                            gameScreenshot.EndInit();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                "An exception has occured while trying to display the game's image:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        // assign our image source to the placeholder
                        imgScreenshot3.Source = gameScreenshot;
                        imgScreenshot3.Opacity = 1;
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
                string gameMusicFile = cleanGameTitle(currentgame, "Screenshot")
                    .Replace("_", " ")
                    .Replace(".png", ".zip");
                if (!string.IsNullOrEmpty(gameMusicFile))
                {
                    // check if the filename exists first, otherwise there's nothing to do
                    if (File.Exists(Path.Combine(Settings.Default.MusicPath, gameMusicFile)))
                    {
                        btnPlayMusic.IsEnabled = true;
                        btnPlayMusic.Opacity = 1;
                    }
                    else
                    {
                        btnPlayMusic.IsEnabled = false;
                        btnPlayMusic.Opacity = 0.25;
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
        ///     Filter the list of games in the interface based on the search pattern specified
        /// </summary>
        /// <param name="searchFilter">The search pattern to use</param>
        private void filterListItems(string searchFilter)
        {
            if (searchFilter == "Search for Game") return;
            // Filter the list dynamically when the user enters something in the Filter textbox
            var cv = (BindingListCollectionView) CollectionViewSource.GetDefaultView(AmigulaDBDataSet.Games);
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
        ///     Process all files (recursively) in the directory specified for any games and add them to the database
        /// </summary>
        /// <param name="targetDirectory">The target directory to scan for games</param>
        private void ProcessDirectory(string targetDirectory)
        {
            // Process all files in the directory passed in, recurse on any directories  
            // that are found, and process the files they contain. 

            // Empty current DataSet to avoid duplicate entries
            AmigulaDBDataSet.Clear();
            //try
            //{
            //    AmigulaDBDataSetGamesTableAdapter.DeleteAllQuery();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("An exception has occured while trying to empty the database:\n\n" + ex.Message, "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            //}

            // Process the list of files found in the directory. 
            // Supported filename extensions
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".zip",
                    ".adz",
                    ".adf",
                    ".dms",
                    ".ipf"
                };

            // prepare the progress bar
            progBar.Height = 15;
            progBar.Width = 100;
            progBar.IsIndeterminate = true;
            statusBar.Items.Add(progBar);
            // Show the Cancel button to allow the user to abort the process
            btnCancel.Visibility = Visibility.Visible;

            // Set the Cancel click event as an observable so we can monitor it
            IObservable<EventPattern<EventArgs>> cancelClicked = Observable.FromEventPattern<EventArgs>(btnCancel,
                                                                                                        "Click");

            AmigulaDBDataSet.GenresRow gameGenre = AmigulaDBDataSet.Genres.FirstOrDefault();
            AmigulaDBDataSet.PublishersRow gamePublisher = AmigulaDBDataSet.Publishers.FirstOrDefault();

            // Use Rx to pick the scanned files from the IEnumerable collection, fill them in the DataSet and finally save the DataSet in the DB
#pragma warning disable 168
            IDisposable files = Directory.EnumerateFiles(targetDirectory, "*.*", SearchOption.AllDirectories)
#pragma warning restore 168
                                         .Where(s => extensions.Contains(Path.GetExtension(s)))
                                         .ToObservable(TaskPoolScheduler.Default)
                                         .TakeUntil(cancelClicked)
                                         .Do(x =>
                                             {
                                                 try
                                                 {
                                                     // Check if the path to file already exists in the database, skip inserting it if it does
                                                     if (AmigulaDBDataSetGamesTableAdapter.FileExists(x) == 0)
                                                     {
                                                         AmigulaDBDataSet.Games.AddGamesRow(
                                                             Regex.Replace(Path.GetFileNameWithoutExtension(x),
                                                                           @"Disk\s(\d{1})\sof\s(\d{1})|Disk-(\d{1})|Disk(\d{1})$|Disk(\d{2})$|Disk[A-Za-z]$|-(\d{1})$|[\[(].+?[\])]|_",
                                                                           ""), x, "default", identifyGameDisks(x).Count,
                                                             getGameYear(x), 0, DateTime.Today, 0, gameGenre,
                                                             gamePublisher, "");
                                                     }
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     MessageBox.Show(
                                                         "An exception has occured while entering the games in the database:\n\n" +
                                                         ex.Message, "An exception has occured", MessageBoxButton.OK,
                                                         MessageBoxImage.Error);
                                                 }
                                             })
                                         .TakeLast(1)
                                         .Do(_ =>
                                             {
                                                 try
                                                 {
                                                     AmigulaDBDataSetGamesTableAdapter.Update(AmigulaDBDataSet.Games);
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     MessageBox.Show(
                                                         "An exception has occured while trying to save the changes in the database:\n\n" +
                                                         ex.Message, "An exception has occured", MessageBoxButton.OK,
                                                         MessageBoxImage.Error);
                                                 }
                                             })
                                         .ObserveOnDispatcher()
                                         .Subscribe(y => { },
                                                    () =>
                                                        {
                                                            statusBar.Items.Remove(progBar);
                                                            btnCancel.Visibility = Visibility.Collapsed;
                                                            fillListView();
                                                        });

            // Cleanup of any files that exist in the database, but no longer exist in the filesystem
        }

        /// <summary>
        /// Display Longplay videos for selected game from Youtube
        /// </summary>
        private void DisplayLongplay()
        {
            if (!Settings.Default.ShowLongplayVideos) return;
            // Load longplay video
            var oDataRowView = GamesListView.SelectedItem as DataRowView;
            if (oDataRowView == null) return;
            var LongplayTitle = oDataRowView.Row["Title"] as string;
            List<YouTubeInfo> videos = LoadVideosKey("Amiga Longplay " + LongplayTitle);
            if (!videos.Any()) return;
            var video = new Uri(GetEmbedUrlFromLink(videos[0].EmbedUrl), UriKind.Absolute);
            wbLongplay.Source = video;
        }

        #region Is64BitOperatingSystem (IsWow64Process)

        /// <summary>
        ///     The function determines whether the current operating system is a
        ///     64-bit operating system.
        /// </summary>
        /// <returns>
        ///     The function returns true if the operating system is 64-bit;
        ///     otherwise, it returns false.
        /// </returns>
        private static bool Is64BitOperatingSystem()
        {
            if (IntPtr.Size == 8) // 64-bit programs run only on Win64
            {
                return true;
            }
            // Detect whether the current process is a 32-bit process 
            // running on a 64-bit system.
            bool flag;
            return ((DoesWin32MethodExist("kernel32.dll", "IsWow64Process") &&
                     SafeNativeMethods.IsWow64Process(SafeNativeMethods.GetCurrentProcess(), out flag)) && flag);
        }

        /// <summary>
        ///     The function determins whether a method exists in the export
        ///     table of a certain module.
        /// </summary>
        /// <param name="moduleName">The name of the module</param>
        /// <param name="methodName">The name of the method</param>
        /// <returns>
        ///     The function returns true if the method specified by methodName
        ///     exists in the export table of the module specified by moduleName.
        /// </returns>
        private static bool DoesWin32MethodExist(string moduleName, string methodName)
        {
            IntPtr moduleHandle = SafeNativeMethods.GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero)
            {
                return false;
            }
            return (SafeNativeMethods.GetProcAddress(moduleHandle, methodName) != IntPtr.Zero);
        }

        #endregion

        #endregion

        #region Events

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
        ///     Handle the DoubleClick event in the ListView (usually launch the selected game)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gamesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            launchInUAE();
        }

        /// <summary>
        ///     Save the window position and state before closing down the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Top = Top;
            Settings.Default.Left = Left;
            Settings.Default.Height = Height;
            Settings.Default.Width = Width;
            Settings.Default.WindowSetting = WindowState;
            Settings.Default.Save();
        }

        /// <summary>
        ///     When the DropDown is opened, it should be populated with all the available UAE configurations in the "config" folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboUAEconfig_DropDownOpened(object sender, EventArgs e)
        {
            refreshUAEconfigs();
        }

        /// <summary>
        ///     Open selected game's containing folder in Explorer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemShowInExplorer_Click(object sender, RoutedEventArgs e)
        {
            var oDataRowView = GamesListView.SelectedItem as DataRowView;
            if (oDataRowView != null) Process.Start(Path.GetDirectoryName(oDataRowView.Row["PathToFile"] as string));
        }

        /// <summary>
        ///     Launch selected game in WinUAE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemLaunchInWinUAE_Click(object sender, RoutedEventArgs e)
        {
            launchInUAE();
        }

        /// <summary>
        ///     Set selected game as Favorite
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemFavorite_Click(object sender, RoutedEventArgs e)
        {
            markAsFavorite();
        }

        /// <summary>
        ///     Remove a game from the Favorites
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemUnmarkFromFavorites_Click(object sender, RoutedEventArgs e)
        {
            unmarkFromFavorites();
        }

        /// <summary>
        ///     Remove a game from the database based on Title
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewMenuItemRemoveGame_Click(object sender, RoutedEventArgs e)
        {
            removeGameFromDB();
        }

        /// <summary>
        ///     Save selected value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboUAEconfig_DropDownClosed(object sender, EventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                AmigulaDBDataSet.Games[GamesListView.SelectedIndex].UAEconfig =
                    comboUAEconfig.SelectedValue.ToString();
                AmigulaDBDataSetGamesTableAdapter.UpdateUAEconfig(
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].UAEconfig,
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to save the selected UAE config!\n\n" + ex.Message,
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
            try
            {
                AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Genre_ID = (int) cmbboxGenre.SelectedValue;
                AmigulaDBDataSetGamesTableAdapter.UpdateGenre(
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Genre_ID,
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to save the selected genre!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Save the contents of the Publisher combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbboxPublisher_DropDownClosed(object sender, EventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Publisher_ID =
                    (int) cmbboxPublisher.SelectedValue;
                AmigulaDBDataSetGamesTableAdapter.UpdatePublisher(
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Publisher_ID,
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to save the selected Publisher!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Save the contents of the Notes textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tboxNotes_LostFocus(object sender, RoutedEventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Notes = tboxNotes.Text;
                AmigulaDBDataSetGamesTableAdapter.UpdateNotes(
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Notes,
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception has occured while trying to save the Notes!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Save the current game's Year of release
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tboxYear_LostFocus(object sender, RoutedEventArgs e)
        {
            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Year = int.Parse(tboxYear.Text);
                AmigulaDBDataSetGamesTableAdapter.UpdateYear(
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Year,
                    AmigulaDBDataSet.Games[GamesListView.SelectedIndex].Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception has occured while trying to save the Year!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);

            var oDataRowView = GamesListView.SelectedItem as DataRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                if (oDataRowView != null)
                {
                    var gameTitle = oDataRowView.Row["Title"] as string;
                    foreach (string file in files)
                    {
                        addGameScreenshot(file, gameTitle);
                    }
                }
                showGameMedia(GamesListView.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to set this game as a Favorite!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);

            var oDataRowView = GamesListView.SelectedItem as DataRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                if (oDataRowView != null)
                {
                    var gameTitle = oDataRowView.Row["Title"] as string;
                    foreach (string file in files)
                    {
                        addGameScreenshot(file, gameTitle);
                    }
                }
                showGameMedia(GamesListView.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to set this game as a Favorite!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);

            var oDataRowView = GamesListView.SelectedItem as DataRowView;

            if (GamesListView.SelectedIndex <= -1) return;
            try
            {
                if (oDataRowView != null)
                {
                    var gameTitle = oDataRowView.Row["Title"] as string;
                    foreach (string file in files)
                    {
                        addGameScreenshot(file, gameTitle);
                    }
                }
                showGameMedia(GamesListView.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An exception has occured while trying to set this game as a Favorite!\n\n" + ex.Message,
                    "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void imgScreenshot_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void imgScreenshot2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void imgScreenshot3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        #endregion

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
        ///     Search for the selected game title in Amiga Hall of Light website
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHOLsearch_Click(object sender, RoutedEventArgs e)
        {
            // Search for the selected game in HOL
            lookupURL(GamesListView.SelectedItem, "HOL");
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
            lookupURL(GamesListView.SelectedItem, "LemonAmiga");
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
        ///     Favorite toggle checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkbxMarkedFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (chkbxMarkedFavorite.IsChecked == true)
                markAsFavorite();
            else
                unmarkFromFavorites();
        }

        /// <summary>
        ///     Update the selected game's metadata from the web
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFetch_Click(object sender, RoutedEventArgs e)
        {
            updateGameMetadata(GamesListView.SelectedItem);
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
                playGameMusic(GamesListView.SelectedItem);
            }
            else
                AppNotDefined(
                    "There is no music player defined in the preferences!\nWithout one, you can't listen to the game music.\n\nDo you want to select the path to one now?",
                    "MusicPlayer");
        }

        #endregion

        #region MenuItems

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
        ///     Empty the games database completely
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editMenu_EmptyLib_Click(object sender, RoutedEventArgs e)
        {
            // Empty the games library DataSet and Database
            AmigulaDBDataSet.Clear();
            try
            {
                AmigulaDBDataSetGamesTableAdapter.DeleteAllQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception has occured while trying to empty the database:\n\n" + ex.Message,
                                "An exception has occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        /// <summary>
        ///     Display the About information window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpMenu_About_Click(object sender, RoutedEventArgs e)
        {
            // Process.Start("mailto:dimitris@blitterstudio.com?subject=Amigula feedback");
            //MessageBox.Show("AMI.G.U.LA. (AMIga Games Uae LAuncher) - v" + 
            //    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + 
            //    " beta" + "\n\nDeveloped by Dimitris Panokostas\nContact: dimitris@blitterstudio.com", 
            //    "Amigula information", MessageBoxButton.OK, MessageBoxImage.Information);
            var aboutWindow = new aboutWindow();
            aboutWindow.ShowDialog();
        }

        /// <summary>
        ///     Launch a game in WinUAE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileMenu_LaunchInWinUAE_Click(object sender, RoutedEventArgs e)
        {
            launchInUAE();
        }

        /// <summary>
        ///     Close the application after saving window position and state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileMenu_Close_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Top = Top;
            Settings.Default.Left = Left;
            Settings.Default.Height = Height;
            Settings.Default.Width = Width;
            Settings.Default.WindowSetting = WindowState;
            Settings.Default.Save();
            Close();
        }

        /// <summary>
        ///     Remove selected Game from the Database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileMenu_RemoveGame_Click(object sender, RoutedEventArgs e)
        {
            removeGameFromDB();
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
            AmigulaDBDataSetGamesTableAdapter.FillByTitle(AmigulaDBDataSet.Games);
            AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
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
            AmigulaDBDataSetGamesTableAdapter.FillByAllFiles(AmigulaDBDataSet.Games);
            AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
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
                AmigulaDBDataSetGamesTableAdapter.FillByTitleMostPlayed(AmigulaDBDataSet.Games);
            else
                AmigulaDBDataSetGamesTableAdapter.FillByAllFilesMostPlayed(AmigulaDBDataSet.Games);
            AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
        }

        private void viewMenu_Statistics_NeverPlayed_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Statistics_None.IsChecked = false;
            viewMenu_Statistics_MostPlayed.IsChecked = false;
            viewMenu_Statistics_NeverPlayed.IsChecked = true;
            viewMenu_Statistics_RecentlyPlayed.IsChecked = false;
            if (viewMenu_Show_GameTitles.IsChecked)
                AmigulaDBDataSetGamesTableAdapter.FillByTitleNeverPlayed(AmigulaDBDataSet.Games);
            else
                AmigulaDBDataSetGamesTableAdapter.FillByAllFilesNeverPlayed(AmigulaDBDataSet.Games);
            AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
        }

        private void viewMenu_Statistics_RecentlyPlayed_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Statistics_None.IsChecked = false;
            viewMenu_Statistics_MostPlayed.IsChecked = false;
            viewMenu_Statistics_NeverPlayed.IsChecked = false;
            viewMenu_Statistics_RecentlyPlayed.IsChecked = true;
            if (viewMenu_Show_GameTitles.IsChecked)
                AmigulaDBDataSetGamesTableAdapter.FillByTitleRecentlyPlayed(AmigulaDBDataSet.Games);
            else
                AmigulaDBDataSetGamesTableAdapter.FillByAllFilesRecentlyPlayed(AmigulaDBDataSet.Games);
            AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
        }

        private void viewMenu_Statistics_None_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Statistics_None.IsChecked = true;
            viewMenu_Statistics_MostPlayed.IsChecked = false;
            viewMenu_Statistics_NeverPlayed.IsChecked = false;
            viewMenu_Statistics_RecentlyPlayed.IsChecked = false;
            if (viewMenu_Show_GameTitles.IsChecked)
                AmigulaDBDataSetGamesTableAdapter.FillByTitle(AmigulaDBDataSet.Games);
            else
                AmigulaDBDataSetGamesTableAdapter.FillByAllFiles(AmigulaDBDataSet.Games);
            AmigulaDBDataSetGenresTableAdapter.Fill(AmigulaDBDataSet.Genres);
        }

        private void viewMenu_Favorites_ShowOnly_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Favorites_ShowOnly.IsChecked = true;
            viewMenu_Favorites_ShowOnTop.IsChecked = false;
            fillListView();
        }

        private void viewMenu_Favorites_ShowOnTop_Click(object sender, RoutedEventArgs e)
        {
            viewMenu_Favorites_ShowOnly.IsChecked = false;
            viewMenu_Favorites_ShowOnTop.IsChecked = true;
            fillListView();
        }

        /// <summary>
        ///     Delete the first screenshot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteScreenshot1_Click(object sender, RoutedEventArgs e)
        {
            deleteGameScreenshot(1);
        }

        /// <summary>
        ///     Delete the second screenshot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteScreenshot2_Click(object sender, RoutedEventArgs e)
        {
            deleteGameScreenshot(2);
        }

        /// <summary>
        ///     Delete the third screenshot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteScreenshot3_Click(object sender, RoutedEventArgs e)
        {
            deleteGameScreenshot(3);
        }

        #endregion

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

        private class YouTubeInfo
        {
            #region Data

            public string LinkUrl { get; set; }
            public string EmbedUrl { get; set; }

            #endregion
        }

        #region Load Videos From Feed

        /// <summary>
        ///     Returns a List<see cref="YouTubeInfo">YouTubeInfo</see> which represent
        ///     the YouTube videos that matched the keyWord input parameter
        /// </summary>
        private static List<YouTubeInfo> LoadVideosKey(string keyWord)
        {
            try
            {
                XElement xraw = XElement.Load(string.Format(SEARCH, keyWord));
                XElement xroot = XElement.Parse(xraw.ToString());
                var xElement = xroot.Element("channel");
                if (xElement != null)
                {
                    IEnumerable<YouTubeInfo> links = (from item in xElement.Descendants("item")
                                                      let element = item.Element("link")
                                                      where element != null
                                                      select new YouTubeInfo
                                                          {
                                                              LinkUrl = element.Value,
                                                              EmbedUrl = GetEmbedUrlFromLink(element.Value),
                                                          }).Take(1);

                    return links.ToList();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "ERROR");
            }
            return null;
        }

        #endregion
    }
}