using System.Collections.Generic;

namespace ShipIt.Models.ApiModels
{
    public class OutBoundResponse : Response
    {
        public double TotalWeightKg { get; set; }

        public int TrucksRequired { get; set; }
        public List<OutboundTruck> Trucks { get; set; }

        public OutBoundResponse()
        {
            this.TotalWeightKg = 0;
            this.TrucksRequired = 0;
            this.Trucks = new List<OutboundTruck>();
            Success = true;
        }
    }
}