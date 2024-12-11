namespace TravelInsuranceAdvisor.Dto
{
    public class QuotePricingRequestDto
    {
        public DestinationDto[] Destinations { get; set; }
        public string State { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string ClubCode { get; set; }
        public int FirstPersonAge { get; set; }
        public int SecondPersonAge { get; set; }
        public string Channel { get; set; }
        public string PromoCode { get; set; }
        public bool IsClubMember { get; set; }
        public bool IsSingleTrip { get; set; }
    }
}
