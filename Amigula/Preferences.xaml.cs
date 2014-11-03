using Microsoft.Win32;
using System;
using System.Windows;

namespace Amigula
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class Preferences
    {
        public Preferences()
        {
            InitializeComponent();
        }

        private void frmPrefs_Loaded(object sender, RoutedEventArgs e)
        {
            tboxGamesLibPath.Text = Properties.Settings.Default.LibraryPath;
            tboxWinUAEPath.Text = Properties.Settings.Default.EmulatorPath;
            tboxScreenshotsPath.Text = Properties.Settings.Default.ScreenshotsPath;
            tboxMusicPlayerPath.Text = Properties.Settings.Default.MusicPlayerPath;
            tboxMusicPath.Text = Properties.Settings.Default.MusicPath;
        }

        private void selectUAEFolder()
        {
            var selectFile = new OpenFileDialog
                {
                    FileName = "WinUAE.exe",
                    InitialDirectory =
                        String.IsNullOrEmpty(tboxWinUAEPath.Text) == false
                            ? System.IO.Path.GetDirectoryName(tboxWinUAEPath.Text)
                            : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    DefaultExt = ".exe",
                    Filter = "Executable files (*.exe)|*.exe"
                };

            // Show open file dialog box
            bool? result = selectFile.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Select file 
                tboxWinUAEPath.Text = selectFile.FileName;
            }
        }

        private void selectGamesFolder()
        {
            var selectFolder = new System.Windows.Forms.FolderBrowserDialog();
            try
            {
                if (String.IsNullOrEmpty(tboxGamesLibPath.Text) == false)
                {
                    selectFolder.SelectedPath = tboxGamesLibPath.Text;
                }
                //else selectFolder.RootFolder = Environment.SpecialFolder.Personal;

                if (selectFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tboxGamesLibPath.Text = selectFolder.SelectedPath;
                }
            }
            finally { selectFolder.Dispose(); }
        }

        private void selectScreenshotsFolder()
        {
            var selectFolder = new System.Windows.Forms.FolderBrowserDialog();
            try
            {
                if (String.IsNullOrEmpty(tboxScreenshotsPath.Text) == false)
                {
                    selectFolder.SelectedPath = tboxScreenshotsPath.Text;
                }
                //else selectFolder.RootFolder = Environment.SpecialFolder.Personal;

                if (selectFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tboxScreenshotsPath.Text = selectFolder.SelectedPath;
                }
            }
            finally { selectFolder.Dispose(); }
        }

        private void selectMusicPlayerFolder()
        {
            var selectFile = new OpenFileDialog
                {
                    InitialDirectory =
                        String.IsNullOrEmpty(tboxMusicPlayerPath.Text) == false
                            ? System.IO.Path.GetDirectoryName(tboxMusicPlayerPath.Text)
                            : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    DefaultExt = ".exe",
                    Filter = "Executable files (*.exe)|*.exe"
                };
            //selectFile.FileName = "DeliPlayer.exe";

            // Show open file dialog box
            bool? result = selectFile.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Select file 
                tboxMusicPlayerPath.Text = selectFile.FileName;
            }
        }

        private void selectMusicFolder()
        {
            var selectFolder = new System.Windows.Forms.FolderBrowserDialog();
            try 
            {
                if (String.IsNullOrEmpty(tboxMusicPath.Text) == false)
                {
                    selectFolder.SelectedPath = tboxMusicPath.Text;
                }
                //else selectFolder.RootFolder = Environment.SpecialFolder.Personal;

                if (selectFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tboxMusicPath.Text = selectFolder.SelectedPath;
                }
            }
            finally
            {
                selectFolder.Dispose();
            }
        }

        private void btnChangeScreenshotsPath_Click(object sender, RoutedEventArgs e)
        {
            selectScreenshotsFolder();
        }

        private void btnChangeUAEPath_Click(object sender, RoutedEventArgs e)
        {
            selectUAEFolder();
        }

        private void btnChangeGamesPath_Click(object sender, RoutedEventArgs e)
        {
            selectGamesFolder();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.LibraryPath = tboxGamesLibPath.Text;
            Properties.Settings.Default.EmulatorPath = tboxWinUAEPath.Text;
            Properties.Settings.Default.ScreenshotsPath = tboxScreenshotsPath.Text;
            Properties.Settings.Default.MusicPlayerPath = tboxMusicPlayerPath.Text;
            Properties.Settings.Default.MusicPath = tboxMusicPath.Text;
            Properties.Settings.Default.Save();
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnChangeMusicPath_Click(object sender, RoutedEventArgs e)
        {
            selectMusicFolder();
        }

        private void btnChangeMusicPlayerPath_Click(object sender, RoutedEventArgs e)
        {
            selectMusicPlayerFolder();
        }

    }
}
