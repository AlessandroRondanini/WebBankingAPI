using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebBankingAPI.Models
{
    public class NewTransferMoneyBankAccount
    {
        public string iban { get; set; }
        public double? importo { get; set; }
    }
}
