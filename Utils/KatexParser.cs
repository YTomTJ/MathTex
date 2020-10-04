using JavaScriptEngineSwitcher.V8;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using BitmapImage = System.Windows.Media.Imaging.BitmapImage;


namespace MathTex.Utils {

    public class KatexParser {

        public static bool IsLoaded { get; protected set; }
        private static V8JsEngine katex_engine;

        public static void Load() {
            if(!IsLoaded) {
                try {
                    katex_engine = new V8JsEngine();
                    katex_engine.ExecuteFile("katex/katex.min.js");
                    IsLoaded = true;
                } catch(Exception e) {
                    IsLoaded = false;
                    throw e;
                }
            }
        }

        public static string Run(string latex, out string err, Tuple<string, string>[] options = null) {

            Debug.Assert(IsLoaded, "Module is not loaded or loaded failed.");

            if(options is null) {
                options = new Tuple<string, string>[] {
                    new Tuple<string, string>("displayMode", "true"),
                    new Tuple<string, string>("output", "'html'"),
                };
            }
            var args = new List<String>();
            foreach(var op in options) {
                args.Add($"{op.Item1}:{op.Item2}");
            }
            var argument = $"{{{string.Join(',', args)}}}";
            latex = Regex.Replace(latex, @"\\", "\\\\");
            var expression = $"katex.renderToString('{latex}', {argument})";
            MessageBox.Show(expression);

            try {
                var html = katex_engine.Evaluate<string>(expression);
                MessageBox.Show(html);

                err = null;
                return html;
            } catch (Exception e){
                err = e.Message;
                return null;
            }
        }

        //<script defer src='katex.min.js' crossorigin='anonymous'></script>
        //<script defer src='contrib/auto-render.min.js' crossorigin='anonymous' onload='renderMathInElement(document.body);'></script>
        public static string PaddingHtml(string snippet) {
            var doc_head = "<!DOCTYPE html>\n<html>\n<head>\n<link rel='stylesheet' href='katex.min.css' crossorigin='anonymous'>\n</head>\n<body>";
            var doc_tail = "\n</body>\n</html>";
            var html = doc_head + snippet + doc_tail;
            return html;
        }

        public static BitmapImage HtmlBmpConverter(string html) {

            var basedir = AppDomain.CurrentDomain.BaseDirectory + "katex\\";
            var uri = new Uri(basedir).AbsoluteUri;

            //BitmapFrame image = TheArtOfDev.HtmlRenderer.WPF.HtmlRender.RenderToImage(html);
            //var encoder = new PngBitmapEncoder();
            //encoder.Frames.Add(image);
            //using(FileStream stream = new FileStream("katex/image.png", FileMode.OpenOrCreate))
            //    encoder.Save(stream);

            //var converter = new SelectPdf.HtmlToImage();
            //converter.RenderingEngine = SelectPdf.RenderingEngine.WebKit;
            //converter.CustomCSS = "katex/katex.min.css";
            //var img = converter.ConvertHtmlString(html);
            //img.Save("katex/xxx.bmp");
            //var bitmap = new BitmapImage();
            //using(var ms = new MemoryStream()) {
            //    img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            //    bitmap.BeginInit();
            //    bitmap.StreamSource = ms;
            //    bitmap.EndInit();
            //}
            //return null;

            //var html = String.Format("<body>Hello world: {0}</body>", DateTime.Now);
            //var htmlToImageConv = new NReco.ImageGenerator.HtmlToImageConverter();
            //var jpegBytes = htmlToImageConv.GenerateImage(html, NReco.ImageGenerator.ImageFormat.Bmp);

            //var converter = new WkHtmlWrapper.Image.Converters.HtmlToImageConverter();
            //converter.ConvertHtmlToFile(html, uri, "katex/xxx.svg");
            //ImageConverter.ConvertHtmlToFile(html, uri, ImageFormat.Png, "xxx.png");
            //var data = converter.ConvertHtml(html, "katex");
            //converter.ConvertAsync(html, "katex/xxx.bmp");
            //var bitmap = new BitmapImage();
            //using(var ms = new MemoryStream(jpegBytes)) {
            //    bitmap.BeginInit();
            //    bitmap.StreamSource = ms;
            //    bitmap.EndInit();
            //}
            //return bitmap;

            //var converter = new Winnovative.HtmlToPdfConverter();
            //var stream = new MemoryStream();
            //var img = converter.ConvertPartialHtmlToImage(html, "katex/katex.min.css", "");

            //var bitmap = new BitmapImage();
            //using(var ms = new MemoryStream(img.ImageData)) {
            //    bitmap.BeginInit();
            //    bitmap.StreamSource = stream;
            //    bitmap.EndInit();
            //}
            //return bitmap;

            //var converter = new Syncfusion.HtmlConverter.HtmlToPdfConverter();
            //var stream = new MemoryStream();
            //var img = converter.ConvertPartialHtmlToImage(html, uri, "");
            //var bitmap = new BitmapImage();
            //using(var ms = new MemoryStream(img.ImageData)) {
            //    bitmap.BeginInit();
            //    bitmap.StreamSource = stream;
            //    bitmap.EndInit();
            //}
            //return bitmap;

            return null;
        }
    }
}