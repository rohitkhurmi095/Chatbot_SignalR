namespace TravelInsuranceAdvisor.Dto
{
    public class QuoteRequestDto
    {
        public string PricingDate { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string[] Ages { get; set; }
        public DestinationDto[] Destinations { get; set; }
        public int DependentsCount { get; set; }
        public bool IsSingleTrip { get; set; }
        public int StateId { get; set; }
        public string AutoClub { get; set; }
        public bool IsClubMember { get; set; }
        public string Channel { get; set; }
        public bool AcknowledgementFlag { get; set; }
        public string MembershipNumber { get; set; }
        public bool MarketingConsent { get; set; }
        public TravelerDto[] Travelers { get; set; }
        public Guid ClubProductGuid { get; set; }
        public string StepName { get; set; }
        public string PromoCode { get; set; }

        #nullable enable
        public Guid? QuoteId { get; set; }
        public EmergencyContactDto? EmergencyContact { get; set; }
    }
}
