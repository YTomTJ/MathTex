using Fluent;
using ImageMagick;
using MathTex.Images;
using MathTex.Properties;
using MathTex.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;
using Clipboard = System.Windows.Forms.Clipboard;

namespace MathTex {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class winMain : Fluent.RibbonWindow, INotifyPropertyChanged {

        private static readonly int ForceDelayTime = 1500;
        private static readonly int MaxClipboardSize = 10;
        //private string formulaFontName;
        //private double formulaFontSize;
        private List<string> myClipboard = new List<string>();  // Only recent 10 record

        private string currentSvgText = null;
        private Tuple<int, int> currentSvgSize;
        private string currentLatexText = null;
        private bool IsRendering = false;

        private CancellationTokenSource TaskCancel;
        private bool IsForcing = false;

        public winMain(winSplash splash = null) {
            InitializeComponent();
            DataContext = this;

            if(splash != null) {
                splash.InvokeUpdate("Loading application settings ...");
            }
            _FormulaZoom = Settings.Default.FormulaScale;
            ClipBgColor.SelectedColor = Settings.Default.BgColorToClip;


            if(splash != null) {
                splash.InvokeUpdate("Loading V8 javascipt engine ...");
            }
            _ = MathjaxParser.GetInstance();

            if(splash != null) {
                splash.InvokeUpdate("Loading application resouces ...");
            }
            //InitializeFontSelector();
            //formulaFontName = ((FontFamily)eFontFamily.GetFirstCheckedItem().Tag).ToString();
            //formulaFontSize = (double)eFontSize.EditValue;
            DataObject.AddCopyingHandler(txtInputFomula, OnInputFomulaCopying);
            if(Clipboard.ContainsText()) {
                myClipboard.Add(Clipboard.GetText());
            }

            if(splash != null) {
                splash.InvokeUpdate("Loading latex resources ...");
            }
            LoadElement();

            if(splash != null) {
                splash.InvokeUpdate("Show window ...");
            }
            AddNotifiactions();

            Application.Current.Exit += OnApplicationExit;
        }

        private void OnApplicationExit(object sender, ExitEventArgs e) {
            Settings.Default.FormulaScale = FormulaZoom;
            Settings.Default.BgColorToClip = ClipBgColor.SelectedColor.Value;
            Settings.Default.Save();
        }

        #region Formula
        #region Formula Operation
        /// <summary>
        /// Convert latex formula and render it.
        /// </summary>
        public void ConvertFormula() {

            if(IsRendering)
                return;

            PreConvert();

            var dump = outputMath.Source;
            outputMath.Source = null;

            // Get the formula latex.
            var tex = txtInputFomula.Text.Trim();
            if(Regex.IsMatch(tex, @"^\s*$")) {
                PostConvert("[ERROR] Empty formula.");
                return;
            }

            // Record last text for not converting.
            if(!IsForcing && currentLatexText == tex) {
                PostConvert($"[REJECT] Text not changed.\nTry angin in {ForceDelayTime / 1000.0}s to force to convert.");
                // Wait sometime for force converting.
                TaskCancel = new CancellationTokenSource();
                var x = new Thread(new ThreadStart(ForceConvertEnd));
                x.IsBackground = true;
                x.Start();
                outputMath.Source = dump;
                return;
            }
            // Stop wait for force converting.
            if(TaskCancel != null)
                TaskCancel.Cancel();
            currentLatexText = tex;
            IsForcing = false;

            // Implement mathjax parser.
            string svgtext;
            try {
                svgtext = MathjaxParser.GetInstance().Run(tex);
            } catch(Exception e) {
                PostConvert("[ERROR] Incorrect formula:\n" + e);
                return;
            }

            // Render SvgImage to ImageSource for showing
            // TODO: I still wonder how to use async well 
            Render(svgtext);
        }

        private void PreConvert() {
            txtOutpuInfo.Text = "";
            txtInputFomula.IsEnabled = false;
            IsRendering = true;
        }

        private void PostConvert(string info, bool pending = false) {
            txtOutpuInfo.Text = pending ? txtOutpuInfo.Text + info : info;
            txtInputFomula.IsEnabled = true;
            IsRendering = false;
        }

