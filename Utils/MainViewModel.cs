using DevExpress.Mvvm;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MathTex.Utils {

    public class MainViewModel {

        public MainViewModel() {
            new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 1000), DispatcherPriority.Background, (object state, EventArgs e) => {
                RaisePropertiesChanged("UsedMemory");
            }, Dispatcher.CurrentDispatcher).Start();
        }

        #region Exported
        private ICommand _TestCommand;
        public ICommand TestCommand {
            get {
                if(_TestCommand is null) {
                    _TestCommand = new ConditionCommand(() => MessageBox.Show("Test"));
                }
                return _TestCommand;
            }
        }

        private ICommand _ExitCommand;
        public ICommand ExitCommand {
            get {
                if(_ExitCommand is null) {
                    _ExitCommand = new ConditionCommand(() => Application.Current.Shutdown());
                }
                return _ExitCommand;
            }
        }

        public long UsedMemory => GC.GetTotalMemory(true) / 1024;

        public double FontSize { get; set; }

        public double Zoom { get; set; }
        #endregion
    }
}
