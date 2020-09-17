using System;
using System.Collections.Generic;

namespace TBot.Models
{
    public class StateModel: BaseModel
    {
        public List<OrderModel> OpenOrders { get; set; }
        public List<OrderModel> CompletedOrders { get; set; }
        public List<ShareModel> Shares { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
        public decimal? BTCAmountValue { get; set; }
        public decimal? InPercentage { get; set; }
        public bool GmailTokenIsValid { get; set; }
    }
}
