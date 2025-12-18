using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace code_Lab6
{
    public partial class Staff : Form
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread receiveThread;
        private bool isConnected = false;

        public Staff()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string ip = txtIP.Text.Trim();
                int port;

                if (!int.TryParse(txtPort.Text.Trim(), out port))
                {
                    MessageBox.Show("Port phải là số nguyên!");
                    return;
                }

                client = new TcpClient(ip, port);
                NetworkStream stream = client.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                isConnected = true;
                lblStatus.Text = "Trạng thái: Đã kết nối (" + ip + ":" + port + ")";

                btnConnect.Enabled = false;
                txtIP.ReadOnly = true;
                txtPort.ReadOnly = true;

                SendMessage("AUTH STAFF");

                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        private void SendMessage(string message)
        {
            if (isConnected && client != null)
            {
                try
                {
                    writer.WriteLine(message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi gửi dữ liệu: " + ex.Message);
                }
            }
        }

        private void ReceiveData()
        {
            try
            {
                while (isConnected)
                {
                    string message = reader.ReadLine();
                    if (message == null) break;
                    this.Invoke(new Action(() => ProcessServerMessage(message)));
                }
            }
            catch
            {
                if (isConnected) MessageBox.Show("Mất kết nối với Server.");
                isConnected = false;
                Invoke(new Action(() => {
                    lblStatus.Text = "Trạng thái: Mất kết nối";
                    btnConnect.Enabled = true;
                    txtIP.ReadOnly = false;
                    txtPort.ReadOnly = false;
                }));
            }
        }

        private void ProcessServerMessage(string message)
        {
            if (message.StartsWith("TABLE_ORDER:"))
            {
                string[] parts = message.Substring(12).Split(',');
                if (parts.Length == 4)
                {
                    dgvOrders.Rows.Add(parts[0], parts[1], parts[2], parts[3]);
                }
            }
            else if (IsNumeric(message))
            {
                lblTotal.Text = message + " VNĐ";
                MessageBox.Show($"Thanh toán thành công! Tổng: {message}");

                string tableID = txtTableID.Text.Trim();
                RemoveOrdersFromGrid(tableID);
            }
        }

        private bool IsNumeric(string value)
        {
            long n;
            return long.TryParse(value, out n);
        }

        private void btnGetOrders_Click(object sender, EventArgs e)
        {
            dgvOrders.Rows.Clear();
            SendMessage("GET_ORDERS");
        }

        private void btnCharge_Click(object sender, EventArgs e)
        {
            string tableID = txtTableID.Text.Trim();
            if (string.IsNullOrEmpty(tableID))
            {
                MessageBox.Show("Vui lòng nhập số bàn!");
                return;
            }

            ExportBillToFile(tableID);
            SendMessage($"PAY {tableID}");
        }

        private void ExportBillToFile(string tableID)
        {
            try
            {
                string fileName = $"bill_Ban{tableID}.txt";
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.WriteLine($"HÓA ĐƠN THANH TOÁN - BÀN {tableID}");
                    sw.WriteLine($"Thời gian: {DateTime.Now}");
                    sw.WriteLine("------------------------------------------------");
                    sw.WriteLine("Món ăn\t\tSL\tThành tiền");

                    long tempTotal = 0;
                    foreach (DataGridViewRow row in dgvOrders.Rows)
                    {
                        if (row.Cells["TableID"].Value?.ToString() == tableID)
                        {
                            string dish = row.Cells["DishName"].Value.ToString();
                            string qty = row.Cells["Quantity"].Value.ToString();
                            string price = row.Cells["TotalPrice"].Value.ToString();

                            sw.WriteLine($"{dish}\t\t{qty}\t{price}");

                            long p;
                            if (long.TryParse(price, out p)) tempTotal += p;
                        }
                    }
                    sw.WriteLine("------------------------------------------------");
                    sw.WriteLine($"Tạm tính (Client): {tempTotal}");
                }
                MessageBox.Show($"Đã xuất hóa đơn tại: {Path.GetFullPath(fileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất file: " + ex.Message);
            }
        }

        private void RemoveOrdersFromGrid(string tableID)
        {
            for (int i = dgvOrders.Rows.Count - 1; i >= 0; i--)
            {
                if (dgvOrders.Rows[i].Cells["TableID"].Value?.ToString() == tableID)
                {
                    dgvOrders.Rows.RemoveAt(i);
                }
            }
        }

        private void Staff_FormClosing(object sender, FormClosingEventArgs e)
        {
            SendMessage("QUIT");
            isConnected = false;
            if (client != null) client.Close();
        }
    }
}