        private async void ForceConvertEnd() {
            IsForcing = true;
            try {
                await Task.Delay(ForceDelayTime, TaskCancel.Token);
                this.Dispatcher.Invoke(() => {
                    PostConvert("");
                    IsForcing = false;
                });
            } catch {
            }
        }

        private void Render(string svgtext) {
            if(svgtext is null)
                return;
            // Tranfer to SvgImage.
            try {
                var stp = Stopwatch.StartNew();
                var bmp = SvgRenderToBitmap(svgtext, FormulaZoom);
                stp.Stop();

                this.Dispatcher.Invoke(() => {
                    outputMath.Source = ImageHelper.GetImageSource(bmp);
                    currentSvgText = svgtext;
                    currentSvgSize = new Tuple<int, int>(bmp.Width, bmp.Height);
                    PostConvert($"[INFO] Succeed. (using {stp.ElapsedTicks * 1000F / Stopwatch.Frequency} ms)", true);
                });
            } catch(Exception e) {
                PostConvert("[ERROR] Render failed:\n" + e);
                return;
            }
        }

        /// <summary>
        /// Redner svg to iamge.
        /// </summary>
        private static System.Drawing.Bitmap SvgRenderToBitmap(string svgtext, double zoom) {
            if(svgtext is null)
                return null;
            if(zoom <= 0)
                zoom = 1.0;
            var svg = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(svgtext);
            svg.Width *= (float)zoom;
            svg.Height *= (float)zoom;
            return svg.Draw();
        }

        /// <summary>
        /// Convert SVG to other image type.
        /// </summary>
        private static MemoryStream ImagickConvert(string svgtext, int width = 0, int height = 0, 
            MagickColor color = null, MagickFormat type = MagickFormat.Png) {
            if(svgtext is null)
                return null;
            var set = new MagickReadSettings();
            set.Width = width;
            set.Height = height;
            set.BackgroundColor = color ?? MagickColors.Transparent;
            set.Format = MagickFormat.Svg;
            var svg = new MagickImage(Encoding.UTF8.GetBytes(svgtext), set);
            var ms = new MemoryStream();
            svg.Write(ms, type);
            return ms;
        }

        public ICommand cConvertFormula { get => new ConditionCommand(ConvertFormula); }
        #endregion Formula Operation

        #region Symbol Operation
        private void LoadElement() {

            var rgb = (RibbonGroupBox)this.FindName("ribGroupFormula");
           
            foreach(var label in MathTex.Images.SymbolImages.GetInstance().SymbolGroups) {
                // Add button
                var button = new Fluent.DropDownButton();
                button.Header = label.Name;
                button.SizeDefinition = "Large, Large, Middle";
                button.LargeIcon = new UriImage($"/icons/mathtex/{label.IconName}.png").GetImageSource();
                button.Icon = button.LargeIcon;
                rgb.Items.Add(button);

                var menu = new ScrollViewer();
                menu.MaxWidth = 640;
                menu.MaxHeight = 320;
                menu.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                menu.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                menu.Content = new StackPanel();
                button.Items.Add(menu);

                foreach(var item in label.SubGroups) {

                    // Make new group of symbils
                    var group = new WrapPanel();
                    group.Tag = item.ToString();
                    
                    foreach(var sym in item.Symbols) {
                        var _symbutton = new System.Windows.Controls.Button();
                        _symbutton.Style = FindResource("FormulaItemStyle") as Style;
                        _symbutton.Content = new System.Windows.Controls.Image() {
                            Stretch = Stretch.None,
                            Source = new ImageSourceConverter().ConvertFrom(sym.PngData) as BitmapSource,
                        };
                        _symbutton.ToolTip = sym.Name;
                        _symbutton.Tag = sym;
                        _symbutton.Click += _symbutton_Click;
                        ;
                        group.Children.Add(_symbutton);
                    }

                    var cap = new Label() { Content = group.Tag as string };
                    cap.Background = Brushes.DarkGray;
                    cap.FontWeight = FontWeights.Bold;
                    cap.Padding = new Thickness(1);
                    (menu.Content as StackPanel).Children.Add(cap);
                    (menu.Content as StackPanel).Children.Add(group);
                }
            }
        }

