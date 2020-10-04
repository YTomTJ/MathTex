using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;

namespace MathTex.Utils {

    public class LatexGroup : DependencyObject {

        /// <summary>
        /// Display name in UI
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Name of the icon
        /// </summary>
        public string IconName { get; set; } = null;

        ///// <summary>
        ///// Groups of the latex symbols.
        ///// </summary>
        //public ResourceDictionary Items {
        //    get { return (ResourceDictionary)GetValue(ItemsProperty); }
        //    set { SetValue(ItemsProperty, value); }
        //}
        //public static readonly DependencyProperty ItemsProperty =
        //    DependencyProperty.Register("Items", typeof(ResourceDictionary), typeof(LatexGroup));
    }

    public class LatexSymbol {
        /// <summary>
        /// Name of the latex math symbol.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Alternate name of the latex math symbol.
        /// For some name is symbol, replace with this when restroe.
        /// </summary>
        public string SymbolName { get; set; } = null;

        /// <summary>
        /// Distinwish some symbols.
        /// </summary>
        public int Id { get; set; } = -1;

        /// <summary>
        /// If not null, means the position indexes in the {Name} of params.
        /// </summary>
        public int[] Index { get; set; } = null;
    }

    public interface IFormulaParser {
        public bool IsLoaded { get; protected set; }
        public void Load();
        public void UnLoad();
        public string Run(string latex, Tuple<string, string>[] options = null);
    }
}
