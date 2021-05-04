using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HitboxAssigner
{
    public partial class FrameNo : Form
    {
        public FrameNo()
        {
            InitializeComponent();
        }

        public int frameNo;

        private void button1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text != "") {
                frameNo = int.Parse(textBox1.Text);
            }
            this.Close();
        }
    }
}
