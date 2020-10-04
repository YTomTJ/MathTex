using System.Text.RegularExpressions;
using System.Windows;

namespace MathTex {
    /// <summary>
    /// winSplash.xaml 的交互逻辑
    /// </summary>
    public partial class winSplash : Window {

        public winSplash() {
            InitializeComponent();
        }

        public void InvokeUpdate(string msg, double progress = 0) {
            this.Dispatcher.Invoke(() => {
                if(Regex.IsMatch(msg, @"^\s*$")) {
                    msg = "Loading ...";
                }
                bMessgae.Content = msg;
                bProgress.Value += progress;
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown(0);
        }
    }
}