        private void _symbutton_Click(object sender, RoutedEventArgs e) {
            if(IsRendering)
                return;
            var sym = (sender as System.Windows.Controls.Button).Tag as LatexSymbol;
            var start = txtInputFomula.SelectionStart;
            txtInputFomula.Text = txtInputFomula.Text.Insert(start, sym.Name);
            txtInputFomula.Focus();
            txtInputFomula.SelectionStart = start + sym.Name.Length;
        }
        #endregion Symbol Operation
        #endregion Formula


        //#region FontSettings
        //void InitializeFontSelector() {
        //    foreach(FontFamily fontFamily in (new DecimatedFontFamilies()).Items) {
        //        FontFamilyGalleryGroup.Items.Add(CreateItem(fontFamily, CreateImage(fontFamily)));
        //    }
        //    ((ComboBoxEditSettings)eFontSize.EditSettings).ItemsSource = FontSizes.Items;
        //    eFontFamily.FirstCheckedItem = FontFamilyGalleryGroup.Items[0]; // Arial
        //    eFontSize.EditValue = FontSizes.Items[14];  // 8
        //}

        //void FontFamilyGallery_ItemChecked(object sender, GalleryItemEventArgs e) {
        //    formulaFontName = ((FontFamily)e.Item.Tag).ToString();
        //}

        //void eFontSize_EditValueChanged(object sender, RoutedEventArgs e) {
        //    formulaFontSize = (double)eFontSize.EditValue;
        //}

        //#region FontSettings_Creator
        //FormattedText fmtText = null;
        //FormattedText createFormattedText(FontFamily fontFamily) {
        //    return new FormattedText("Aa", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
        //        new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
        //        18, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        //}

        //ImageSource CreateImage(FontFamily fontFamily) {
        //    const double DimensionSize = 24;
        //    const double HalfDimensionSize = DimensionSize / 2d;
        //    DrawingVisual v = new DrawingVisual();
        //    DrawingContext c = v.RenderOpen();
        //    c.DrawRectangle(Brushes.White, null, new Rect(0, 0, DimensionSize, DimensionSize));
        //    fmtText ??= createFormattedText(fontFamily);
        //    fmtText.SetFontFamily(fontFamily);
        //    fmtText.TextAlignment = TextAlignment.Center;
        //    double verticalOffset = (DimensionSize - fmtText.Baseline) / 2d;
        //    c.DrawText(fmtText, new System.Windows.Point(HalfDimensionSize, verticalOffset));
        //    c.Close();

        //    RenderTargetBitmap rtb = new RenderTargetBitmap((int)DimensionSize, (int)DimensionSize, 96, 96, PixelFormats.Pbgra32);
        //    rtb.Render(v);
        //    return rtb;
        //}

        //GalleryItem CreateItem(FontFamily fontFamily, ImageSource image) {
        //    GalleryItem item = new GalleryItem();
        //    item.Glyph = image;
        //    item.Caption = fontFamily.ToString();
        //    item.Tag = fontFamily;
        //    return item;
        //}
        //#endregion FontSettings_Creator

        //#region FontSettings_Classes
        //public class FontSizes {
        //    public static double[] Items {
        //        get {
        //            return new double[] {
        //                3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        //                16, 17, 18, 19, 20, 22, 24, 26, 28, 30,
        //                32, 34, 36, 38, 40, 44, 48, 52, 56, 60,
        //                64, 68, 72, 76, 80, 88, 96, 104, 112, 120,
        //                128, 136, 144
        //            };
        //        }
        //    }
        //}

        //public class DecimatedFontFamilies : FontFamilies {
        //    const int DecimationFactor = 5;

        //    public override ObservableCollection<FontFamily> Items {
        //        get {
        //            ObservableCollection<FontFamily> res = new ObservableCollection<FontFamily>();
        //            for(int i = 0; i < ItemsCore.Count; i++) {
        //                if(i % DecimationFactor == 0)
        //                    res.Add(ItemsCore[i]);
        //            }
        //            return res;
        //        }
        //    }
        //}

