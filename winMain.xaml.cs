using Fluent;
using MathTex.Images;
using MathTex.Utils;
using Svg;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;
using Clipboard = System.Windows.Forms.Clipboard;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace MathTex {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class winMain : Fluent.RibbonWindow, INotifyPropertyChanged {

        private static readonly int MaxClipboardSize = 10;
        //private string formulaFontName;
        //private double formulaFontSize;
        private List<string> myClipboard = new List<string>();  // Only recent 10 record

        private static Stopwatch stp = new Stopwatch();

        private string currentFormula = null;
        private string lastText = null;
        private Svg.SvgDocument currentSvg = null;
        private Tuple<SvgUnit, SvgUnit> currentSvgSize = null;
        private System.Drawing.Bitmap currentBitmap = null;
        bool IsRendering = false;

        public winMain(winSplash splash = null) {
            InitializeComponent();
            DataContext = this;

            if(splash != null) {
                splash.InvokeUpdate("Loading application settings ...");
            }
            _FormulaZoom = Properties.Settings.Default.FormulaScale;

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
            Properties.Settings.Default.FormulaScale = FormulaZoom;
            Properties.Settings.Default.Save();
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

            // Get the formula latex.
            var tex = txtInputFomula.Text.Trim();
            if(Regex.IsMatch(tex, @"^\s*$")) {
                PostConvert("[ERROR] Empty formula.");
                return;
            }

            // Record last text for not converting
            if(lastText == tex) {
                PostConvert("[REJECT] Text not changed.\nTry angin to force to convert.");
                lastText = "";
                return;
            }
            lastText = tex;

            // Implement mathjax parser.
            try {
                var formula = MathjaxParser.GetInstance().Run(tex);
                currentFormula = formula;
            } catch(Exception e) {
                PostConvert("[ERROR] Incorrect formula:\n" + e);
                return;
            }

            // Tranfer to SvgImage.
            try {
                var svg = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(currentFormula);
                currentSvg = svg;
                currentSvgSize = new Tuple<SvgUnit, SvgUnit>(currentSvg.Width, currentSvg.Height);
            } catch(Exception e) {
                PostConvert("[ERROR] Convert or render image failed:\n" + e);
                return;
            }

            // Render SvgImage to ImageSource for showing
            // TODO: I still wonder how to use async well 
            SvgRender();
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

        /// <summary>
        /// Redner svg to iamge.
        /// </summary>
        /// <param name="svg"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private void SvgRender() {

            if(currentSvg == null || currentSvgSize == null) {
                return;
            }

            currentSvg.Width = (float)(currentSvgSize.Item1 * FormulaZoom);
            currentSvg.Height = (float)(currentSvgSize.Item2 * FormulaZoom);
            txtOutpuInfo.Text = "Rendering ...";

            try {
                stp.Start();
                currentBitmap = currentSvg.Draw();
                stp.Stop();
                this.Dispatcher.Invoke(() => {
                    outputMath.Source = ImageHelper.GetImageSource(currentBitmap);
                    PostConvert($"\nSucceed. (using {stp.ElapsedTicks * 1000F / Stopwatch.Frequency} ms)", true);
                });
            } catch(Exception e) {
                this.Dispatcher.Invoke(() => {
                    PostConvert("[ERROR] Render failed:\n" + e);
                });
            }
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
            currentBitmap = null;
            currentFormula = null;
            currentSvg = null;
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
            if(currentBitmap is null) {
                txtOutpuInfo.Text = $"[ERROR] Empty result now.";
                return;
            }
            var dialog = new SaveFileDialog();
            dialog.Filter = "Portable Network Graphics (*.png)|*.png";
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    currentBitmap.Save(dialog.FileName);
                    txtOutpuInfo.Text = $"[SUCCEED] Save image to {dialog.FileName}.";
                } catch(Exception err) {
                    txtOutpuInfo.Text = $"[ERROR] Save to PNG failed:\n {err}";
                }
            }
        }

        public void SaveToSvg() {
            if(currentSvg is null) {
                txtOutpuInfo.Text = $"[ERROR] Empty result now.";
                return;
            }
            var dialog = new SaveFileDialog();
            dialog.Filter = "Scalable Vector Graphics (*.svg)|*.svg";
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    currentSvg.Write(dialog.FileName);
                    txtOutpuInfo.Text = $"[SUCCEED] Save image to {dialog.FileName}.";
                } catch(Exception err) {
                    txtOutpuInfo.Text = $"[ERROR] Save to SVG failed:\n {err}";
                }
            }
        }

        public void CopyToClipboard() {
            if(currentBitmap is null) {
                txtOutpuInfo.Text = $"[ERROR] Empty result now.";
                return;
            }
            try {
                // TODO: image need modify
                Clipboard.SetImage(currentBitmap);
                txtOutpuInfo.Text = $"[SUCCEED] Copyied image to clipboard .";
            } catch(Exception err) {
                txtOutpuInfo.Text = $"[ERROR] Save to PNG failed:\n {err}";
            }
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
                    SvgRender();
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
