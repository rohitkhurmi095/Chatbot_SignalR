namespace TravelInsuranceAdvisor.Dto
{
    public class CountryResponseDto
    {
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string RatingRegionName { get; set; }
        public bool IsValid { get; set; }
    }
}
