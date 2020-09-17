using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TBot.Models
{
    public class AlertMessage
    {
        public string MessageId { get; set; }
        public bool Buy { get; set; }
        public bool Sell { get; set; }
        public bool Bear { get; set; }
        public bool Bull { get; set; }
        public long PercentageToTrade { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
