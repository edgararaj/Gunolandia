using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Text;

namespace Gunolandia
{
    class Utils
    {
        public static void DebugLog(string msg)
        { 
            Debug.WriteLine($"<{DateTime.Now.Second * 1e3 + DateTime.Now.Millisecond}> {msg}");
        }

        public static Bitmap ResizeImage(Image source, int scale)
        {
            var width = source.Width * scale;
            var height = source.Height * scale;
            var result = new Bitmap(width, height);
            using (var canvas = Graphics.FromImage(result))
            {
                canvas.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                canvas.DrawImage(source, 0, 0, width, height);
            }
            return result;
        }

        [SuppressUnmanagedCodeSecurity, DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetAsyncKeyState(int keyCode);
        public static bool KeyIsDown(Keys key)
        {
            var key_bytes = BitConverter.GetBytes(GetAsyncKeyState((int)key));
            var key_is_down = (key_bytes[1] == 0x80 || key_bytes[0] == 1);

            return key_is_down;
        }

        public static Vector2 Normalize(Vector2 vec)
        { 
            if (vec.X != 0 && vec.Y != 0)
            {
                vec *= (float)Math.Sqrt(0.5f);
            }
            return vec;
        }

        public static Rectangle GetFrameRectFromRoll(int width, int height, int frame_x, int frame_y)
        {
            return new Rectangle(width * frame_x, height * frame_y, width, height);
        }

        public static Bitmap BitmapFromNinePatch(Bitmap source, Size size, int scale)
        {
            var width = size.Width / scale;
            var height = size.Height / scale;
            var result = new Bitmap(width, height);

            var src_grid_size = 10;

            var grid_x_count = width / src_grid_size;
            var grid_x_remainder = width % src_grid_size;
            if (grid_x_remainder > 0) grid_x_count++;

            var grid_y_count = height / src_grid_size;
            var grid_y_remainder = height % src_grid_size;
            if (grid_y_remainder > 0) grid_y_count++;

            using (var canvas = Graphics.FromImage(result))
            {
                canvas.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                source.SetResolution(canvas.DpiX, canvas.DpiY);

                for (int grid_x = 0; grid_x < grid_x_count; grid_x++)
                {
                    for (int grid_y = 0; grid_y < grid_y_count; grid_y++)
                    {
                        var dest_rect = new Rectangle(grid_x * src_grid_size, grid_y * src_grid_size, src_grid_size, src_grid_size);
                        if (grid_x == 0)
                        {
                            if (grid_y == 0)
                                canvas.DrawImage(source, dest_rect, new Rectangle(0, 0, src_grid_size, src_grid_size), GraphicsUnit.Pixel);
                            else if (grid_y == grid_y_count - 1)
                            {
                                if (grid_y_remainder > 0) dest_rect.Offset(0, - src_grid_size + grid_y_remainder);
                                canvas.DrawImage(source, dest_rect, new Rectangle(0, src_grid_size * 2, src_grid_size, src_grid_size), GraphicsUnit.Pixel);
                            }
                            else
                            {
                                var rect_height = (grid_y == grid_y_count - 2 && grid_y_remainder > 0) ? grid_y_remainder : src_grid_size;
                                dest_rect.Height = rect_height;
                                canvas.DrawImage(source, dest_rect, new Rectangle(0, src_grid_size, src_grid_size, rect_height), GraphicsUnit.Pixel);
                            }
                        }
                        else if (grid_x == grid_x_count - 1)
                        {
                            if (grid_x_remainder > 0) dest_rect.Offset(- src_grid_size + grid_x_remainder, 0);
                            if (grid_y == 0)
                                canvas.DrawImage(source, dest_rect, new Rectangle(src_grid_size * 2, 0, src_grid_size, src_grid_size), GraphicsUnit.Pixel);
                            else if (grid_y == grid_y_count - 1)
                            {
                                if (grid_y_remainder > 0) dest_rect.Offset(0, - src_grid_size + grid_y_remainder);
                                canvas.DrawImage(source, dest_rect, new Rectangle(src_grid_size * 2, src_grid_size * 2, src_grid_size, src_grid_size), GraphicsUnit.Pixel);
                            }
                            else
                            { 
                                var rect_height = (grid_y == grid_y_count - 2 && grid_y_remainder > 0) ? grid_y_remainder : src_grid_size;
                                dest_rect.Height = rect_height;
                                canvas.DrawImage(source, dest_rect, new Rectangle(src_grid_size * 2, src_grid_size, src_grid_size, rect_height), GraphicsUnit.Pixel);
                            }
                        }
                        else
                        {
                            var rect_width = (grid_x == grid_x_count - 2 && grid_x_remainder > 0) ? grid_x_remainder : src_grid_size;
                            dest_rect.Width = rect_width;
                            if (grid_y == 0)
                                canvas.DrawImage(source, dest_rect, new Rectangle(src_grid_size, 0, rect_width, src_grid_size), GraphicsUnit.Pixel);
                            else if (grid_y == grid_y_count - 1)
                            {
                                if (grid_y_remainder > 0) dest_rect.Offset(0, - src_grid_size + grid_y_remainder);
                                canvas.DrawImage(source, dest_rect, new Rectangle(src_grid_size, src_grid_size * 2, rect_width, src_grid_size), GraphicsUnit.Pixel);
                            }
                            else
                            { 
                                var rect_height = (grid_y == grid_y_count - 2 && grid_y_remainder > 0) ? grid_y_remainder : src_grid_size;
                                dest_rect.Height = rect_height;
                                canvas.DrawImage(source, dest_rect, new Rectangle(src_grid_size, src_grid_size, rect_width, rect_height), GraphicsUnit.Pixel);
                            }
                        }
                    }
                }
            }

            return Utils.ResizeImage(result, scale);
        }

        public static Rectangle ScaleRect(Rectangle rect, int scale)
        {
            return new Rectangle(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
        }
    }
}
