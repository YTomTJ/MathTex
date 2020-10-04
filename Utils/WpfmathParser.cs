using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using WpfMath;

namespace MathTex.Utils {

    public class WpfmathParser {

        private static TexFormulaParser parser = new TexFormulaParser();

        public static BitmapSource TextToFormulaConvert(string tex, out string error, Size size, double fontsize = 16, string fontname = "Arial") {
            error = null;
            try {
                var formula = parser.Parse(tex);
                var renderer = formula.GetRenderer(TexStyle.Display, fontsize, fontname);

                BitmapSource img = renderer.RenderToBitmap(1, 1, 96);
                return img;
            } catch(Exception err) {
                error = err.Message;
            }
            return null;
        }

        public static BitmapSource TextToFormulaConvert(string tex, out string error, double fontsize = 16, string fontname = "Arial") {
            return TextToFormulaConvert(tex, out error, new Size(0, 0), fontsize, fontname);
        }
    }
}
