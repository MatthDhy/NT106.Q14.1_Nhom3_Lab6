using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace code_Lab6
{
    public partial class Client : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private bool isConnected = false;
        private Thread receiveThread;

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
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                isConnected = true;

                writer.WriteLine("AUTH CUSTOMER");

                writer.WriteLine("MENU");

                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                MessageBox.Show("Kết nối thành công đến server!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnConnect.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không kết nối được: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReceiveData()
        {
            try
            {
                while (isConnected)
                {
                    string response = reader.ReadLine();
                    if (response == null) break;

                    // Xử lý dữ liệu trả về trên luồng giao diện (UI Thread)
                    this.Invoke(new Action(() => ProcessResponse(response)));
                }
            }
            catch
            {
                isConnected = false;
            }
        }

        private void ProcessResponse(string response)
        {
            if (response.StartsWith("OK"))
            {
                string totalItemPrice = response.Substring(3);
                MessageBox.Show($"Đặt món thành công!\nThành tiền món này: {totalItemPrice} VNĐ", "Thành công");
            }
            else if (response.Contains(";"))
            {
                string[] lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows.Clear();
                foreach (var line in lines)
                {
                    string[] parts = line.Split(';');
                    if (parts.Length == 3)
                    {
                        dataGridView1.Rows.Add(parts[0], parts[1], parts[2]);
                    }
                }
            }
        }

        private void btnPlaceOrder_Click(object sender, EventArgs e)
        {
            if (!isConnected || client == null)
            {
                MessageBox.Show("Chưa kết nối đến Server!");
                return;
            }

            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn món ăn!");
                return;
            }

            string tableID = colID.ToString();
            string menuID = dataGridView1.CurrentRow.Cells[0].Value.ToString();
            int quantity = (int)numericUpDown1.Value;

            if (quantity <= 0)
            {
                MessageBox.Show("Số lượng phải lớn hơn 0!");
                return;
            }

            writer.WriteLine($"ORDER {tableID} {menuID} {quantity}");
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isConnected)
            {
                try { writer.WriteLine("QUIT"); }
                catch { }
                isConnected = false;
                client?.Close();
            }
        }
    }
}