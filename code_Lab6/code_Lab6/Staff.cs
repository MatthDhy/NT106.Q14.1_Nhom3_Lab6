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

                // Gửi định danh (Protocol)
                SendMessage("AUTH STAFF");

                // Bắt đầu luồng nhận tin
                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        // 2. Gửi lệnh đi (Common function)
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
            else
            {
                MessageBox.Show("Chưa kết nối đến Server!");
            }
        }

        // 3. Luồng nhận dữ liệu
        private void ReceiveData()
        {
            try
            {
                while (isConnected)
                {
                    string message = reader.ReadLine();
                    if (message == null) break;

                    // Đẩy về luồng UI để xử lý hiển thị
                    this.Invoke(new Action(() => ProcessServerMessage(message)));
                }
            }
            catch
            {
                if (isConnected) // Chỉ báo lỗi nếu đang trạng thái kết nối mà bị ngắt
                {
                    isConnected = false;
                    Invoke(new Action(() => {
                        lblStatus.Text = "Trạng thái: Mất kết nối";
                        btnConnect.Enabled = true;
                        txtIP.ReadOnly = false;
                        txtPort.ReadOnly = false;
                        MessageBox.Show("Đã mất kết nối với Server.");
                    }));
                }
            }
        }

        // 4. Xử lý logic phản hồi từ Server (QUAN TRỌNG)
        private void ProcessServerMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // -- XỬ LÝ DANH SÁCH ORDER (GET_ORDERS) --
            // Giả định Server trả về dạng: "TableID;DishName;Quantity;TotalPrice"
            // Hoặc: "TableID,DishName,Quantity,TotalPrice"
            // Ta kiểm tra nếu dòng tin nhắn có chứa các ký tự phân cách đặc trưng
            if (message.Contains(";") || message.Contains(","))
            {
                // Tách chuỗi dựa trên cả dấu chấm phẩy và dấu phẩy
                string[] parts = message.Split(new char[] { ';', ',' });

                // Kiểm tra xem có đủ 4 cột dữ liệu không (Bàn, Món, SL, Tiền)
                if (parts.Length >= 4)
                {
                    string tableId = parts[0].Trim();
                    string dishName = parts[1].Trim();
                    string quantity = parts[2].Trim();
                    string price = parts[3].Trim();

                    // Thêm vào DataGridView
                    dgvOrders.Rows.Add(tableId, dishName, quantity, price);
                }
            }
            // -- XỬ LÝ THANH TOÁN (PAY) --
            // Nếu Server trả về một con số nguyên -> Tổng tiền thanh toán
            else if (IsNumeric(message))
            {
                lblTotal.Text = message + " VNĐ";
                MessageBox.Show($"Thanh toán thành công! Tổng tiền: {message} VNĐ");

                // Xóa các món của bàn vừa thanh toán khỏi danh sách
                string currentTable = txtTableID.Text.Trim();
                RemoveOrdersFromGrid(currentTable);
            }
            // Các thông báo text khác (VD: "Server started", "Error", v.v.)
            else
            {
                // Có thể hiển thị lên Log hoặc bỏ qua
                // Console.WriteLine(message); 
            }
        }

        // Helper kiểm tra số
        private bool IsNumeric(string value)
        {
            long n;
            return long.TryParse(value, out n);
        }

        // 5. Nút Lấy danh sách Order (GET_ORDERS)
        private void btnGetOrders_Click(object sender, EventArgs e)
        {
            // Xóa dữ liệu cũ trên Grid để nhận dữ liệu mới
            dgvOrders.Rows.Clear();

            // Gửi lệnh yêu cầu Server gửi danh sách
            SendMessage("GET_ORDERS");
        }

        // 6. Nút Tính tiền (PAY)
        private void btnCharge_Click(object sender, EventArgs e)
        {
            string tableID = txtTableID.Text.Trim();
            if (string.IsNullOrEmpty(tableID))
            {
                MessageBox.Show("Vui lòng nhập số bàn cần thanh toán!");
                return;
            }

            // Xuất file hóa đơn trước (Client tự xử lý)
            ExportBillToFile(tableID);

            // Gửi lệnh lên Server để tính toán và xóa đơn
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
                        if (row.Cells["TableID"].Value?.ToString() == tableID)
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
                    sw.WriteLine($"Tổng tiền (Tạm tính): {clientTotal} VNĐ");
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