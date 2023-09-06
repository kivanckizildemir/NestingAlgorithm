
namespace Nesting
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.scrollPanel = new System.Windows.Forms.Panel();
            this.boundaryGapsTxt = new System.Windows.Forms.TextBox();
            this.panelGapsTxt = new System.Windows.Forms.TextBox();
            this.boundaryGaps = new System.Windows.Forms.Label();
            this.panelGaps = new System.Windows.Forms.Label();
            this.Ok = new System.Windows.Forms.Button();
            this.scrollPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // scrollPanel
            // 
            this.scrollPanel.AutoScroll = true;
            this.scrollPanel.Controls.Add(this.boundaryGapsTxt);
            this.scrollPanel.Controls.Add(this.panelGapsTxt);
            this.scrollPanel.Controls.Add(this.boundaryGaps);
            this.scrollPanel.Controls.Add(this.panelGaps);
            this.scrollPanel.Controls.Add(this.Ok);
            this.scrollPanel.Location = new System.Drawing.Point(41, 26);
            this.scrollPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.scrollPanel.Name = "scrollPanel";
            this.scrollPanel.Size = new System.Drawing.Size(551, 496);
            this.scrollPanel.TabIndex = 0;
            // 
            // boundaryGapsTxt
            // 
            this.boundaryGapsTxt.Location = new System.Drawing.Point(444, 105);
            this.boundaryGapsTxt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.boundaryGapsTxt.Name = "boundaryGapsTxt";
            this.boundaryGapsTxt.Size = new System.Drawing.Size(73, 22);
            this.boundaryGapsTxt.TabIndex = 4;
            this.boundaryGapsTxt.Visible = false;
            // 
            // panelGapsTxt
            // 
            this.panelGapsTxt.Location = new System.Drawing.Point(444, 59);
            this.panelGapsTxt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panelGapsTxt.Name = "panelGapsTxt";
            this.panelGapsTxt.Size = new System.Drawing.Size(73, 22);
            this.panelGapsTxt.TabIndex = 3;
            this.panelGapsTxt.Visible = false;
            // 
            // boundaryGaps
            // 
            this.boundaryGaps.AutoSize = true;
            this.boundaryGaps.Location = new System.Drawing.Point(329, 113);
            this.boundaryGaps.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.boundaryGaps.Name = "boundaryGaps";
            this.boundaryGaps.Size = new System.Drawing.Size(101, 16);
            this.boundaryGaps.TabIndex = 2;
            this.boundaryGaps.Text = "Boundary Gaps";
            this.boundaryGaps.Visible = false;
            // 
            // panelGaps
            // 
            this.panelGaps.AutoSize = true;
            this.panelGaps.Location = new System.Drawing.Point(329, 68);
            this.panelGaps.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.panelGaps.Name = "panelGaps";
            this.panelGaps.Size = new System.Drawing.Size(78, 16);
            this.panelGaps.TabIndex = 1;
            this.panelGaps.Text = "Panel Gaps";
            this.panelGaps.Visible = false;
            // 
            // Ok
            // 
            this.Ok.Location = new System.Drawing.Point(409, 447);
            this.Ok.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Ok.Name = "Ok";
            this.Ok.Size = new System.Drawing.Size(100, 28);
            this.Ok.TabIndex = 0;
            this.Ok.Text = "Ok";
            this.Ok.UseVisualStyleBackColor = true;
            this.Ok.Click += new System.EventHandler(this.btnSubmit_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(635, 554);
            this.Controls.Add(this.scrollPanel);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Form1";
            this.Text = "Form1";
            this.scrollPanel.ResumeLayout(false);
            this.scrollPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Panel scrollPanel;
        private System.Windows.Forms.Button Ok;
        private System.Windows.Forms.Label boundaryGaps;
        private System.Windows.Forms.Label panelGaps;
        private System.Windows.Forms.TextBox boundaryGapsTxt;
        private System.Windows.Forms.TextBox panelGapsTxt;
    }
}

