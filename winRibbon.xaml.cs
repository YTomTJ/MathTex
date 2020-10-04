using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Editors.Settings;
using DevExpress.Xpf.Ribbon;
using MathTex.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;

namespace MathTex {
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class winRibbon {


        private string formulaFontName;
        private double formulaFontSize;
        private List<string> myClipboard = new List<string>();  // Only recent 10 record

        private static Stopwatch stp = new Stopwatch();

        private string currentFormula = null;
        private string lastText = null;
        private Svg.SvgDocument currentSvg = null;
        private Bitmap currentBitmap = null;
        bool IsRendering = false;

        public winRibbon(winSplash splash = null) {
            InitializeComponent();
            DataContext = new MainViewModel();

            if(splash != null) {
                splash.InvokeUpdate("Loading fonts ...");
            }
            InitializeFontSelector();
            formulaFontName = ((FontFamily)eFontFamily.GetFirstCheckedItem().Tag).ToString();
            formulaFontSize = (double)eFontSize.EditValue;
            DataObject.AddCopyingHandler(txtInputFomula, OnInputFomulaCopying);

            if(Clipboard.ContainsText()) {
                myClipboard.Add(Clipboard.GetText());
            }

            if(splash != null) {
                splash.InvokeUpdate("Loading V8 javascipt engine ...");
            }
            _ = MathjaxParser.GetInstance();

            if(splash != null) {
                splash.InvokeUpdate("Loading latex resources ...");
            }
            LoadElement();

            if(splash != null) {
                splash.InvokeUpdate("Show window ...");
            }
        }


        #region Formula
        #region Formula_API
        /// <summary>
        /// Convert latex formula and render it.
        /// </summary>
        private void Convert() {

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
            } catch(Exception e) {
                PostConvert("[ERROR] Convert or render image failed:\n" + e);
                return;
            }

            // Render SvgImage to ImageSource for showing
            _ = SvgRender(currentSvg);
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
        private async Task SvgRender(Svg.SvgDocument svg, double scale = 1.0) {
            // TODO: scale image
            //currentSvg.Width *= 2;
            //currentSvg.Height *= 2;

            txtOutpuInfo.Text = "Rendering ...";

            await Task.Run(() => {
                try {
                    stp.Start();
                    currentBitmap = currentSvg.Draw();
                    stp.Stop();

                    this.Dispatcher.Invoke(() => {
                        bMath.Source = Images.ImageHelper.GetImageSource(currentBitmap);
                        PostConvert($"\nSucceed. (using {stp.ElapsedTicks * 1000F / Stopwatch.Frequency} ms)", true);
                    });
                } catch(Exception e) {
                    this.Dispatcher.Invoke(() => {
                        PostConvert("[ERROR] Render failed:\n" + e);
                    });
                }
            });
        }

        private void LoadElement() {

            foreach(var label in MathTex.Images.SymbolImages.GetInstance().SymbolGroups) {

                var lab = label.Name;
                var menu = (RibbonPageGroup)this.FindName("gFormulaButtons");
                // New BarSplitButtonItem
                var barbutton = new BarSplitButtonItem();
                barbutton.Name = $"gFormula_{lab}";
                barbutton.Content = lab;
                barbutton.ActAsDropDown = true;
                barbutton.RibbonStyle = RibbonItemStyles.Large;
                barbutton.Glyph = Images.ImageHelper.GetImageSource(new Uri($"/icons/{label.IconName}.png", UriKind.RelativeOrAbsolute));
                barbutton.LargeGlyph = barbutton.Glyph;
                var barbuttonmenu = new GalleryDropDownPopupMenu();
                barbuttonmenu.MaxWidth = 720;
                barbuttonmenu.Gallery = new Gallery();
                barbuttonmenu.Gallery.Style = this.FindResource("FormulaGalleryStyle") as Style;

                var menugroups = barbuttonmenu.Gallery.Groups;
                foreach(var item in label.SubGroups) {
                    var menugroup = new GalleryItemGroup() { Caption = item };
                    foreach(var sym in item.Symbols) {

                        var gi = new GalleryItem() { Glyph = new ImageSourceConverter().ConvertFrom(sym.PngData) as BitmapSource, Hint = sym.Name, Tag = sym };
                        gi.Click += Symbol_Click;
                        menugroup.Items.Add(gi);
                    }
                    menugroups.Add(menugroup);
                }
                barbutton.PopupControl = barbuttonmenu;
                menu.Items.Add(barbutton);
            }
        }
        #endregion Formula_API

        #region Formula_Operations
        private void BarButtonItem_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e) {
            Convert();
        }

