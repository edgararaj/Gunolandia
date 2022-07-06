using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Numerics;
using System.Diagnostics;

namespace Gunolandia
{
    class Player
    {
        public Bitmap bitmap;
        public Rectangle collision_box;
        public Vector2 velocity;
        public Vector2 center;
        public Timer idle_timer = new Timer(300);
        public Timer running_timer = new Timer(150);
        public int frame_x = 0;
        public int frame_start;
        private int frame_count;
        public int frame_end;
        private int sprite_height;
        public int sprite_width;
        public bool rotate_flip = false;
        public float tile_width;
        public AnimationState state;
        public bool wants_to_smoke = false;

        public enum AnimationState { 
            Idle, RunningX, RunningY, Smoking
        }

        private AnimationState State {
            set {
                if (value != state)
                {
                    SetState(value);
                    frame_x = frame_start;
                    idle_timer.Reset();
                    running_timer.Reset();
                }
            }
        }

        public Player()
        {
            bitmap = new Bitmap("assets/guna.png");
            sprite_height = 32;
            sprite_width = 32;
            frame_count = 10;
            tile_width = 1f;
            collision_box = new Rectangle(10, sprite_height - 17, sprite_width - 10 * 2, 10);
            collision_box.Offset(-sprite_height / 2, -sprite_height / 2);
            center = new Vector2(0, 0);

            SetState(AnimationState.Idle);
            frame_x = frame_start;
        }

        public void SetState(AnimationState state)
        {
            this.state = state;
            switch (state)
            {
                case AnimationState.RunningY:
                    frame_start = 0;
                    frame_end = 3;
                    break;
                case AnimationState.RunningX:
                    frame_start = 3;
                    frame_end = 6;
                    break;
                case AnimationState.Idle:
                    frame_start = 6;
                    frame_end = 8;
                    break;
                case AnimationState.Smoking:
                    frame_start = 8;
                    frame_end = 10;
                    break;
            }
        }

        public Player(Bitmap bitmap, float tile_width, int sprite_width, int sprite_height, Vector2 center)
        {
            this.bitmap = bitmap;
            this.sprite_height = sprite_height;
            this.sprite_width = sprite_width;
            this.tile_width = tile_width;
            collision_box = new Rectangle(0, sprite_height, sprite_height, sprite_height);
            collision_box.Offset(-sprite_height / 2, -sprite_height / 2);
            this.center = center;
        }

        public void IncrementTime(ulong delta_us)
        {
            var epsilon = 0.3f;
            if (Math.Abs(velocity.Y) > epsilon)
            {
                State = AnimationState.RunningY;
            }
            else if (Math.Abs(velocity.X) > epsilon)
            {
                State = AnimationState.RunningX;
            }
            else
            {
                if (wants_to_smoke)
                {
                    if (state == AnimationState.Smoking)
                    {
                        if (idle_timer.IncrementTime(delta_us))
                        {
                            frame_x = ((++frame_x - frame_start) % (frame_end - frame_start)) + frame_start;
                            /*
                                        if (frame_x == frame_start)
                                        {
                                            wants_to_smoke = false;
                                        }
                            */
                        }
                    }
                    else
                    {
                        State = AnimationState.Smoking;
                    }
                }

                if (!wants_to_smoke)
                    State = AnimationState.Idle;
            }

            if (!wants_to_smoke)
            {
                if (state == AnimationState.Idle)
                {
                    if (idle_timer.IncrementTime(delta_us))
                    {
                        frame_x = ((++frame_x - frame_start) % (frame_end - frame_start)) + frame_start;
                    }
                }
                else
                {
                    if (running_timer.IncrementTime((ulong)(delta_us * velocity.Length())))
                    {
                        frame_x = ((++frame_x - frame_start) % (frame_end - frame_start)) + frame_start;
                    }
                }
            }
        }

        public void Draw(Graphics canvas, int scale, int tile_center_x, int tile_center_y)
        {
            var resized_bitmap = Utils.ResizeImage(bitmap, scale);
            var offset_x = 0;
            switch (frame_x)
            {
                case 0:
                    offset_x = 1;
                    break;
                case 1:
                    offset_x = 0;
                    break;
                case 3:
                    offset_x = 1;
                    break;
                case 4:
                    offset_x = 0;
                    break;
                case 5:
                    offset_x = -1;
                    break;
            }
            var frame = frame_x;
            var offset_y = 0;
            if (rotate_flip)
            {
                frame = frame_count - frame_x - 1;
                resized_bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                offset_y = -2;
            }
            var player_top = tile_center_y - (int)((sprite_height - center.Y) * scale / 2) - (offset_x * scale);
            var player_left = tile_center_x - (int)((sprite_width + center.X) * scale / 2) + (offset_y * scale);
            var rect = Utils.GetFrameRectFromRoll(sprite_width * scale, sprite_height * scale, frame, 0);
            canvas.DrawImage(resized_bitmap, player_left, player_top, rect, GraphicsUnit.Pixel);
        }
    }
}