        //public class FontFamilies {
        //    static ObservableCollection<FontFamily> items;
        //    protected static ObservableCollection<FontFamily> ItemsCore {
        //        get {
        //            if(items == null) {
        //                items = new ObservableCollection<FontFamily>();
        //                foreach(FontFamily fam in Fonts.SystemFontFamilies) {
        //                    if(!IsValidFamily(fam))
        //                        continue;
        //                    items.Add(fam);
        //                }
        //            }
        //            return items;
        //        }
        //    }
        //    public static bool IsValidFamily(FontFamily fam) {
        //        foreach(Typeface f in fam.GetTypefaces()) {
        //            GlyphTypeface g = null;
        //            try {
        //                if(f.TryGetGlyphTypeface(out g))
        //                    if(g.Symbol)
        //                        return false;
        //            } catch(Exception) {
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //    public virtual ObservableCollection<FontFamily> Items {
        //        get {
        //            ObservableCollection<FontFamily> res = new ObservableCollection<FontFamily>();
        //            foreach(FontFamily fm in ItemsCore) {
        //                res.Add(fm);
        //            }
        //            return res;
        //        }
        //    }
        //}
        //#endregion FontSettings_Classes
        //#endregion FontSettings


        #region Text Operations
        public void InputPaste() {
            txtInputFomula.Paste();
            txtInputFomula.Focus();
        }

        public void InputCut() {
            txtInputFomula.Cut();
            txtInputFomula.Focus();
        }

        public void InputCopy() {
            txtInputFomula.Copy();
            txtInputFomula.SelectionStart = txtInputFomula.SelectionStart + txtInputFomula.SelectionLength;
            txtInputFomula.SelectionLength = 0;
            txtInputFomula.Focus();
        }

        public void InputClear() {
            txtInputFomula.Clear();
            txtOutpuInfo.Clear();
            currentSvgText = null;
            currentSvgSize = null;
            currentLatexText = null;
            outputMath.Source = null;
            txtInputFomula.Focus();
        }

        public ICommand cInputPaste { get => new ConditionCommand(InputPaste); }
        public ICommand cInputCut { get => new ConditionCommand(InputCut); }
        public ICommand cInputCopy { get => new ConditionCommand(InputCopy); }
        public ICommand cInputClear { get => new ConditionCommand(InputClear); }


        #region Clipboard Operations
        private void OnInputFomulaCopying(object sender, DataObjectCopyingEventArgs e) {
            while(myClipboard.Count >= MaxClipboardSize) {
                myClipboard.RemoveAt(myClipboard.Count - 1);
            }
            myClipboard.Insert(0, (string)e.DataObject.GetData(typeof(string)));
        }

        private void PasteItems_ContextMenuOpening(object sender, EventArgs e) {
            int i = 0;
            foreach(var rec in myClipboard) {
                var item = new System.Windows.Controls.Button() {
                    ToolTip = $"Item-{i++}",
                    Tag = rec,
                    Style = this.FindResource("PasteItemStyle") as Style,
                };
                item.Click += OnPasteItemClick;
                PasteItems.Children.Add(item);
            }
        }

        private void PasteItems_ContextMenuClosing(object sender, EventArgs e) {
            PasteItems.Children.Clear();
        }

        private void OnPasteItemClick(object sender, RoutedEventArgs e) {
            var start = txtInputFomula.SelectionStart;
            var length = txtInputFomula.SelectionLength;
            var desc = (sender as System.Windows.Controls.Button).Tag as string;
            txtInputFomula.Text = txtInputFomula.Text.Substring(0, start) + desc + txtInputFomula.Text.Substring(start + length);
            txtInputFomula.SelectionStart = start + desc.Length;
            txtInputFomula.SelectionLength = 0;
            txtInputFomula.Focus();
        }

        private void OnPasteItemClearClick(object sender, RoutedEventArgs e) {
            myClipboard.Clear();
            PasteItems.Children.Clear();
        }
        #endregion Clipboard Operations
        #endregion Text Operations


