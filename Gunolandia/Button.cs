using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

namespace Gunolandia
{
    internal class Button
    {
        private Bitmap unpressed;
        private Bitmap pressed;
        private Bitmap heavily_pressed;
        private Bitmap icon;
        private string text;
        private Font font;

        private bool is_toggled = false;

        public Pressure pressure;
        public ClickType click_type;
        public Type type;

        public enum Pressure { 
            None, Normal, Heavy
        }

        public enum ClickType { 
            Normal, Toggle
        }

        public enum Type { 
            Icon, Text
        }

        public int Height
        { 
            get { return 50; }
        }
        public int Width
        {
            get { return 100; }
        }

        public Size Size {
            get { return new Size(Width, Height); }
        }

        public Button(Rectangle rect, ClickType click_type, string icon_path)
        {
            this.click_type = click_type;
            this.icon = Utils.ResizeImage(new Bitmap(icon_path), 2);
            this.type = Type.Icon;
            unpressed = Utils.BitmapFromNinePatch(new Bitmap(@"assets/ButtonUnpressed.9.png"), Size, 2);
            pressed = Utils.BitmapFromNinePatch(new Bitmap(@"assets/ButtonPressed.9.png"), Size, 2);
            heavily_pressed = Utils.BitmapFromNinePatch(new Bitmap(@"assets/ButtonHeavilyPressed.9.png"), Size, 2);
        }

        public Button(ClickType click_type, string text, Font font)
        {
            this.click_type = click_type;
            this.text = text;
            this.font = font;
            this.type = Type.Text;
            unpressed = Utils.BitmapFromNinePatch(new Bitmap(@"assets/ButtonUnpressed.9.png"), Size, 2);
            pressed = Utils.BitmapFromNinePatch(new Bitmap(@"assets/ButtonPressed.9.png"), Size, 2);
            heavily_pressed = Utils.BitmapFromNinePatch(new Bitmap(@"assets/ButtonHeavilyPressed.9.png"), Size, 2);
        }

        public void Draw(Graphics canvas, Rectangle rect)
        {
            var position = rect.Location;
            switch (pressure)
            {
                case Pressure.None:
                    canvas.DrawImageUnscaled(unpressed, position.X, position.Y);
                    break;
                case Pressure.Normal:
                    canvas.DrawImageUnscaled(pressed, position.X, position.Y);
                    break;
                case Pressure.Heavy:
                    canvas.DrawImageUnscaled(heavily_pressed, position.X, position.Y);
                    break;
            }
            var offset = 0;
            if (pressure == Pressure.None) offset = -4;
            else if (pressure == Pressure.Heavy) offset = 1;
            switch (type)
            {
                case Type.Icon:
                    canvas.DrawImageUnscaled(icon, position.X + (rect.Width - icon.Width) / 2, position.Y + (rect.Height - icon.Height) / 2 + offset);
                    break;
                case Type.Text:
                    var text_rect = canvas.MeasureString(text, font);
                    var offset_y = 2;
                    canvas.DrawString(text, font, Brushes.White, position.X + (rect.Width - text_rect.Width) / 2, position.Y + (rect.Height - text_rect.Height) / 2 + offset + offset_y);
                    break;
            }
        }

        public bool MouseDown(Point location, Rectangle rect)
        {
            if (rect.Contains(location))
            {
                pressure = Pressure.Heavy;
                return true;
            }

            return false;
        }

        public int MouseUp(Point location, Rectangle rect)
        {
            if (pressure != Pressure.None)
            {
                if (rect.Contains(location))
                {
                    if (click_type == ClickType.Toggle)
                    {
                        pressure = is_toggled ? Pressure.None : Pressure.Normal;
                        is_toggled = !is_toggled;
                    }
                    else
                    {
                        pressure = Pressure.None;
                    }
                    return is_toggled ? 2 : 1;
                }
                pressure = (click_type == ClickType.Toggle && is_toggled) ? Pressure.Normal : Pressure.None;
            }
            return 0;
        }
    }
}
