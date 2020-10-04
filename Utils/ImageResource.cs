using MathTex.Properties;
using System;
using System.Drawing;
using System.Windows.Markup;
using System.Windows.Media;

namespace MathTex.Images {

    public class ImageResource : MarkupExtension {

        public string Name { get; set; }

        public ImageResource(string path) {
			this.Name = path;
		}

		public ImageSource GetImageSource() {
			if(this.Name == null) {
				return null;
			}
            object obj = Resources.ResourceManager.GetObject(Name, Resources.Culture);
            if(obj is null) {
                return null;
            }
            return ImageHelper.GetImageSource((Bitmap)obj);
        }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            return GetImageSource();
        }
    }
}
