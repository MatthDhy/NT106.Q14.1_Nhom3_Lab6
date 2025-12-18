using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace code_Lab6
{
    public class MenuItem
    {   
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
    }

    public class OrderItem
    { 
        public int TableNumber { get; set; }
        public int MenuId { get; set; }
        public string MenuName { get; set; }
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }
    }

}