        #region Save Operations
        public void SaveToPng() {
            if(currentSvgText is null) {
                txtOutpuInfo.Text = $"[ERROR] Empty result now.";
                return;
            }
            var dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.Filter = "Portable Network Graphics (*.png)|*.png";
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    var stp = Stopwatch.StartNew();
                    var ms = ImagickConvert(currentSvgText, currentSvgSize.Item1, currentSvgSize.Item2, null, MagickFormat.Png);
                    stp.Stop();
                    Bitmap.FromStream(ms).Save(dialog.FileName);
                    txtOutpuInfo.Text = $"[SUCCEED] Save image to {dialog.FileName}. (using {stp.ElapsedTicks * 1000F / Stopwatch.Frequency} ms)";
                } catch(Exception err) {
                    txtOutpuInfo.Text = $"[ERROR] Save to PNG failed:\n {err}";
                }
            }
        }

        public void SaveToSvg() {
            if(currentSvgText is null) {
                txtOutpuInfo.Text = $"[ERROR] Empty result now.";
                return;
            }
            var dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.Filter = "Scalable Vector Graphics (*.svg)|*.svg";
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    var svg = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(currentSvgText);
                    svg.Write(dialog.FileName);
                    txtOutpuInfo.Text = $"[SUCCEED] Save image to {dialog.FileName}.";
                } catch(Exception err) {
                    txtOutpuInfo.Text = $"[ERROR] Save to SVG failed:\n {err}";
                }
            }
        }

        public void CopyToClipboard() {
            if(currentSvgText is null) {
                txtOutpuInfo.Text = $"[ERROR] Empty result now.";
                return;
            }
            try {
                var stp = Stopwatch.StartNew();
                var ms = ImagickConvert(currentSvgText, currentSvgSize.Item1, currentSvgSize.Item2, ColorToMagick(ClipBgColor.SelectedColor.Value), MagickFormat.Png);
                stp.Stop();
                Clipboard.SetImage(Bitmap.FromStream(ms));
                txtOutpuInfo.Text = $"[SUCCEED] Copyied image to clipboard . (using {stp.ElapsedTicks * 1000F / Stopwatch.Frequency} ms)";
            } catch(Exception err) {
                txtOutpuInfo.Text = $"[ERROR] Save to PNG failed:\n {err}";
            }
        }

        private static MagickColor ColorToMagick(System.Drawing.Color color) {
            return new MagickColor((ushort)(color.R * (ushort.MaxValue / byte.MaxValue)),
                (ushort)(color.G * 1.0 * (ushort.MaxValue / byte.MaxValue)),
                (ushort)(color.B * 1.0 * (ushort.MaxValue / byte.MaxValue)),
                (ushort)(color.A * 1.0 * (ushort.MaxValue / byte.MaxValue)));
        }

        private static MagickColor ColorToMagick(System.Windows.Media.Color color) {
            return new MagickColor((ushort)(color.R * (ushort.MaxValue / byte.MaxValue)),
                (ushort)(color.G * 1.0 * (ushort.MaxValue / byte.MaxValue)),
                (ushort)(color.B * 1.0 * (ushort.MaxValue / byte.MaxValue)),
                (ushort)(color.A * 1.0 * (ushort.MaxValue / byte.MaxValue)));
        }

        public ICommand cSaveToPng { get => new ConditionCommand(SaveToPng); }
        public ICommand cSaveToSvg { get => new ConditionCommand(SaveToSvg); }
        public ICommand cCopyToClipboard { get => new ConditionCommand(CopyToClipboard); }
        #endregion Save Operations


        #region Commands & Notification
        #region Notification
        private void AddNotifiactions() {
            // Notify memory usage
            new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 1000), DispatcherPriority.Background, (object state, EventArgs e) => {
                OnPropertyChanged("UsedMemory");
            }, Dispatcher.CurrentDispatcher).Start();

            // Notify new zoom
            this.PropertyChanged += (s, e) => {
                if(e.PropertyName == "FormulaZoom") {
                    Render(currentSvgText);
                }
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion Notification


        #region Other Commands
        public ICommand ExitCommand { get => new ConditionCommand(() => Application.Current.Shutdown()); }
        public long UsedMemory => GC.GetTotalMemory(true) / 1024;
        #endregion Other Commands


        #region FormulaZoom
        private double _FormulaZoom;
        public double FormulaZoom {
            get => _FormulaZoom;
            set {
                _FormulaZoom = value;
                OnPropertyChanged();
            }
        }

        private void StatusBarItem_MouseDown(object sender, MouseButtonEventArgs e) {
            FormulaZoom = 1.0;
        }

        private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            FormulaZoom = e.NewValue;
        }
        #endregion FormulaZoom

        #endregion Commands & Notification
    }
}
