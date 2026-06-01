using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Eneca.CustomDataGrid.Theme
{
    internal static class DataGridCursors
    {
        private static Cursor _columnResize;

        public static Cursor ColumnResize => _columnResize ?? (_columnResize = CreateWideHorizontalCursor());

        private static Cursor CreateWideHorizontalCursor()
        {
            const int size = 24;
            const int hotspot = size / 2;

            try
            {
                var resources = new ResourceDictionary
                {
                    Source = new Uri("/Eneca.CustomDataGrid;component/Controls/Icons/WideHorizontal.xaml", UriKind.Relative)
                };

                var drawing = resources["wide_horizontal_Icon_wide_horizontalDrawingImage"] as DrawingImage;
                if (drawing?.Drawing == null)
                    return Cursors.SizeWE;

                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                    context.DrawDrawing(drawing.Drawing);

                var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);

                return CreateCursorFromBitmap(bitmap, hotspot, hotspot);
            }
            catch
            {
                return Cursors.SizeWE;
            }
        }

        private static Cursor CreateCursorFromBitmap(BitmapSource bitmap, int xHotspot, int yHotspot)
        {
            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);

            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.Default, leaveOpen: true))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)2);
                writer.Write((ushort)1);

                writer.Write((byte)width);
                writer.Write((byte)height);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)xHotspot);
                writer.Write((ushort)yHotspot);

                var sizeOffset = stream.Position;
                writer.Write(0);
                writer.Write(0);

                var dibOffset = (int)stream.Position;

                writer.Write(40);
                writer.Write(width);
                writer.Write(height * 2);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);

                var xorStride = ((width * 32 + 31) / 32) * 4;
                var andStride = ((width + 31) / 32) * 4;

                for (var row = height - 1; row >= 0; row--)
                {
                    for (var col = 0; col < width; col++)
                    {
                        var i = (row * width + col) * 4;
                        writer.Write(pixels[i + 2]);
                        writer.Write(pixels[i + 1]);
                        writer.Write(pixels[i]);
                        writer.Write(pixels[i + 3]);
                    }

                    for (var pad = width * 4; pad < xorStride; pad++)
                        writer.Write((byte)0);
                }

                for (var row = height - 1; row >= 0; row--)
                {
                    for (var colByte = 0; colByte < andStride; colByte++)
                    {
                        byte maskByte = 0;
                        for (var bit = 0; bit < 8; bit++)
                        {
                            var col = colByte * 8 + bit;
                            if (col >= width)
                                continue;

                            var alpha = pixels[(row * width + col) * 4 + 3];
                            if (alpha == 0)
                                maskByte |= (byte)(1 << (7 - bit));
                        }

                        writer.Write(maskByte);
                    }
                }

                var dibSize = (int)(stream.Position - dibOffset);
                stream.Seek(sizeOffset, SeekOrigin.Begin);
                writer.Write(dibSize);
                writer.Write(dibOffset);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return new Cursor(stream);
        }
    }
}
