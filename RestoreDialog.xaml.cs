using System.Globalization;
using System.Windows;

namespace RemnantSaveManager
{
    /// <summary>
    /// Interaction logic for RestoreDialog.xaml
    /// </summary>
    public partial class RestoreDialog : Window
    {
        private SaveBackup _saveBackup;
        private RemnantSave _activeSave;
        public string Result { get; set; }
        public RestoreDialog(MainWindow @mw, SaveBackup @sb, RemnantSave @as)
        {
            InitializeComponent();
            this.txtSave.Content = $"Save Name:\t{sb.Name}\nSave Date:\t{sb.SaveDate.ToString(CultureInfo.CurrentCulture)}";
            this._saveBackup = sb;
            this._activeSave = @as;
        }

        private void btnCharacter_Click(object sender, RoutedEventArgs e)
        {
            Result = "Character";
            DialogResult = true;
            this.Close();
        }

        private void btnWorld_Click(object sender, RoutedEventArgs e)
        {

            if (this._saveBackup.Save.Characters.Count != this._activeSave.Characters.Count)
            {
                MessageBoxResult confirmResult = MessageBox.Show("The active save has a different number of characters than the backup worlds you are restoring. This may result in unexpected behavior. Proceed?",
                                     "Character Mismatch", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (confirmResult == MessageBoxResult.No)
                {
                    DialogResult = false;
                    this.Close();
                    return;
                }
            }

            Result = "World";
            DialogResult = true;
            this.Close();
        }

        private void btnAll_Click(object sender, RoutedEventArgs e)
        {
            Result = "All";
            DialogResult = true;
            this.Close();
        }
    }
}
