using System.Windows;

namespace MarkSnap
{
    public partial class SettingsWindow : Window
    {
        public string SelectedTheme { get; private set; }

        public SettingsWindow(string currentTheme)
        {
            InitializeComponent();
            SelectedTheme = currentTheme;

            // Set the radio button based on current theme
            switch (currentTheme)
            {
                case "Light":
                    LightThemeRadio.IsChecked = true;
                    break;
                case "Dark":
                    DarkThemeRadio.IsChecked = true;
                    break;
                default:
                    SystemThemeRadio.IsChecked = true;
                    break;
            }
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (SystemThemeRadio.IsChecked == true)
                SelectedTheme = "System";
            else if (LightThemeRadio.IsChecked == true)
                SelectedTheme = "Light";
            else if (DarkThemeRadio.IsChecked == true)
                SelectedTheme = "Dark";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
