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
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Diagnostics;

namespace RemnantSaveManager
{
    /// <summary>
    /// Interaction logic for SaveAnalyzer.xaml
    /// </summary>
    public partial class SaveAnalyzer : Window
    {
        private MainWindow mainWindow;
        public bool ActiveSave { get; set; }
        private List<RemnantCharacter> listCharacters;
        private AnalyzerColor analyzerColor;
        public SaveAnalyzer(MainWindow mw)
        {
            InitializeComponent();

            mainWindow = mw;

            listCharacters = new List<RemnantCharacter>();

            cmbCharacter.ItemsSource = listCharacters;

            analyzerColor = new AnalyzerColor();
            analyzerColor.backgroundColor = (Color)ColorConverter.ConvertFromString("#343a40");
            analyzerColor.textColor = (Color)ColorConverter.ConvertFromString("#f8f9fa");
            analyzerColor.headerBackgroundColor = (Color)ColorConverter.ConvertFromString("#70a1ff");
            analyzerColor.borderColor = (Color)ColorConverter.ConvertFromString("#dddddd");

            dgCampaign.VerticalGridLinesBrush = new SolidColorBrush(analyzerColor.borderColor);
            dgCampaign.HorizontalGridLinesBrush = new SolidColorBrush(analyzerColor.borderColor);
            dgCampaign.RowBackground = new SolidColorBrush(analyzerColor.backgroundColor);
            dgCampaign.RowHeaderStyle = new Style(typeof(DataGridRowHeader));
            dgCampaign.RowHeaderStyle.Setters.Add(new Setter(DataGridRowHeader.BackgroundProperty, new SolidColorBrush(analyzerColor.backgroundColor)));
            dgCampaign.ColumnHeaderStyle = new Style(typeof(DataGridColumnHeader));
            dgCampaign.ColumnHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(analyzerColor.headerBackgroundColor)));

            dgAdventure.VerticalGridLinesBrush = new SolidColorBrush(analyzerColor.borderColor);
            dgAdventure.HorizontalGridLinesBrush = new SolidColorBrush(analyzerColor.borderColor);
            dgAdventure.RowBackground = new SolidColorBrush(analyzerColor.backgroundColor);
            dgAdventure.RowHeaderStyle = new Style(typeof(DataGridRowHeader));
            dgAdventure.RowHeaderStyle.Setters.Add(new Setter(DataGridRowHeader.BackgroundProperty, new SolidColorBrush(analyzerColor.backgroundColor)));
            dgAdventure.ColumnHeaderStyle = new Style(typeof(DataGridColumnHeader));
            dgAdventure.ColumnHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(analyzerColor.headerBackgroundColor)));

            lblCredits.Content = "Thanks to /u/hzla00 for the original online implementation.\n\nLots of code used here was adapted from his original javascript (as was the styling!).";

            txtMissingItems.BorderThickness = new Thickness(0);
        }

        public void LoadData(List<RemnantCharacter> chars)
        {
            int selectedChar = cmbCharacter.SelectedIndex;
            listCharacters = chars;
            /*Console.WriteLine("Loading characters in analyzer: " + listCharacters.Count);
            foreach (CharacterData cd in listCharacters)
            {
                Console.WriteLine("\t" + cd);
            }*/
            cmbCharacter.ItemsSource = listCharacters;
            if (selectedChar == -1 && listCharacters.Count > 0) selectedChar = 0;
            if (selectedChar > -1 && listCharacters.Count > selectedChar) cmbCharacter.SelectedIndex = selectedChar;
            cmbCharacter.IsEnabled = (listCharacters.Count > 1);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.ActiveSave)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void CmbCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCharacter.Items.Count > 0 && cmbCharacter.SelectedIndex > -1)
            {
                dgCampaign.ItemsSource = listCharacters[cmbCharacter.SelectedIndex].CampaignEvents;
                if (listCharacters[cmbCharacter.SelectedIndex].AdventureEvents.Count > 0)
                {
                    ((TabItem)tabAnalyzer.Items[1]).IsEnabled = true;
                    dgAdventure.ItemsSource = listCharacters[cmbCharacter.SelectedIndex].AdventureEvents;
                } else
                {
                    ((TabItem)tabAnalyzer.Items[1]).IsEnabled = false;
                    tabAnalyzer.SelectedIndex = 0;
                }
                //Console.WriteLine(listCharacters[cmbCharacter.SelectedIndex].ToFullString());
                txtMissingItems.Text = string.Join("\n", listCharacters[cmbCharacter.SelectedIndex].GetMissingItems());
            }
        }

        private void DgCampaign_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void DgAdventure_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void logMessage(string message) {
            mainWindow.logMessage(this.Title+": "+message);
        }

        struct AnalyzerColor
        {
            internal Color backgroundColor;
            internal Color textColor;
            internal Color headerBackgroundColor;
            internal Color borderColor;
        }
        private void autoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.HeaderStyle = new Style(typeof(DataGridColumnHeader));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(analyzerColor.headerBackgroundColor)));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Colors.White)));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(12)));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontSizeProperty, 24.0));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, new SolidColorBrush(analyzerColor.borderColor)));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(1)));

            e.Column.CellStyle = new Style(typeof(DataGridCell));
            e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(analyzerColor.backgroundColor)));
            e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(4)));
            //e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, new SolidColorBrush(borderColor)));
            //e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(1)));
            if (e.Column.Header.Equals("MissingItems"))
            {
                e.Column.Header = "Missing Items";
                e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new SolidColorBrush(Colors.Red)));
            } else
            {
                e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.FontSizeProperty, 18.0));
                e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new SolidColorBrush(analyzerColor.textColor)));
            }
        }

        private void DgCampaign_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            autoGeneratingColumn(sender, e);
        }

        private void DgAdventure_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            autoGeneratingColumn(sender, e);
        }

        private void LblCredits_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://hzla.github.io/Remnant-World-Analyzer/");
        }
    }
}
