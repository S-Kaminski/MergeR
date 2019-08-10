using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MergeR_BGWorker
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Prop init.
        private BackgroundWorker bgw = null;
        public string MainPath { get; set; }
        public string OutputFilePath { get; set; }
        public string TilesDirectoryName { get; set; }
        public string TileFileName { get; set; }
        public int DirectoriesCount { get; set; }
        public int TilesCount { get; set; }
        public int TileWidth { get; set; }
        public int TileHight { get; set; }
        public int TotalWidth { get; set; }
        public int TotalHeight { get; set; }
        public int AllTiles { get; set; }
        public int TilesPerPercent { get; set; }
        #endregion

        #region Controls init.
        public System.Windows.Controls.TextBox LogsTB { get; set; }
        #endregion

        public MainWindow()
        {
            LogsTB = LOGS; //init

            InitializeComponent();
        }

        private void START_Click(object sender, RoutedEventArgs e)
        {
            //ASSIGN VALUE FOR PROP HERE
            try
            {
                DirectoryInfo mainDirectory = new DirectoryInfo(MainPath);
                DirectoryInfo[] listOfSubdirectories = mainDirectory.GetDirectories();

                DirectoriesCount = listOfSubdirectories.Length; //Includes count of directories inside main path
                FileInfo[] tilesWH = listOfSubdirectories[0].GetFiles();// Gets first image to get properties like width and height
                System.Drawing.Image img = System.Drawing.Image.FromFile(tilesWH[0].FullName);
                TileWidth = img.Width; //Tile's width
                TileHight = img.Height; //Tile's height
                TilesCount = tilesWH.Length; //Tiles per directory
                TotalWidth = TileWidth * DirectoriesCount; //Total image's width
                TotalHeight = TileHight * TilesCount; //Total image's height

                if (bgw == null)
                {
                    bgw = new BackgroundWorker();
                    bgw.DoWork += new DoWorkEventHandler(bgw_DoWork1);
                    bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgw_RunWorkedCompleted1);
                    bgw.ProgressChanged += new ProgressChangedEventHandler(bgw_ProgressChanged1);

                    bgw.WorkerReportsProgress = true;
                    bgw.WorkerSupportsCancellation = true;
                }
                MainPROGRESSBAR.Value = 0;
                LOGS.Text = ("Uruchomiono proces\n");
                bgw.RunWorkerAsync();
            }
            catch (Exception exc)
            {
                AppendLog("BŁĄD KURWA " + exc);
            }
        }

        private void STOP_Click(object sender, RoutedEventArgs e)
        {
            if (bgw != null && bgw.IsBusy) bgw.CancelAsync();
        }

        private void PathSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string[] directories = Directory.GetDirectories(fbd.SelectedPath);
                    //System.Windows.Forms.MessageBox.Show("Znalezione foldery: " + directories.Length.ToString(), "Message");
                    //TODO: Sprawdz poprawnosc, czy instnieje dany folder lub wywal błąd
                    MainPath = fbd.SelectedPath;
                    PATH.Text = fbd.SelectedPath;
                }
            }
        }

        private void FilePathSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "map"; // Default file name
            dlg.DefaultExt = ".jpg"; // Default file extension
            dlg.Filter = "Pliki graficzne (*.jpg *.jpeg)|*.jpg; *.jpeg"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                OutputFilePath = dlg.FileName;
                OUTPUTFILEPATHTB.Text = OutputFilePath;
                //OUTPUTFILEPATHTB.ScrollToEnd(); //NIE DZIAŁA
            }
        }

        #region BGWorker1

        private void bgw_DoWork1(object sender, DoWorkEventArgs e)
        {
            DirectoryInfo mainDirectory = new DirectoryInfo(MainPath);
            DirectoryInfo[] listOfSubdirectories = mainDirectory.GetDirectories();
            List<Tiles> tilesList = new List<Tiles>();
            int dir = 0;
            int currentHeight = 0;
            foreach (DirectoryInfo directory in listOfSubdirectories)
            {
                if (bgw.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                int iteration = 0;
                FileInfo[] filesInDir = directory.GetFiles();
                foreach (FileInfo file in filesInDir)
                {
                    if (bgw.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    if (iteration == 0) //first element
                    {
                        Tiles tile = new Tiles(file.FullName, TileWidth * dir, 0, directory.Name, file.Name);
                        tilesList.Add(tile);
                        iteration++;
                        currentHeight = TileHight;
                    }
                    else
                    {
                        Tiles tile = new Tiles(file.FullName, TileWidth * dir, currentHeight, directory.Name, file.Name);
                        tilesList.Add(tile);
                        iteration++;
                        currentHeight += TileHight;
                    }
                    string action = "Analizowanie " + directory.Name + "/" + file.Name;
                    bgw.ReportProgress(0, action);//END TEMP
                }
                dir++;
                bgw.ReportProgress(dir, "!");//END TEMP

            }
            Bitmap merged = new Bitmap(TotalWidth, TotalHeight);
            Graphics g = Graphics.FromImage(merged);
            bgw.ReportProgress(tilesList.Count, "Rozpoczynanie łączenia plików");
            int step = 0;
            AllTiles = tilesList.Count;
            TilesPerPercent = AllTiles / 100;
            foreach (Tiles tile in tilesList)
            {
                if (bgw.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                g.DrawImage(tile.PartImage, new System.Drawing.Point(tile.X, tile.Y));
                tile.PartImage.Dispose();
                bgw.ReportProgress(++step, "Dołączono pliki z folderu: " + tile.DirectoryName);
            }
            bgw.ReportProgress(0, "Trwa zapisywanie pliku, proszę czekać...");
            g.Dispose();
            merged.Save(OutputFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            merged.Dispose();
            bgw.ReportProgress(0, "Plik został prawidłowo zapisany");
        }
        private void bgw_ProgressChanged1(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState.ToString() == "Rozpoczynanie łączenia plików")
                MainPROGRESSBAR.Maximum = AllTiles;//e.ProgressPercentage;
            else if (e.UserState.ToString()[0] == 'A')
            {
                AppendLog(e.UserState.ToString());
                SUBPB.Content = e.UserState.ToString();
            }
            else if (e.UserState.ToString()[0] == 'D')
            {
                if (e.ProgressPercentage % TilesCount == 0)
                {
                    SUBPB.Content = e.UserState.ToString();
                    AppendLog(e.UserState.ToString());
                    MainPROGRESSBAR.Value = e.ProgressPercentage;
                }
            }
            else if (e.UserState.ToString()[0] == '!')
            {
                MainPROGRESSBAR.Maximum = DirectoriesCount;//e.ProgressPercentage;
                //AppendLog(e.UserState.ToString());
                //SUBPB.Content = e.UserState.ToString();
                MainPROGRESSBAR.Value = e.ProgressPercentage;
            }
            else
            {
                //MainPROGRESSBAR. zmien kolor jezeli przerwano - nie tutaj
                AppendLog(e.UserState.ToString());
            }
        }

        private void bgw_RunWorkedCompleted1(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled) AppendLog("Operacja została przerwana przez użytkonwika");
            else
            {
                AppendLog("Program zakończył pracę");
                MAINPB.Content = "ZAKOŃCZNO";
            }
        }
        #endregion

        #region Methods

        #endregion


        #region Helper methods
        private void AppendLog(string s)
        {
            LOGS.AppendText(s + "\n");
            LOGS.ScrollToEnd();
        }
        #endregion


    }
}
