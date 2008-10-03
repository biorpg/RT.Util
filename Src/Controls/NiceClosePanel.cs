using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace RT.Util.Controls
{
    /// <summary>Provides a narrow panel that somewhat resembles a
    /// tooltip window's title bar with a close button.</summary>
    public class NiceClosePanel : Panel
    {
        private Button FCloseButton;

        /// <summary>Triggers when the close button is clicked.</summary>
        public event EventHandler CloseClicked;

        /// <summary>Initialises a new <see cref="NiceClosePanel"/> instance.</summary>
        public NiceClosePanel()
            : base()
        {
            InitializeComponent();
            this.Resize += new EventHandler(NiceClosePanel_Resize);
            this.Paint += new PaintEventHandler(NiceClosePanel_Paint);
            this.FCloseButton.Click += new EventHandler(FCloseButton_Click);
        }

        private void FCloseButton_Click(object sender, EventArgs e)
        {
            if (CloseClicked != null)
                CloseClicked(this, e);
        }

        private void NiceClosePanel_Paint(object sender, PaintEventArgs e)
        {
            for (int i = 2; i < ClientSize.Height - 2; i++)
                e.Graphics.DrawLine(
                    new Pen(Color.FromKnownColor(
                        i % 2 == 1 ? KnownColor.ControlDark : KnownColor.ControlLightLight
                    )),
                    0, i, ClientSize.Width - FCloseButton.Width - 3, i
                );
        }

        private void NiceClosePanel_Resize(object sender, EventArgs e)
        {
            FCloseButton.Size = new Size(ClientSize.Height, ClientSize.Height);
            FCloseButton.Location = new Point(ClientSize.Width - FCloseButton.Width, 0);
            Refresh();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NiceClosePanel));
            this.FCloseButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // FCloseButton
            // 
            this.FCloseButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.FCloseButton.Image = ((System.Drawing.Image) (resources.GetObject("FCloseButton.Image")));
            this.FCloseButton.Location = new System.Drawing.Point(0, 0);
            this.FCloseButton.Margin = new System.Windows.Forms.Padding(0);
            this.FCloseButton.Name = "FCloseButton";
            this.FCloseButton.Padding = new System.Windows.Forms.Padding(0, 0, 2, 2);
            this.FCloseButton.Size = new System.Drawing.Size(8, 8);
            this.FCloseButton.TabIndex = 0;
            this.FCloseButton.TabStop = false;
            // 
            // NiceClosePanel
            // 
            this.Controls.Add(this.FCloseButton);
            this.ResumeLayout(false);

        }
    }
}
