using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartyProductCore.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public decimal RateOfProduct { get; set; }
        public int Quantity { get; set; }
        public string PartyName { get; set; }
        public string ProductName { get; set; }
        public DateTime DateOfInvoice { get; set; }
        public decimal Total { get; set; }
    }
}
