using System.Windows;

namespace FileIndexer.DemoApp
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (sender, args) => DataContext = new DemoAppViewModel(this);
        }
        
    }
}