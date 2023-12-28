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
using System.IO;
using System.Collections.ObjectModel;

namespace RemnantSaveManager
{
    /// <summary>
    /// Interaction logic for SaveAnalyzer.xaml
    /// </summary>
    public partial class SaveAnalyzer : Window
    {
        private MainWindow mainWindow;
        public bool ActiveSave { get; set; }
        private ObservableCollection<RemnantCharacter> listCharacters;
        private AnalyzerColor analyzerColor;
        private Dictionary<string,Dictionary<string,double>> columnWidths;
        private bool initialized;
        public SaveAnalyzer(MainWindow mw)
        {
            initialized = false;
            InitializeComponent();

            mainWindow = mw;

            listCharacters = new ObservableCollection<RemnantCharacter>();

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

            columnWidths = new Dictionary<string, Dictionary<string, double>>();
            columnWidths.Add(dgCampaign.Name, new Dictionary<string, double>());
            columnWidths.Add(dgAdventure.Name, new Dictionary<string, double>());

            sliderSize.Value = Properties.Settings.Default.AnalyzerFontSize;
            treeMissingItems.FontSize = sliderSize.Value - 4;
            lblCredits.FontSize = sliderSize.Value;
            initialized = true;
            TreeViewItem nodeNormal = new TreeViewItem();
            nodeNormal.Header = "Normal";
            nodeNormal.Foreground = treeMissingItems.Foreground;
            nodeNormal.IsExpanded = Properties.Settings.Default.NormalExpanded;
            nodeNormal.Expanded += GameType_CollapsedExpanded;
            nodeNormal.Collapsed += GameType_CollapsedExpanded;
            nodeNormal.Tag = "mode";
            TreeViewItem nodeHardcore = new TreeViewItem();
            nodeHardcore.Header = "Hardcore";
            nodeHardcore.Foreground = treeMissingItems.Foreground;
            nodeHardcore.IsExpanded = Properties.Settings.Default.HardcoreExpanded;
            nodeHardcore.Expanded += GameType_CollapsedExpanded;
            nodeHardcore.Collapsed += GameType_CollapsedExpanded;
            nodeHardcore.Tag = "mode";
            TreeViewItem nodeSurvival = new TreeViewItem();
            nodeSurvival.Header = "Survival";
            nodeSurvival.Foreground = treeMissingItems.Foreground;
            nodeSurvival.IsExpanded = Properties.Settings.Default.SurvivalExpanded;
            nodeSurvival.Expanded += GameType_CollapsedExpanded;
            nodeSurvival.Collapsed += GameType_CollapsedExpanded;
            nodeSurvival.Tag = "mode";
            treeMissingItems.Items.Add(nodeNormal);
            treeMissingItems.Items.Add(nodeHardcore);
            treeMissingItems.Items.Add(nodeSurvival);
        }

        private void GameType_CollapsedExpanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem modeItem = (TreeViewItem)sender;
            if (modeItem.Header.ToString().Contains("Normal")) {
                Properties.Settings.Default.NormalExpanded = modeItem.IsExpanded;
            }
            else if (modeItem.Header.ToString().Contains("Hardcore"))
            {
                Properties.Settings.Default.HardcoreExpanded = modeItem.IsExpanded;
            }
            else if (modeItem.Header.ToString().Contains("Survival"))
            {
                Properties.Settings.Default.SurvivalExpanded = modeItem.IsExpanded;
            }
            Properties.Settings.Default.Save();
        }

        public void LoadData(List<RemnantCharacter> chars)
        {
            int selectedChar = cmbCharacter.SelectedIndex;
            listCharacters.Clear();
            chars.ForEach(c => listCharacters.Add(c));
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
            if (cmbCharacter.SelectedIndex == -1 && listCharacters.Count > 0) return;
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
                    if (tabAnalyzer.SelectedIndex == 1) tabAnalyzer.SelectedIndex = 0;
                }
                txtMissingItems.Text = string.Join("\n", listCharacters[cmbCharacter.SelectedIndex].GetMissingItems());

