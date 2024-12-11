namespace TravelInsuranceAdvisor.Dto
{
    public class QuoteResponseDto
    {
        public string QuoteId { get; set; }
        public string QuoteNumber { get; set; }
        public bool IsSking { get; set; }
        public bool IsCruise { get; set; }
        public string PolicyStatus { get; set; }
        public string ProductAlias { get; set; }
        public PremiumsDto Premiums { get; set; }
        public bool AllowBindSubmission { get; set; }
        public bool IsSuccessful { get; set; }
        public string PricingDate { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string[] Ages { get; set; }
        public DestinationDto[] Destinations { get; set; }
        public int DependentsCount { get; set; }
        public bool IsSingleTrip { get; set; }
        public int StateId { get; set; }
        public string PromoCode { get; set; }
        public string AutoClub { get; set; }
        public bool IsClubMember { get; set; }
        public string Channel { get; set; }
        public bool AcknowledgementFlag { get; set; }
        public int BuyInAdvance { get; set; }
        public TravelerDto[] Travelers { get; set; }
        public string StepName { get; set; }
        public object Partner { get; set; }
        public bool MarketingConsent { get; set; }

        #nullable enable
        public string? QuoteName { get; set; }
        public string? Excess { get; set; }
        public string? ProductType { get; set; }
        public string? ClubProductGuid { get; set; }
        public EmergencyContactDto? EmergencyContact { get; set; }
    }
}
