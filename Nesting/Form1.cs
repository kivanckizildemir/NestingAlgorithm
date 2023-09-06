using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nesting
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public void GenerateTextBoxes(Dictionary<string, List<string>> filesByMaterial)
        {
            int xPos = 10;
            int yPos = 10;

            foreach (var material in filesByMaterial.Keys)
            {
                foreach (var fileName in filesByMaterial[material])
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    Label label = new Label();
                    label.Text = fileNameWithoutExtension;
                    label.Location = new Point(xPos, yPos);
                    scrollPanel.Controls.Add(label); // Add to panel

                    TextBox textBox = new TextBox();
                    textBox.Name = "txt_" + fileNameWithoutExtension;
                    textBox.Location = new Point(xPos + 100, yPos);
                    scrollPanel.Controls.Add(textBox); // Add to panel

                    yPos += 30;
                }
            }
            Button btnSubmit = new Button();
            btnSubmit.Text = "Submit";
            btnSubmit.Location = new Point(120, yPos);
            btnSubmit.Click += new EventHandler(this.btnSubmit_Click);
            this.Controls.Add(btnSubmit);
        }

        public void btnSubmit_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();

        }
    }
}
