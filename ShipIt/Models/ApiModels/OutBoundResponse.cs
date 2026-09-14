namespace ShipIt.Models.ApiModels
{
    public class OutBoundResponse : Response
    {
        public double TotalWeightKg { get; set; }

        public int TrucksRequired { get; set; }
        public OutBoundResponse(double totalWeight, int trucksRequired)
        {
            this.TotalWeightKg = totalWeight;
            this.TrucksRequired = trucksRequired;
            Success = true;
        }
     
        public OutBoundResponse() { }
    }
}