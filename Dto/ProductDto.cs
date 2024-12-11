namespace TravelInsuranceAdvisor.Dto
{
    public class ProductDto
    {
        public bool IsSki { get; set; }
        public double MembershipDisc { get; set; }
        public string Excess { get; set; }
        public double StampDuty { get; set; }
        public double BasePremium { get; set; }
        public double CruisePremium { get; set; }
        public double SkiingPremium { get; set; }
        public double ExcessPremium { get; set; }
        public double Gst { get; set; }
        public bool IsCruise { get; set; }
        public double PromoCodeDisc { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public string ProductAlias { get; set; }
        public Guid ClubProductGuid { get; set; }
        public double TotalPremium { get; set; }
        public double MultiTripLengthPremium { get; set; }
        public double RegularPremium { get; set; }
    }
}
