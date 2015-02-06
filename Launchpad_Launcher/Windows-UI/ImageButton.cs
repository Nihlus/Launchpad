using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Launchpad_Launcher.Buttons
{
    class ImageButton : System.Windows.Forms.Control
    {
        Image backgroundImage, hoverImage, pressedImage;
        bool isPressed = false;
        bool mouseIsOver = false;

        public override Image BackgroundImage
        {
            get
            {
                return this.backgroundImage;
            }
            set
            {
                this.backgroundImage = value;
            }
        }

        public Image HoverImage
        {
            get
            {
                return this.hoverImage;
            }
            set
            {
                this.hoverImage = value;
            }
        }

        public Image PressedImage
        {
            get
            {
                return this.pressedImage;
            }
            set
            {
                this.pressedImage = value;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.isPressed = true;
            this.Invalidate();
            base.OnMouseDown(e);
        }

        //OnEnter and OnLeave handle the mouseover image updates
        protected override void OnMouseEnter(EventArgs e)
        {
            this.mouseIsOver = true;
            this.Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            this.mouseIsOver = false;
            this.Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            this.isPressed = false;
            this.Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //if the button is pressed and we have an image for it
            if (this.isPressed && this.pressedImage != null)
            {
                e.Graphics.DrawImage(this.pressedImage, 0, 0);
            }
            //if the button is not pressed, the mouse is over it and we have an image for it
            else if (!this.isPressed && this.mouseIsOver == true && this.hoverImage != null)
            {
                e.Graphics.DrawImage(this.hoverImage, 0, 0);
            }
            //the button is idle
            else
            {
                e.Graphics.DrawImage(this.backgroundImage, 0, 0);
            }

            if (this.Text.Length > 0)
            {
                SizeF size = e.Graphics.MeasureString(this.Text, this.Font);

                e.Graphics.DrawString(this.Text,
                    this.Font,
                    new SolidBrush(this.ForeColor),
                    (this.ClientSize.Width - size.Width) / 2,
                    (this.ClientSize.Height - size.Height) / 2);
            }
            base.OnPaint(e);
        }
    }
}
