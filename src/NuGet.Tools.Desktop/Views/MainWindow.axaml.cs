using Avalonia.Controls;

using System.Reflection;

namespace NuGet.Tools.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Title += $"\t v{Assembly.GetExecutingAssembly().GetName().Version}";
        }
    }
}