                foreach (TreeViewItem item in treeMissingItems.Items)
                {
                    item.Items.Clear();
                }
                foreach (RemnantItem rItem in listCharacters[cmbCharacter.SelectedIndex].GetMissingItems())
                {
                    TreeViewItem item = new TreeViewItem();
                    item.Header = rItem.ItemName;
                    if (!rItem.ItemNotes.Equals("")) item.ToolTip = rItem.ItemNotes;
                    item.Foreground = treeMissingItems.Foreground;
                    item.ContextMenu = this.treeMissingItems.Resources["ItemContext"] as System.Windows.Controls.ContextMenu;
                    item.Tag = "item";
                    TreeViewItem modeNode = ((TreeViewItem)treeMissingItems.Items[(int)rItem.ItemMode]);
                    TreeViewItem itemTypeNode = null;
                    foreach (TreeViewItem typeNode in modeNode.Items)
                    {
                        if (typeNode.Header.ToString().Equals(rItem.ItemType))
                        {
                            itemTypeNode = typeNode;
                            break;
                        }
                    }
                    if (itemTypeNode == null)
                    {
                        itemTypeNode = new TreeViewItem();
                        itemTypeNode.Header = rItem.ItemType;
                        itemTypeNode.Foreground = treeMissingItems.Foreground;
                        itemTypeNode.IsExpanded = true;
                        itemTypeNode.ContextMenu = this.treeMissingItems.Resources["ItemGroupContext"] as System.Windows.Controls.ContextMenu;
                        itemTypeNode.Tag = "type";
                        ((TreeViewItem)treeMissingItems.Items[(int)rItem.ItemMode]).Items.Add(itemTypeNode);
                    }
                    itemTypeNode.Items.Add(item);
                }
            }
        }

        private void dgBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
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
            double fontSize = sliderSize.Value;
            e.Column.HeaderStyle = new Style(typeof(DataGridColumnHeader));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(analyzerColor.headerBackgroundColor)));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Colors.White)));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(8,4,8,4)));
            e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontSizeProperty, fontSize));
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
                e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.FontSizeProperty, ((fontSize / 3) * 2)));
                if (Properties.Settings.Default.MissingItemColor.Equals("Red"))
                {
                    e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new SolidColorBrush(Colors.Red)));
                } else
                {
                    e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new SolidColorBrush(analyzerColor.textColor)));
                }
            } else if (e.Column.Header.Equals("PossibleItems"))
            {
                if (!Properties.Settings.Default.ShowPossibleItems)
                {
                    e.Cancel = true;
                    return;
                }
                e.Column.Header = "All Items";
                e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.FontSizeProperty, ((fontSize / 3) * 2)));
                if (Properties.Settings.Default.MissingItemColor.Equals("Red"))
                {
                    e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new SolidColorBrush(Colors.Red)));
                }
                else
                {
                    e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new SolidColorBrush(analyzerColor.textColor)));
                }
            }
            else
            {
                e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.FontSizeProperty, fontSize));
                e.Column.CellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new SolidColorBrush(analyzerColor.textColor)));
            }

            /*DataGrid dg = (DataGrid)sender;
            if (columnWidths[dg.Name].ContainsKey(e.Column.Header.ToString()))
            {
                if (columnWidths[dg.Name][e.Column.Header.ToString()] > -1)
                {
                    e.Column.HeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.WidthProperty, columnWidths[dg.Name][e.Column.Header.ToString()]));
                }
            }*/
        }

        private void LblCredits_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://hzla.github.io/Remnant-World-Analyzer/");
        }

        private void dgCampaign_LayoutUpdated(object sender, EventArgs e)
        {
            saveColumnWidth(dgCampaign);
        }
        private void dgAdventure_LayoutUpdated(object sender, EventArgs e)
        {
            saveColumnWidth(dgAdventure);
        }

        private void saveColumnWidth(DataGrid dg)
        {
            /*if (dg.Columns.Count > 0)
            {
                for (int i=0; i < dg.Columns.Count; i++)
                {
                    if (!dg.Columns[i].Width.ToString().Equals("Auto"))
                    {
                        columnWidths[dg.Name][dg.Columns[i].Header.ToString()] = dg.Columns[i].Width.Value;
                    }
                }
                dg.UpdateLayout();
            }*/
        }

        private void autoGeneratedColumns(object sender, EventArgs e)
        {
            /*DataGrid dg = (DataGrid)sender;
            if (columnWidths[dg.Name].Count == 0)
            {
                for (int i=0; i < dg.Columns.Count; i++)
                {
                    if (dg.Columns[i].Width.ToString().Equals("Auto"))
                    {
                        columnWidths[dg.Name].Add(dg.Columns[i].Header.ToString(), -1);
                    } else
                    {
                        columnWidths[dg.Name].Add(dg.Columns[i].Header.ToString(), dg.Columns[i].Width.Value);
                    }
                }
            } */
        }

        private void sliderSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (initialized)
            {
                Properties.Settings.Default.AnalyzerFontSize = sliderSize.Value;
                Properties.Settings.Default.Save();

                dgCampaign.ItemsSource = null;
                dgAdventure.ItemsSource = null;
                CmbCharacter_SelectionChanged(null, null);
                treeMissingItems.FontSize = sliderSize.Value - 4;
                lblCredits.FontSize = sliderSize.Value;
            }

        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            string name = (string)((TabItem)tabAnalyzer.SelectedItem).Header;
            saveFileDialog.FileName = name + ".md";
            System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                using (System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog.OpenFile())
                using (System.IO.StreamWriter sw = new StreamWriter(fs))
                {
                    switch (tabAnalyzer.SelectedIndex)
                    {
                        case 0:
                            sw.Write(ExportCampaign(isAdventure: false));
                            break;
                        case 1:
                            sw.Write(ExportCampaign(isAdventure: true));
                            break;
                        case 3:
                            sw.Write(ExportMissingItems());
                            break;
                        case 4:
                            sw.Write(ExportCredits());
                            break;
                        default:
                            throw new Exception("Tab does not exist");
                    }
                }
            }
        }

        private string FormatItems(string header, string items)
        {
            StringBuilder sb = new StringBuilder();
            if (string.IsNullOrEmpty(items))
            {
                sb.AppendLine($"- **{header}** - None");
            }
            else
            {
                sb.AppendLine($"- **{header}:**");
                foreach (string item in items.Split('\n'))
                {
                    sb.AppendLine($"  - {item}");
                }
            }
            return sb.ToString();
        }

        private string DumpEvents(IEnumerable<RemnantWorldEvent> events)
        {
            StringBuilder sb = new StringBuilder();
            foreach(RemnantWorldEvent eItem in events)
            {
                sb.AppendLine($"##### {eItem.Name}");
                sb.AppendLine($"- **Type** - {eItem.Type}");
                sb.Append(FormatItems("Missing Items", eItem.MissingItems));
                if (Properties.Settings.Default.ShowPossibleItems)
                {
                    sb.Append(FormatItems("Possible Items", eItem.PossibleItems));
                }
            }
            return sb.ToString();
        }
        private string ExportCampaign(bool isAdventure = false)
        {
            StringBuilder sb = new StringBuilder();

            List<RemnantWorldEvent> events = isAdventure ?
                listCharacters[cmbCharacter.SelectedIndex].AdventureEvents :
                listCharacters[cmbCharacter.SelectedIndex].CampaignEvents;
            foreach(var region in events.GroupBy(x => x.Location.Split(':')[0].Trim()))
            {
                sb.AppendLine();
                sb.AppendLine($"## {region.Key}");
                sb.Append(DumpEvents(region.Where(x => x.Location.Split(':').Length == 1)));
                foreach (var zone in region.Where(x=>x.Location.Split(':').Length > 1).GroupBy(x => x.Location.Split(':')[1].Trim()))
                {
                    sb.AppendLine($"### {zone.Key}");
                    sb.Append(DumpEvents(zone.Where(x => x.Location.Split(':').Length == 2)));
                    foreach (var locality in zone.Where(x => x.Location.Split(':').Length > 2).GroupBy(x => x.Location.Split(':')[2].Trim()))
                    {
                        sb.AppendLine($"#### {locality.Key}");
                        sb.Append(DumpEvents(locality));
                    }
                }
            }
            return sb.ToString().TrimStart('\r','\n');
        }
        private string ExportMissingItems()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var mode in listCharacters[cmbCharacter.SelectedIndex].GetMissingItems().GroupBy(x => x.ItemMode))
            {
                sb.AppendLine($"## {mode.Key}");
                foreach(var type in mode.GroupBy(x=> x.ItemType))
                {
                    sb.AppendLine($"- {type.Key}");
                    foreach (var item in type)
                    {
                        if (string.IsNullOrEmpty(item.ItemNotes))
                        {
                            sb.AppendLine($"  - {item.ItemName}");
                        }
                        else
                        {
                            sb.AppendLine($"  - {item.ItemName} {{{item.ItemNotes}}}");
                        }
                    }
                }
            }
            return sb.ToString();
        }
        private string ExportCredits()
        {
            return (string)lblCredits.Content;
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            switch (tabAnalyzer.SelectedIndex)
            {
                case 0:
                    Clipboard.SetText(ExportCampaign(isAdventure: false));
                    break;
                case 1:
                    Clipboard.SetText(ExportCampaign(isAdventure: true));
                    break;
                case 3:
                    Clipboard.SetText(ExportMissingItems());
                    break;
                case 4:
                    Clipboard.SetText(ExportCredits());
                    break;
                default:
                    throw new Exception("Tab does not exist");
            }

            MessageBox.Show("Content copied.");
        }

        private string GetTreeItem(TreeViewItem item)
        {
            if ((string)item.Tag == "item") return item.Header.ToString();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(item.Header.ToString() + ":");
            foreach (TreeViewItem i in item.Items)
            {
                sb.AppendLine("\t- " + GetTreeItem(i));
            }
            return sb.ToString();
        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mnu = sender as MenuItem;
            TreeViewItem treeItem = ((ContextMenu)mnu?.Parent)?.PlacementTarget as TreeViewItem;

            Clipboard.SetText(GetTreeItem(treeItem));
        }

        private void SearchItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mnu = sender as MenuItem;
            TreeViewItem treeItem = ((ContextMenu)mnu?.Parent)?.PlacementTarget as TreeViewItem;
            var type = ((TreeViewItem)treeItem?.Parent)?.Header.ToString();
            var itemname = treeItem?.Header.ToString();

            if (type == "Armor")
            {
                itemname = itemname.Substring(0, itemname.IndexOf("(")) + "Set";
            }

            System.Diagnostics.Process.Start($"https://remnantfromtheashes.wiki.fextralife.com/{itemname}");
        }
    }
}
