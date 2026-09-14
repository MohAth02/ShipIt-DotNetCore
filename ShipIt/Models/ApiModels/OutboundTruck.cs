using System.Collections.Generic;

namespace ShipIt.Models.ApiModels
{
    public class OutboundTruck
    {
        public double TotalWeightKg { get; set; }
        public List<OrderLine> OrderLines { get; set; }

        public OutboundTruck()
        {
            OrderLines = new List<OrderLine>();
        }
    }
}
