using System.Threading.Tasks;
using System.Windows;

namespace MathTex {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {


        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            //this.MainWindow = new winMain();
            //this.MainWindow.Show();

            // Initialize the splash screen
            var splash = new winSplash();
            this.MainWindow = splash;
            splash.Show();
            splash.Focus();

            // Ensure the UI stays responsive
            Task.Factory.StartNew(() => {
                // Loading something
                // DO ...
                splash.InvokeUpdate("Load the main window ...");

                // Return UI thread
                this.Dispatcher.Invoke(() => {
                    this.MainWindow = new winMain(splash);
                    this.MainWindow.Loaded += (object sender, RoutedEventArgs e) => {
                        splash.Close();
                    };
                    this.MainWindow.Show();
                    this.MainWindow.Focus();
                });
            });
        }

    }
}
