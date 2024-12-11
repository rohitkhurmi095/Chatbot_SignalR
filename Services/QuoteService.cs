using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using System.Text.Json;
using TravelInsuranceAdvisor.Dto;
using TravelInsuranceAdvisor.Hubs;
namespace TravelInsuranceAdvisor.Services
{
    public class QuoteService
    {

        private readonly ILogger<ChatHub> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public QuoteService(ILogger<ChatHub> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }


        //--------------
        //Countries Api
        //--------------
        //GET
        public async Task<List<CountryResponseDto>> GetCountries()
        {
            try
            {
                var apiUrl = _configuration["TravelInsuranceApiUrls:CountriesApi"];

                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseContent))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return System.Text.Json.JsonSerializer.Deserialize<List<CountryResponseDto>>(responseContent, options);
                }
                return new List<CountryResponseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching countries from API.");
                return null;
            }
        }

        //---------------
        //Valate Country
        //---------------
        public async Task<CountryResponseDto> ValidateCountry(string countryName)
        {
            try
            {
                var countries = await GetCountries();
                var country = countries?.FirstOrDefault(c => c.CountryName.Equals(countryName, StringComparison.OrdinalIgnoreCase));
                if (country != null)
                {
                    return new CountryResponseDto { CountryCode = country.CountryCode, CountryName = country.CountryName, RatingRegionName = country.RatingRegionName, IsValid = true };
                }
                else
                {
                    return new CountryResponseDto { CountryName = countryName, IsValid = false };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating country.");
                return new CountryResponseDto { CountryName = countryName, IsValid = false };
            }
        }


        //-------------
        //Get Quote 
        //-------------
        //PUT
        public async Task<QuoteResponseDto> GetQuote(QuoteRequestDto quoteRequestDto)
        {
            try
            {
                // Populate remaining fields with hardcoded values
                //StateId = 1 | State - NSW
                quoteRequestDto.PricingDate = DateTime.Now.AddDays(1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture); ;
                quoteRequestDto.IsSingleTrip = true;
                quoteRequestDto.StateId = 1;
                quoteRequestDto.PromoCode = quoteRequestDto.PromoCode == null ? "" : quoteRequestDto.PromoCode;
                quoteRequestDto.IsClubMember = false;
                quoteRequestDto.Channel = "Online";
                quoteRequestDto.AcknowledgementFlag = true;
                quoteRequestDto.MembershipNumber = "";
                quoteRequestDto.MarketingConsent = false;
                quoteRequestDto.Travelers = new TravelerDto[] {}; 
                quoteRequestDto.ClubProductGuid = new Guid("45e45beb-ee6e-4c4d-ba79-84532dd47b63");
                quoteRequestDto.StepName = "Home";
                quoteRequestDto.AutoClub = "W2C";

                var json = JsonConvert.SerializeObject(quoteRequestDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var apiUrl = _configuration["TravelInsuranceApiUrls:GetQuoteApi"];

                var response = await _httpClient.PutAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseContent))
                {     
                    QuoteResponseDto quoteResponseDto = JsonConvert.DeserializeObject<QuoteResponseDto>(responseContent);

                    return quoteResponseDto;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating quote from API.");
                return null;
            }
        }


        //------------------
        //Get Quote Pricing
        //------------------
        //POST
        public async Task<List<QuoteTierDto>> GetQuotePricing(QuotePricingRequestDto pricingRequestDto)
        {
            try
            {
                // Populate remaining fields with hardcoded values
                //State - NSW 
                pricingRequestDto.PromoCode = pricingRequestDto.PromoCode == null ? "" : pricingRequestDto.PromoCode;
                pricingRequestDto.IsClubMember = false;
                pricingRequestDto.IsSingleTrip = true;
                pricingRequestDto.State = "NSW";
                pricingRequestDto.Channel = "Online";
                pricingRequestDto.ClubCode = "W2C";

                var json = JsonConvert.SerializeObject(pricingRequestDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var apiUrl = _configuration["TravelInsuranceApiUrls:GetQuotePricingApi"];

                var response = await _httpClient.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseContent))
                {
                    var tiers = JsonConvert.DeserializeObject<List<QuoteTierDto>>(responseContent);
                    return tiers;
                }
                else
                {
                    return null; 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching quote pricing from API.");
                return null; 
            }
        }


        //------------------
        //Get Quote Details
        //------------------
        //PUT
        public async Task<QuoteResponseDto> GetQuoteDetails(QuoteRequestDto quoteDetailsRequestDto, QuoteResponseDto quoteResponse)
        {
            try
            {
                quoteDetailsRequestDto.QuoteId = new Guid(quoteResponse.QuoteId);
                quoteDetailsRequestDto.PricingDate = quoteResponse.PricingDate;
                quoteDetailsRequestDto.FromDate = quoteResponse.FromDate;
                quoteDetailsRequestDto.ToDate = quoteResponse.ToDate;
                quoteDetailsRequestDto.Ages = quoteResponse.Ages;
                quoteDetailsRequestDto.Destinations = quoteResponse.Destinations;
                quoteDetailsRequestDto.IsSingleTrip = quoteResponse.IsSingleTrip;
                quoteDetailsRequestDto.AutoClub = quoteResponse.AutoClub;
                quoteDetailsRequestDto.StateId = quoteResponse.StateId;
                quoteDetailsRequestDto.DependentsCount = quoteResponse.DependentsCount;
                quoteDetailsRequestDto.IsSingleTrip = quoteResponse.IsSingleTrip;
                quoteDetailsRequestDto.IsClubMember = quoteResponse.IsClubMember;
                quoteDetailsRequestDto.Channel = quoteResponse.Channel;
                quoteDetailsRequestDto.MarketingConsent = quoteResponse.MarketingConsent;
                quoteDetailsRequestDto.Travelers[0].Address.CountryCode = quoteResponse.Destinations[0].CountryCode;
                quoteDetailsRequestDto.PromoCode = quoteResponse.PromoCode;
                quoteDetailsRequestDto.ClubProductGuid = new Guid(quoteResponse.ClubProductGuid);

                //populate remaining fields with hardcoded values
                quoteDetailsRequestDto.EmergencyContact = new EmergencyContactDto()
                {
                    FirstName = "",
                    LastName = "",
                    Email = "",
                    Phone = ""
                };
                quoteDetailsRequestDto.Travelers[0].Role = "primary";
                quoteDetailsRequestDto.Travelers[0].MedicalRequired = false;
                quoteDetailsRequestDto.Travelers[0].TravelerId = null;
                quoteDetailsRequestDto.Travelers[0].Address.StateId = 1;
                quoteDetailsRequestDto.StepName = "step3";
                quoteDetailsRequestDto.MembershipNumber = "";

                var json = JsonConvert.SerializeObject(quoteDetailsRequestDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var apiUrl = _configuration["TravelInsuranceApiUrls:GetQuoteDetailsApi"];

                var response = await _httpClient.PutAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseContent))
                {
                    QuoteResponseDto quoteResponseDto = JsonConvert.DeserializeObject<QuoteResponseDto>(responseContent);

                    return quoteResponseDto;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching quote details from API.");
                return null;
            }
        }
    }
}
