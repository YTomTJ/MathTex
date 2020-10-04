using System;
using System.Runtime.CompilerServices;

namespace MathTex {

    public enum RasterFormat : Int32 {
        Bitmap = 1,
        Gif8bit = 2,
        Gif4bit = 3
    }

    public unsafe partial struct Raster {
        public Int32 Width;              /* #pixels wide */
        public Int32 Height;             /* #pixels high */
        public RasterFormat Format;      /* 1=bitmap, 2=gf/8bits,3=gf/4bits */
        public Int32 PixelSize;          /* #bits per pixel, 1 or 8 */
        public IntPtr PixelMap;          /* unsigned char */
    };

    public unsafe partial struct RasterPtr {
        public Raster* NativePtr { get; }
        public RasterPtr(Raster* nativePtr) => NativePtr = nativePtr;
        public RasterPtr(IntPtr nativePtr) => NativePtr = (Raster*)nativePtr;
        public static implicit operator RasterPtr(Raster* nativePtr) => new RasterPtr(nativePtr);
        public static implicit operator Raster*(RasterPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator RasterPtr(IntPtr nativePtr) => new RasterPtr(nativePtr);

        public ref Int32 Width => ref Unsafe.AsRef<Int32>(&NativePtr->Width);
        public ref Int32 Height => ref Unsafe.AsRef<Int32>(&NativePtr->Height);
        public ref RasterFormat Format => ref Unsafe.AsRef<RasterFormat>(&NativePtr->Format);
        public ref Int32 PixelSize => ref Unsafe.AsRef<Int32>(&NativePtr->PixelSize);
        public ref IntPtr PixelMap => ref Unsafe.AsRef<IntPtr>(&NativePtr->PixelMap);
    }
}
