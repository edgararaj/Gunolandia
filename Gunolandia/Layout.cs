using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Gunolandia
{
    struct ButtonAndRectAndAction
    {
        public Button button;
        public Rectangle rect;
        public Action<int> action;
    }

    class Layout
    {
        List<ButtonAndRectAndAction> buttons = new List<ButtonAndRectAndAction>();
        int padding;
        Type type;

        public enum Type { 
            CenterCenter, LeftTop
        }
  
        public Layout(int padding, Type type)
        {
            this.padding = padding;
            this.type = type;
        }

        public void Add(Button btn, Action<int> ac)
        {
            buttons.Add(new ButtonAndRectAndAction() { button = btn, action = ac });
        }

        public void CalculateRects(int x, int y)
        {
            if (type == Type.CenterCenter)
            {
                var center_x = x;
                var center_y = y;
                int total_height = 0;
                for (int i = 0; i < buttons.Count; i++)
                {
                    var btn = buttons[i];
                    total_height += btn.button.Height;
                }
                total_height += (buttons.Count - 1) * padding;

                var top_y = center_y - total_height / 2;
                for (int i = 0; i < buttons.Count; i++)
                {
                    var btn = buttons[i];
                    btn.rect = new Rectangle(center_x - btn.button.Width / 2, top_y, btn.button.Width, btn.button.Height);
                    top_y += btn.button.Height + padding;
                    buttons[i] = btn;
                }
            }
            else if (type == Type.LeftTop)
            { 
                var top_y = y;
                for (int i = 0; i < buttons.Count; i++)
                {
                    var btn = buttons[i];
                    btn.rect = new Rectangle(x, top_y, btn.button.Width, btn.button.Height);
                    top_y += btn.button.Height + padding;
                    buttons[i] = btn;
                }
            }
        }

        public void Draw(Graphics canvas)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                var btn = buttons[i];
                btn.button.Draw(canvas, btn.rect);
            }
        }

        public void MouseDown(Point e)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                var btn = buttons[i];
                btn.button.MouseDown(e, btn.rect);
            }
        }
        public void MouseUp(Point e)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                var btn = buttons[i];
                btn.action(btn.button.MouseUp(e, btn.rect));
            }
        }
    }
}
