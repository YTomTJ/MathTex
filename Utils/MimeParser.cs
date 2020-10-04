using DevExpress.Xpf.Editors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace MathTex {

    public class MimeParser {

        public static BitmapPalette Gray256Inverse;

        public static BitmapSource TextToBitmapSource(string tex, out string info, int fontsize = 8, int dpix = 96, int dpiy = 96) {

            info = null;
            var raster = new Raster();
            
            if(fontsize <= 0 && fontsize > 10) {
                info = "Warning: Fontsize should limit int (1~10).\n";
            }
            fontsize = Math.Clamp(fontsize, 1, 10);

            int ret;
            unsafe {
                ret = CovertTexToPixelmap(tex, &raster, fontsize);
            }
            if(ret == 0) {
                if(Gray256Inverse is null) {
                    var colors = new List<Color>();
                    for(int i = 255; i >= 0; --i) {
                        colors.Add(Color.FromRgb((byte)i, (byte)i, (byte)i));
                    }
                    Gray256Inverse = new BitmapPalette(colors);
                }

                var bitmapSource = BitmapSource.Create(raster.Width, raster.Height, dpix, dpiy,
                    PixelFormats.Indexed8,
                    Gray256Inverse,
                    raster.PixelMap,
                    raster.Width * raster.Height * raster.PixelSize / 8,
                    raster.Width * raster.PixelSize / 8);
                return bitmapSource;
            } else {
                info += "MimeTex parse failed.\n";
                return null;
            }
        }

        /// <summary>
        /// Covert Latex text formula to pixelmap data.
        /// </summary>
        /// <param name="expression">Latex formula.</param>
        /// <param name="gray">Output Raster structure containing pixelmap data.</param>
        /// <param name="fontsize">Font size, range 1~10.</param>
        /// <returns>0 is succeed, others failed.</returns>
        [DllImport("mimetex64", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern int CovertTexToPixelmap(string expression, Raster* gray, int fontsize);
    }
}
