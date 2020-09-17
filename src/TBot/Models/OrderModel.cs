using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TBot.Models
{
    public class OrderModel: BaseModel
    {
        public Guid Id { get; set; }
        public string MessageId { get; set; }
        public long OrderId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool Buy { get; set; }
        public bool Sell { get; set; }
        public long PercentageToTrade { get; set; }
        public bool Completed { get; set; }
        public decimal? BTCToTradeAmount { get; set; }
        public decimal? BTCToTradeAmountValue { get; set; }
    }
}
