using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace code_Lab6
{
    public partial class Client : Form
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread receiveThread;
        private bool isConnected = false;

        public Client()
        {
            InitializeComponent();
        }

        // 1. Kết nối Server
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string ip = txtIP.Text.Trim();
                int port;
                if (!int.TryParse(txtPort.Text.Trim(), out port))
                {
                    MessageBox.Show("Port không hợp lệ!");
                    return;
                }

                client = new TcpClient(ip, port);
                NetworkStream stream = client.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                isConnected = true;
                lblStatus.Text = "Trạng thái: Đã kết nối";
                btnConnect.Enabled = false;
                txtIP.ReadOnly = true;
                txtPort.ReadOnly = true;

                // Gửi định danh Khách hàng
                SendMessage("AUTH CUSTOMER");

                // Bắt đầu luồng nhận dữ liệu
                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                // Tự động yêu cầu Menu ngay khi kết nối
                SendMessage("MENU");
                btnConnect.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        // 2. Gửi tin nhắn đến Server
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
                    MessageBox.Show("Lỗi gửi: " + ex.Message);
                }
            }
        }

        // 3. Nhận dữ liệu từ Server
        private void ReceiveData()
        {
            try
            {
                while (isConnected)
                {
                    string message = reader.ReadLine();
                    if (message == null) break;

                    // Đẩy việc xử lý về luồng giao diện chính
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

        // 4. Xử lý logic phản hồi từ Server
        private void ProcessServerMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            // TH1: Nhận Menu (Server trả về các dòng: ID;Name;Price)
            // Giả sử server gửi từng dòng một hoặc gửi một chuỗi dài có xuống dòng
            if (message.Contains(";"))
            {
                // Định dạng: ID; Tên món; Giá
                string[] parts = message.Split(';');
                if (parts.Length >= 3)
                {
                    // Thêm vào Grid. Cột cuối để trống cho user nhập số lượng
                    dgvMenu.Rows.Add(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), "");
                }
            }
            // TH2: Phản hồi đặt món thành công
            else if (message.StartsWith("OK"))
            {
                MessageBox.Show("Đặt món thành công! " + message);

                // (Tùy chọn) Reset số lượng đã nhập về 0
                foreach (DataGridViewRow row in dgvMenu.Rows)
                {
                    row.Cells["OrderQuantity"].Value = "";
                }
            }
            else
            {
                // Các thông báo khác
                // MessageBox.Show("Server says: " + message);
            }
        }

        // 5. Nút Đặt món (Place Order)
        private void btnOrder_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Vui lòng kết nối trước!");
                return;
            }

            string tableID = nudTable.Value.ToString();
            bool hasOrder = false;

            // Duyệt qua DataGridView để tìm các món có nhập Số lượng
            foreach (DataGridViewRow row in dgvMenu.Rows)
            {
                string qtyStr = row.Cells["OrderQuantity"].Value?.ToString();
                int quantity;

                // Kiểm tra nếu ô số lượng có dữ liệu và là số > 0
                if (!string.IsNullOrEmpty(qtyStr) && int.TryParse(qtyStr, out quantity) && quantity > 0)
                {
                    string dishID = row.Cells["DishID"].Value.ToString();

                    // Gửi lệnh theo Protocol: ORDER <Bàn> <ID_Món> <SL>
                    string orderCmd = $"ORDER {tableID} {dishID} {quantity}";
                    SendMessage(orderCmd);

                    hasOrder = true;
                }
            }

            if (!hasOrder)
            {
                MessageBox.Show("Vui lòng nhập số lượng cho ít nhất một món ăn!");
            }
        }

        // 6. Ngắt kết nối khi đóng form
        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            SendMessage("QUIT");
            isConnected = false;
            if (client != null) client.Close();
        }
    }
}