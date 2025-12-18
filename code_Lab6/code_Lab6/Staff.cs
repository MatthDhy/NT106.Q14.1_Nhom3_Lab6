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
            // Đăng ký sự kiện khi nhập số bàn sẽ tự động tính lại tiền
            txtTableID.TextChanged += TxtTableID_TextChanged;
        }

        // Sự kiện khi thay đổi số bàn -> Tính lại tiền hiển thị
        private void TxtTableID_TextChanged(object sender, EventArgs e)
        {
            CalculateLocalTotal();
        }

        // Hàm tính tổng tiền "Tạm tính" dựa trên dữ liệu đang có trên lưới
        private void CalculateLocalTotal()
        {
            string currentTableID = txtTableID.Text.Trim();
            long total = 0;

            if (string.IsNullOrEmpty(currentTableID))
            {
                lblTotal.Text = "0 VNĐ";
                return;
            }

            // Duyệt qua toàn bộ lưới để cộng tiền các món thuộc bàn này
            foreach (DataGridViewRow row in dgvOrders.Rows)
            {
                // Kiểm tra null để tránh lỗi
                var cellTable = row.Cells["TableID"].Value;
                var cellPrice = row.Cells["TotalPrice"].Value;

                if (cellTable != null && cellTable.ToString() == currentTableID)
                {
                    long price;
                    if (cellPrice != null && long.TryParse(cellPrice.ToString(), out price))
                    {
                        total += price;
                    }
                }
            }

            lblTotal.Text = $"{total:N0} VNĐ"; // Định dạng số có dấu phẩy (VD: 50,000 VNĐ)
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
                    MessageBox.Show("Port phải là số nguyên!");
                    return;
                }

                client = new TcpClient(ip, port);
                NetworkStream stream = client.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                isConnected = true;
                lblStatus.Text = $"Trạng thái: Đã kết nối ({ip}:{port})";
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
            if (isConnected && client != null && client.Connected)
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
                if (isConnected)
                {
                    isConnected = false;
                    Invoke(new Action(() => {
                        lblStatus.Text = "Trạng thái: Mất kết nối";
                        btnConnect.Enabled = true;
                        txtIP.ReadOnly = false;
                        txtPort.ReadOnly = false;
                    }));
                }
            }
        }

        // 4. Xử lý logic phản hồi
        private void ProcessServerMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // TH1: Nhận danh sách món (Get Orders)
            if (message.Contains(";") || message.Contains(","))
            {
                string[] parts = message.Split(new char[] { ';', ',' });
                if (parts.Length >= 4)
                {
                    string tableId = parts[0].Trim();
                    string dishName = parts[1].Trim();
                    string quantity = parts[2].Trim();
                    string price = parts[3].Trim();

                    dgvOrders.Rows.Add(tableId, dishName, quantity, price);

                    // Sau khi thêm món mới, gọi hàm tính lại tổng tiền ngay
                    CalculateLocalTotal();
                }
            }
            // TH2: Nhận kết quả thanh toán từ Server (PAY)
            else if (IsNumeric(message))
            {
                // Server trả về số tiền chốt -> Cập nhật hiển thị và báo thành công
                lblTotal.Text = $"{long.Parse(message):N0} VNĐ";
                MessageBox.Show($"Thanh toán thành công! Tổng thực thu: {message} VNĐ");

                // Xóa các món của bàn đã thanh toán khỏi lưới
                string currentTable = txtTableID.Text.Trim();
                RemoveOrdersFromGrid(currentTable);

                // Reset lại hiển thị tiền về 0
                CalculateLocalTotal();
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
            lblTotal.Text = "0 VNĐ"; // Reset tiền về 0 trước khi tải mới
            SendMessage("GET_ORDERS");
        }

        private void btnCharge_Click(object sender, EventArgs e)
        {
            string tableID = txtTableID.Text.Trim();
            if (string.IsNullOrEmpty(tableID))
            {
                MessageBox.Show("Vui lòng nhập số bàn cần thanh toán!");
                return;
            }

            ExportBillToFile(tableID);
            SendMessage($"PAY {tableID}");
        }

        private void ExportBillToFile(string tableID)
        {
            try
            {
                string fileName = $"bill_Ban{tableID}_{DateTime.Now.Ticks}.txt";
                long clientTotal = 0;

                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.WriteLine($"HÓA ĐƠN THANH TOÁN - BÀN {tableID}");
                    sw.WriteLine($"Thời gian: {DateTime.Now}");
                    sw.WriteLine("----------------------------------------------------------");
                    sw.WriteLine("Món ăn\t\t\tSL\tThành tiền");

                    foreach (DataGridViewRow row in dgvOrders.Rows)
                    {
                        var cellTable = row.Cells["TableID"].Value;
                        if (cellTable != null && cellTable.ToString() == tableID)
                        {
                            string dish = row.Cells["DishName"].Value?.ToString() ?? "";
                            string qty = row.Cells["Quantity"].Value?.ToString() ?? "0";
                            string price = row.Cells["TotalPrice"].Value?.ToString() ?? "0";

                            sw.WriteLine($"{dish.PadRight(20)}\t{qty}\t{price}");

                            long p;
                            if (long.TryParse(price, out p)) clientTotal += p;
                        }
                    }
                    sw.WriteLine("----------------------------------------------------------");
                    sw.WriteLine($"Tổng tiền: {clientTotal:N0} VNĐ");
                }
                MessageBox.Show($"Đã xuất hóa đơn: {fileName}");
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
                var cellTable = dgvOrders.Rows[i].Cells["TableID"].Value;
                if (cellTable != null && cellTable.ToString() == tableID)
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