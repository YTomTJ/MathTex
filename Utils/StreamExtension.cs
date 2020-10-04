using System.IO;
using System.Text;

namespace MathTex.Utils {

    public static class StreamExtension {

        public static byte[] GetDataFromStream(this Stream stream) {
            if(stream == null) {
                return null;
            }
            byte[] array = new byte[stream.Length];
            stream.Seek(0L, SeekOrigin.Begin);
            stream.Read(array, 0, array.Length);
            return array;
        }

        public static string ReadToString(this Stream stream) {
            StringBuilder stringBuilder = new StringBuilder();
            if(stream == null) {
                return null;
            }
            try {
                int num;
                do {
                    num = stream.ReadByte();
                    if(num != -1) {
                        stringBuilder.Append((char)num);
                    }
                }
                while(num != -1);
            } finally {
                stream.Dispose();
            }
            return stringBuilder.ToString();
        }

        public static string ReadString(this Stream stream) {
            if(stream == null) {
                return string.Empty;
            }
            long position = stream.Position;
            string result = string.Empty;
            try {
                stream.Position = 0L;
                result = new StreamReader(stream).ReadToEnd();
            } finally {
                stream.Position = position;
            }
            return result;
        }
    }

}