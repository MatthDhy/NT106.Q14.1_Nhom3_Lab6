using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace code_Lab6
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
        }
        private void BtnStart_Click(object sender, EventArgs e)
        {
            rtbLog.AppendText($"[{DateTime.Now}] Server is starting...\n");
            btnStart.Enabled = false;
            lblStatus.Text = "Status: Listening...";

            // Code socket sẽ viết thêm ở đây sau
        }
    }
}
