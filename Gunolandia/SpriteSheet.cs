using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Gunolandia
{
    internal class SpriteSheet
    {
        Bitmap bitmap;
        int sprite_height;
        int sprite_width;
        int frame_x = 0;
        int frame_count;
        int scale;
        public Timer animation_timer;

        public SpriteSheet(string path, int sprite_width, int sprite_height, int frame_count, int delta_ms, int scale)
        {
            this.scale = scale;
            bitmap = Utils.ResizeImage(new Bitmap(path), scale);
            this.sprite_height = sprite_height;
            this.sprite_width = sprite_width;
            this.frame_count = frame_count;
            animation_timer = new Timer(delta_ms);
        }

        public void IncrementTime(ulong delta_us)
        {
            if (animation_timer.IncrementTime(delta_us))
            {
                frame_x = (frame_x + 1) % frame_count;
            }
        }

        public void Draw(Graphics canvas, int tile_center_x, int tile_center_y)
        {
            var player_left = tile_center_x - (sprite_width * scale) / 2;
            var player_top = tile_center_y - (sprite_height * scale) / 2;
            var rect = Utils.GetFrameRectFromRoll(sprite_width * scale, sprite_height * scale, frame_x, 0);
            canvas.DrawImage(bitmap, player_left, player_top, rect, GraphicsUnit.Pixel);
        }
    }
}
