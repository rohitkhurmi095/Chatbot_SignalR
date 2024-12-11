namespace TravelInsuranceAdvisor.Dto
{
    public class PremiumsDto
    {
        public decimal Product { get; set; }
        public decimal Excess { get; set; }
        public int MultiTripLength { get; set; }
        public decimal Cruise { get; set; }
        public decimal Skiing { get; set; }
        public decimal Medical { get; set; }
        public decimal PromoCodeDiscount { get; set; }
        public decimal MembershipDiscount { get; set; }
        public decimal Gst { get; set; }
        public decimal StampDuty { get; set; }
        public decimal TotalCost { get; set; }
    }
}
