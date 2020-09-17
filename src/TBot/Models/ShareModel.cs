using System;

namespace TBot.Models
{
    public class ShareModel
    {
        public string Name { get; set; }
        public Guid Key { get; set; }
        public decimal Amount { get; set; }
        public decimal InitialValue { get; set; }
        public decimal? Profit { get; set; }
        public decimal? Value { get; set; }
    }
}
