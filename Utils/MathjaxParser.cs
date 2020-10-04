using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using JavaScriptEngineSwitcher.ChakraCore;

namespace MathTex.Utils {

    public class MathjaxParser {
        
        private static ChakraCoreJsEngine _MathjaxEngine;
        private static bool _IsLoaded { get; set; }
        private static string _ConvertFunction = @"
function HtmlToSvgConvert(text, options=null) {
    MathJax.texReset();
    !options && (options={ em:'16', scale:'1.0' });
    !options.display && (options.display = true);
    var node = MathJax.tex2svg(text, options);
    return MathJax.startup.adaptor.outerHTML(node.children[0])
}
";
        private static MathjaxParser _Instance = null;

        private MathjaxParser() {
            Load();
        }

        public static MathjaxParser GetInstance() {
            if(_Instance is null)
                _Instance = new MathjaxParser();
            return _Instance;
        }

        public bool IsLoaded { get => _IsLoaded; }


        /// <summary>
        /// Parse for latex to svg.
        /// </summary>
        /// <param name="latex">Source latex formula text.</param>
        /// <param name="options">Options for MathJax conversion(in JS way). 
        ///     NOTE that string object should add additinal quotation marks(' or ").</param>
        /// <returns></returns>
        public string Run(string latex, Tuple<string, string>[] options = null) {

            if(_MathjaxEngine is null)
                return null;

            Debug.Assert(_IsLoaded, "Module is not loaded or loaded failed.");

            // Parse options
            // TODO: not functinal options
            if(options is null) {
                options = new Tuple<string, string>[] {
                    //new Tuple<string, string>("display", "true"),
                    //new Tuple<string, string>("em", "\'20\'"),
                    //new Tuple<string, string>("family", "\'\'"),
                    //new Tuple<string, string>("scale", "\'1.0\'"),
                };
            }
            var args = new List<String>();
            foreach(var op in options) {
                args.Add($"{op.Item1}:{op.Item2}");
            }
            var argument = $"{{{string.Join(',', args)}}}";

            // Preprocess formula text
            latex = Regex.Replace(latex, @"(\r\n)", " ");
            latex = Regex.Replace(latex, @"\\", "\\\\");

            // Make mathjax call expression
            var expression = $"HtmlToSvgConvert(\"{latex}\",{argument})";

            return _MathjaxEngine.Evaluate<string>(expression);
        }


        /// <summary>
        /// Load JS engine and execute js resource.
        /// </summary>
        private static void Load() {
            if(!_IsLoaded) {
                try {
                    _MathjaxEngine = new ChakraCoreJsEngine();
                    _MathjaxEngine.Execute(Properties.Resources.tex_svg);
                    _MathjaxEngine.Execute(Properties.Resources.liteDOM);
                    _MathjaxEngine.Execute("MathJax.config.startup.ready();");
                    _MathjaxEngine.Execute(_ConvertFunction);
                    _IsLoaded = true;
                } catch(Exception e) {
                    _IsLoaded = false;
                    throw e;
                }
            }
        }

        /// <summary>
        /// Dispose JS engine resource.
        /// </summary>
        public void UnLoad() {
            if(_MathjaxEngine != null) {
                _MathjaxEngine.Dispose();
                _MathjaxEngine = null;
            }
            _IsLoaded = false;
        }
    }
}
