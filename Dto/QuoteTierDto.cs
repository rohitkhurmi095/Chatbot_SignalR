namespace TravelInsuranceAdvisor.Dto
{
    public class QuoteTierDto
    {
        public string Type { get; set; }
        public bool MostPopular { get; set; }
        public string DefaultExcess { get; set; }
        public bool DefaultSkii { get; set; }
        public bool DefaultCruise { get; set; }
        public ProductDto[] Products { get; set; }
    }
}