        private void Symbol_Click(object sender, EventArgs e) {
            if(IsRendering)
                return;
            var sym = ((GalleryItem)sender).Tag as Images.LatexSymbol;
            txtInputFomula.Text = txtInputFomula.Text.Insert(txtInputFomula.SelectionStart, sym.Name);
        }
        #endregion Formula_Operations
        #endregion Formula


        #region FontSettings
        void InitializeFontSelector() {
            foreach(FontFamily fontFamily in (new DecimatedFontFamilies()).Items) {
                FontFamilyGalleryGroup.Items.Add(CreateItem(fontFamily, CreateImage(fontFamily)));
            }
            ((ComboBoxEditSettings)eFontSize.EditSettings).ItemsSource = FontSizes.Items;
            eFontFamily.FirstCheckedItem = FontFamilyGalleryGroup.Items[0]; // Arial
            eFontSize.EditValue = FontSizes.Items[14];  // 8
        }

        void FontFamilyGallery_ItemChecked(object sender, GalleryItemEventArgs e) {
            formulaFontName = ((FontFamily)e.Item.Tag).ToString();
        }

        void eFontSize_EditValueChanged(object sender, RoutedEventArgs e) {
            formulaFontSize = (double)eFontSize.EditValue;
        }

        #region FontSettings_Creator
        FormattedText fmtText = null;
        FormattedText createFormattedText(FontFamily fontFamily) {
            return new FormattedText("Aa", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                18, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        ImageSource CreateImage(FontFamily fontFamily) {
            const double DimensionSize = 24;
            const double HalfDimensionSize = DimensionSize / 2d;
            DrawingVisual v = new DrawingVisual();
            DrawingContext c = v.RenderOpen();
            c.DrawRectangle(Brushes.White, null, new Rect(0, 0, DimensionSize, DimensionSize));
            fmtText ??= createFormattedText(fontFamily);
            fmtText.SetFontFamily(fontFamily);
            fmtText.TextAlignment = TextAlignment.Center;
            double verticalOffset = (DimensionSize - fmtText.Baseline) / 2d;
            c.DrawText(fmtText, new System.Windows.Point(HalfDimensionSize, verticalOffset));
            c.Close();

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)DimensionSize, (int)DimensionSize, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(v);
            return rtb;
        }

        GalleryItem CreateItem(FontFamily fontFamily, ImageSource image) {
            GalleryItem item = new GalleryItem();
            item.Glyph = image;
            item.Caption = fontFamily.ToString();
            item.Tag = fontFamily;
            return item;
        }
        #endregion FontSettings_Creator

        #region FontSettings_Classes
        public class FontSizes {
            public static double[] Items {
                get {
                    return new double[] {
                        3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                        16, 17, 18, 19, 20, 22, 24, 26, 28, 30,
                        32, 34, 36, 38, 40, 44, 48, 52, 56, 60,
                        64, 68, 72, 76, 80, 88, 96, 104, 112, 120,
                        128, 136, 144
                    };
                }
            }
        }

        public class DecimatedFontFamilies : FontFamilies {
            const int DecimationFactor = 5;

            public override ObservableCollection<FontFamily> Items {
                get {
                    ObservableCollection<FontFamily> res = new ObservableCollection<FontFamily>();
                    for(int i = 0; i < ItemsCore.Count; i++) {
                        if(i % DecimationFactor == 0)
                            res.Add(ItemsCore[i]);
                    }
                    return res;
                }
            }
        }

        public class FontFamilies {
            static ObservableCollection<FontFamily> items;
            protected static ObservableCollection<FontFamily> ItemsCore {
                get {
                    if(items == null) {
                        items = new ObservableCollection<FontFamily>();
                        foreach(FontFamily fam in Fonts.SystemFontFamilies) {
                            if(!IsValidFamily(fam))
                                continue;
                            items.Add(fam);
                        }
                    }
                    return items;
                }
            }
            public static bool IsValidFamily(FontFamily fam) {
                foreach(Typeface f in fam.GetTypefaces()) {
                    GlyphTypeface g = null;
                    try {
                        if(f.TryGetGlyphTypeface(out g))
                            if(g.Symbol)
                                return false;
                    } catch(Exception) {
                        return false;
                    }
                }
                return true;
            }
            public virtual ObservableCollection<FontFamily> Items {
                get {
                    ObservableCollection<FontFamily> res = new ObservableCollection<FontFamily>();
                    foreach(FontFamily fm in ItemsCore) {
                        res.Add(fm);
                    }
                    return res;
                }
            }
        }
        #endregion FontSettings_Classes
        #endregion FontSettings


        #region Text_Operations
        private void OnInputFomulaCopying(object sender, DataObjectCopyingEventArgs e) {
            while(myClipboard.Count > 10) {
                myClipboard.RemoveAt(myClipboard.Count);
            }
            myClipboard.Insert(0, (string)e.DataObject.GetData(typeof(string)));
        }

        private void bPaste_ItemClick(object sender, ItemClickEventArgs e) {
            txtInputFomula.Paste();
        }

        private void bCut_ItemClick(object sender, ItemClickEventArgs e) {
            txtInputFomula.Cut();
        }

        private void bCopy_ItemClick(object sender, ItemClickEventArgs e) {
            txtInputFomula.Copy();
        }

        private void bClear_ItemClick(object sender, ItemClickEventArgs e) {
            txtInputFomula.Clear();
        }

        private void PopupControlContainer_Opening(object sender, System.ComponentModel.CancelEventArgs e) {
            int i = 0;
            foreach(var rec in myClipboard) {
                PasteDropDownItems.Items.Add(new GalleryItem() { Caption = i++, Description = rec });
            }
        }

        private void PopupControlContainer_Closed(object sender, EventArgs e) {
            PasteDropDownItems.Items.Clear();
        }

        private void Gallery_ItemClick(object sender, GalleryItemEventArgs e) {
            var start = txtInputFomula.SelectionStart;
            var length = txtInputFomula.SelectionLength;
            var desc = (string)e.Item.Description;
            txtInputFomula.Text = txtInputFomula.Text.Substring(0, start) + desc + txtInputFomula.Text.Substring(start + length);
            txtInputFomula.SelectionStart = start + desc.Length;
            txtInputFomula.SelectionLength = 0;
            txtInputFomula.Focus();
        }

        private void bClearPaste_ItemClick(object sender, ItemClickEventArgs e) {
            myClipboard.Clear();
            PasteDropDownItems.Items.Clear();
        }
        #endregion Text_Operations


        #region Image_Operations
        private void SaveToPng(object sender, ItemClickEventArgs e) {
            if(currentBitmap is null) {
                return;
            }
            var dialog = new SaveFileDialog();
            dialog.Filter = "Portable Network Graphics (*.png)|*.png";
            if(dialog.ShowDialog() ?? true) {
                try {
                    currentBitmap.Save(dialog.FileName);
                } catch(Exception err) {
                    txtOutpuInfo.Text = $"[ERROR] Save to PNG failed:\n {err}";
                }
            }
        }

        private void SaveToSvg(object sender, ItemClickEventArgs e) {
            if(currentSvg is null) {
                return;
            }
            var dialog = new SaveFileDialog();
            dialog.Filter = "Scalable Vector Graphics (*.svg)|*.svg";
            if(dialog.ShowDialog() ?? true) {
                try {
                    currentSvg.Write(dialog.FileName);
                } catch(Exception err) {
                    txtOutpuInfo.Text = $"[ERROR] Save to SVG failed:\n {err}";
                }
            }
        }
        #endregion Image_Operations
    }
}
