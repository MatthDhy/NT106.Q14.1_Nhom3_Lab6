using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace code_Lab6
{
    public partial class Client : Form
    {
        TcpClient client;
        NetworkStream stream;
        public Client()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string ip = txtIP.Text.Trim();
                int port = int.Parse(txtPort.Text.Trim());
                client = new TcpClient();
                client.Connect(IPAddress.Parse(ip), port);
                stream = client.GetStream();

                MessageBox.Show("Kết nối thành công đến server!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không kết nối được: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void btnPlaceOrder_Click(object sender, EventArgs e)
        {

        }
    }
}
