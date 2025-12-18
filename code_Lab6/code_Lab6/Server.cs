using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace code_Lab6
{
    public partial class Server : Form
    {
<<<<<<< HEAD
=======
        TcpListener listener;
        bool running = false;

        Dictionary<int, MenuItem> menuMap = new Dictionary<int, MenuItem>();
        Dictionary<int, List<OrderItem>> ordersByTable = new Dictionary<int, List<OrderItem>>();
>>>>>>> 098dbd9aa4a6f7818e9b45f05f0c21643782687a

        public Server()
        {
            InitializeComponent();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            rtbLog.AppendText($"[{DateTime.Now}] Server is starting...\n");
            btnStart.Enabled = false;
            lblStatus.Text = "Status: Listening...";

<<<<<<< HEAD
=======
            LoadMenu();
            StartServer(5000);
        }

        void LoadMenu()
        {
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "menu.txt"
            );

            foreach (var line in File.ReadAllLines(path))
            {
                var p = line.Split(';');
                int id = int.Parse(p[0]);

                menuMap[id] = new MenuItem
                {
                    Id = id,
                    Name = p[1],
                    Price = int.Parse(p[2])
                };
            }
        }

        void StartServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            running = true;

            Thread t = new Thread(AcceptClients);
            t.IsBackground = true;
            t.Start();

            rtbLog.AppendText($"[{DateTime.Now}] Server started on port {port}\n");
        }

        void AcceptClients()
        {
            while (running)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread t = new Thread(() => HandleClient(client));
                t.IsBackground = true;
                t.Start();
            }
        }

        void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Invoke(new Action(() =>
                {
                    rtbLog.AppendText($"[{DateTime.Now}] Recv: {line}\n");
                }));

                string response = ProcessCommand(line);
                writer.WriteLine(response);

                if (line.StartsWith("QUIT"))
                    break;
            }

            client.Close();
            Invoke(new Action(() =>
            {
                rtbLog.AppendText($"[{DateTime.Now}] Client disconnected\n");
            }));
        }

        string ProcessCommand(string request)
        {
            string[] parts = request.Split(' ');
            string cmd = parts[0];

            switch (cmd)
            {
                case "MENU":
                    return HandleMenu();

                case "ORDER":
                    return HandleOrder(parts);

                case "GET_ORDERS":
                    return HandleGetOrders();

                case "PAY":
                    return HandlePay(parts);

                case "QUIT":
                    return "BYE";
            }

            return "UNKNOWN";
        }

        string HandleMenu()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var m in menuMap.Values)
                sb.AppendLine($"{m.Id};{m.Name};{m.Price}");
            return sb.ToString();
        }

        string HandleOrder(string[] parts)
        {
            int table = int.Parse(parts[1]);
            int menuId = int.Parse(parts[2]);
            int qty = int.Parse(parts[3]);

            if (!menuMap.ContainsKey(menuId))
                return "FAIL";

            if (!ordersByTable.ContainsKey(table))
                ordersByTable[table] = new List<OrderItem>();

            var m = menuMap[menuId];

            ordersByTable[table].Add(new OrderItem
            {
                TableNumber = table,
                MenuId = menuId,
                MenuName = m.Name,
                Quantity = qty,
                UnitPrice = m.Price
            });

            return "OK " + (qty * m.Price);
        }

        string HandleGetOrders()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var table in ordersByTable)
            {
                foreach (var item in table.Value)
                {
                    sb.AppendLine(
                        $"{table.Key};{item.MenuName};{item.Quantity};{item.Quantity * item.UnitPrice}"
                    );
                }
            }
            return sb.ToString();
        }

        string HandlePay(string[] parts)
        {
            int table = int.Parse(parts[1]);

            if (!ordersByTable.ContainsKey(table))
                return "TOTAL 0";

            int total = ordersByTable[table]
                .Sum(x => x.Quantity * x.UnitPrice);

            ordersByTable.Remove(table);

            return "TOTAL " + total;
>>>>>>> 098dbd9aa4a6f7818e9b45f05f0c21643782687a
        }
    }
}
