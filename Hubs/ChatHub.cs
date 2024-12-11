using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Globalization;
using Newtonsoft.Json;
using System.Text;
using TravelInsuranceAdvisor.Dto;
using TravelInsuranceAdvisor.Services;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using TravelInsuranceAdvisor.Helpers;
using System.Diagnostics;

namespace TravelInsuranceAdvisor.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly QuoteService _quoteService;
        private readonly ValidatonHelper _helper;

         #nullable enable
        //-----------------
        //State Management
        //-----------------
        // ConcurrentDictionary to track the state of each user by their connection ID
        private static ConcurrentDictionary<string, (string state, QuoteRequestDto quoteRequestDto, QuotePricingRequestDto quotePricingRequestDto, QuoteResponseDto? quoteResponseDto, List<QuoteTierDto>? tierList, string? selectedQuoteType, QuoteRequestDto? quoteDetailsRequest, QuoteResponseDto? quoteDetailsResponse)> userStates = new ConcurrentDictionary<string, (string state, QuoteRequestDto quoteRequestDto, QuotePricingRequestDto quotePricingRequestDto, QuoteResponseDto? quoteResponseDto, List<QuoteTierDto>? tierList, string? selectedQuoteType, QuoteRequestDto? quoteDetailsRequestDto, QuoteResponseDto? quoteDetailsResponseDto)>();
        #nullable disable

        public ChatHub(ILogger<ChatHub> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration, QuoteService quoteService, ValidatonHelper helper)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _quoteService = quoteService;
            _helper = helper;
        }


        //---------------------------
        //Start Chatbot conversation
        //---------------------------
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;

            if (!userStates.ContainsKey(connectionId))
            {
                QuoteRequestDto newQuoteRequestDto = new QuoteRequestDto();
                QuotePricingRequestDto newQuotePricingRequestDto = new QuotePricingRequestDto();
                QuoteRequestDto quoteDetailsRequestDto = new QuoteRequestDto();
                QuoteResponseDto quoteDetailsResponse = new QuoteResponseDto();

                userStates[connectionId] = ("ASK_Destination", newQuoteRequestDto, newQuotePricingRequestDto, null, null, null, null, null);
                await Clients.Caller.SendAsync("ReceiveMessage", "Chatbot", "Welcome to Travel Insurance! Let's get started. Where are you traveling to?");
            }

            await base.OnConnectedAsync();
        }

        //-------------------------------------
        // Handle incoming messages from users
        //-------------------------------------
        public async Task SendMessage(string user, string message)
        {
            _logger.LogInformation("Received message from {User}: {Message}", user, message);

            // Check if the message is empty
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogInformation("Empty message received, ignoring.");
                return;
            }

            // Process the user's message and get the chatbot's response
            string botResponse = await GetChatbotResponse(Context.ConnectionId, message);
            _logger.LogInformation("Chatbot response: {Response}", botResponse);

            // Send the user's message and the chatbot's response back to the caller
            await Clients.Caller.SendAsync("ReceiveMessage", user, message);
            await Clients.Caller.SendAsync("ReceiveMessage", "Chatbot", botResponse);
        }

        //-----------------------
        //Update StepIndicators
        //----------------------
        public async Task MarkSectionAsComplete(string sectionId)
        {
            await Clients.All.SendAsync("SectionCompleted", sectionId);
        }


        //=======================
        //Get Chatbot's Response
        //=======================
        private async Task<string> GetChatbotResponse(string connectionId, string message)
        {
            // Check if the user is new and initialize their state
            if (!userStates.ContainsKey(connectionId))
            {
                QuoteRequestDto newQuoteRequestDto = new QuoteRequestDto();
                QuotePricingRequestDto newQuotePricingRequestDto = new QuotePricingRequestDto();
                userStates[connectionId] = ("ASK_Destination", newQuoteRequestDto, newQuotePricingRequestDto, null, null, null, null, null);
                return "Welcome to Travel Insurance! Let's get started. Where are you traveling to?";
            }

            // Get the current state of the user
            var (state, quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto) = userStates[connectionId];

            switch (state)
            {
                //***********************
                //STEP1: Quote Selection
                //***********************
                //Country
                case "ASK_Destination":
                    //Transition to the next state and ask for the DepartDate
                    var countryResponse = await _quoteService.ValidateCountry(message);
                    if (countryResponse != null && countryResponse.IsValid)
                    {
                        DestinationDto destination = new DestinationDto()
                        {
                            CountryCode = countryResponse.CountryCode,
                            CountryName = countryResponse.CountryName,
                            RatingRegionName = countryResponse.RatingRegionName
                        };
                        quoteRequestDto.Destinations = new DestinationDto[] { destination };
                        quotePricingRequestDto.Destinations = new DestinationDto[] { destination };

                        //Transition to the next state 
                        userStates[connectionId] = ("ASK_DepartDate", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "Got it! When does your trip start?";
                    }
                    else
                    {
                        return $"{message} is not recognized as a valid country. Kindly enter a valid country name.";
                    }
                //Depart Date
                case "ASK_DepartDate":
                    if (_helper.IsValidDate(message))
                    {
                        quoteRequestDto.FromDate = message;
                        quotePricingRequestDto.FromDate = message;

                        //Transition to the next state 
                        userStates[connectionId] = ("ASK_ReturnDate", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "And when will you return?";
                    }
                    else
                    {
                        return "Please provide the date in the format dd/mm/yyyy.";

                    }
                //Return Date
                case "ASK_ReturnDate":
                    if (_helper.IsValidDate(message))
                    {
                        quoteRequestDto.ToDate = message;
                        quotePricingRequestDto.ToDate = message;

                        //Transition to the next state 
                        userStates[connectionId] = ("ASK_Adult1_Age", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "How old is the first adult traveler?";
                    }
                    else
                    {
                        return "Please provide the date in the format dd/mm/yyyy.";

                    }
                //Adult1 Age
                case "ASK_Adult1_Age":
                    if (_helper.IsValidAge(message))
                    {
                        quoteRequestDto.Ages = new string[] { message };
                        quotePricingRequestDto.FirstPersonAge = int.Parse(message);

                        //Transition to the next state and ask for the Adult2 Age
                        userStates[connectionId] = ("ASK_Adult2_Required", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return $"Is there a secondary traveler? (y/n)";
                    }
                    else
                    {
                        return "The age of the traveler must be between 18 and 75. Please enter a valid age.";
                    }

                //Is Adult2 Reuired
                case "ASK_Adult2_Required":
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (message.Trim().ToLower() == "y")
                        {
                            //Transition to the next state
                            userStates[connectionId] = ("ASK_Adult2_Age", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                            return $"How old is the second adult traveler?";
                        }
                        else if (message.Trim().ToLower() == "n")
                        {
                            //Skip ASK_Adult2_Age
                            //Transition to the next state 
                            userStates[connectionId] = ("ASK_Dependents", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                            return "How many dependents will be traveling with you?";
                        }
                        else
                        {
                            return "Please enter a valid response (y/n).";
                        }
                    }
                    else
                    {
                        return "Please enter a valid response (y/n).";
                    }
                //Adult2 Age (Optional)
                case "ASK_Adult2_Age":
                    if (_helper.IsValidAge(message))
                    {
                        quoteRequestDto.Ages = new string[] { quoteRequestDto.Ages[0], message };
                        quotePricingRequestDto.SecondPersonAge = int.Parse(message);

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_Dependents", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "How many dependents will be traveling with you?";
                    }
                    else
                    {
                        return "The age of the traveler must be between 18 and 75. Please enter a valid age.";
                    }
                //No.Of Dependents
                case "ASK_Dependents":
                    if (!string.IsNullOrWhiteSpace(message) && int.TryParse(message, out int dependents))
                    {
                        quoteRequestDto.DependentsCount = dependents;

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_PromoQues", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return $"Do you have a promo code you'd like to apply? (y/n)";
                    }
                    else
                    {
                        return "Please enter a valid number for the dependents.";
                    }
                //Have PromoCode
                case "ASK_PromoQues":
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (message.Trim().ToLower() == "y")
                        {
                            //Transition to the next state
                            userStates[connectionId] = ("ASK_PromoCode", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                            return $"Please enter a valid promo code.";
                        }
                        else if (message.Trim().ToLower() == "n")
                        {
                            //Skip ASK_PromoCode
                            //Get Quote and QuotePricing - API Calls
                            quoteResponseDto = await _quoteService.GetQuote(quoteRequestDto);
                            tierList = await _quoteService.GetQuotePricing(quotePricingRequestDto);

                            //Transition to the next state
                            userStates[connectionId] = ("ASK_TierSelection", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);

                            //Show Quotes
                            return ShowQuotes(tierList);
                        }
                        else
                        {
                            return "Please enter a valid response (y/n).";
                        }
                    }
                    else
                    {
                        return "Please enter a valid response (y/n).";
                    }
                //Promo Code
                case "ASK_PromoCode":
                    if (!string.IsNullOrEmpty(message))
                    {
                        quoteRequestDto.PromoCode = message;
                        quotePricingRequestDto.PromoCode = message;

                        //Get Quote and QuotePricing - API Calls
                        quoteResponseDto = await _quoteService.GetQuote(quoteRequestDto);
                        tierList = await _quoteService.GetQuotePricing(quotePricingRequestDto);

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_TierSelection", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);

                        //Show Quotes
                        return ShowQuotes(tierList);
                    }
                    else
                    {
                        return "Please enter a valid promo code!";
                    }
                //Quote Tier Selection
                case "ASK_TierSelection":
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        string selectedTier = message.Trim();

                        // List of valid tiers 
                        var validTiers = new[] { "Basic Cover", "Top Cover", "Essentials Cover" };

                        // Check if the selected tier is valid (case-insensitive)
                        if (validTiers.Contains(selectedTier, StringComparer.OrdinalIgnoreCase))
                        {
                            //** Marking "step1" as completed
                            await MarkSectionAsComplete("step1");

                            selectedQuoteType = selectedTier;

                            // Return a confirmation message and proceed to next steps
                            userStates[connectionId] = ("ASK_Adult1_FirstName", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                            return $"You've selected the {selectedTier} quote! Great choice! Now, let's proceed with gathering your details. Can I have your first name, please?";
                        }
                        else
                        {
                            return "Please enter a valid tier: 'Basic Cover', 'Top Cover', or 'Essentials Cover'";
                        }
                    }
                    else
                    {
                        return "Please type a quote tier to proceed.";
                    }


                //************************
                //STEP2: Traveler Details
                //************************
                //-------------------------
                //Primary Traveler Details
                //-------------------------
                //Fist Name
                case "ASK_Adult1_FirstName":
                    if (_helper.IsValidName(message))
                    {
                        quoteDetailsRequestDto = new QuoteRequestDto
                        {
                            // Initialize the Travelers array with 1 element
                            Travelers = new TravelerDto[1] { new TravelerDto() }
                        };

                        quoteDetailsRequestDto.Travelers[0].FirstName = message.Trim();

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_Adult1_LastName", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "Great! Now, can you please provide your last name?";
                    }
                    else{
                        return "Please enter a valid first name using only alphabetic characters.";
                    }
                //Last Name
                case "ASK_Adult1_LastName":
                if (_helper.IsValidName(message))
                {
                    quoteDetailsRequestDto.Travelers[0].LastName = message.Trim();

                    //Transition to the next state
                    userStates[connectionId] = ("ASK_Adult1_DOB", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                    return "Great! Now, can you please provide your Date Of Birth?";
                }
                else
                {
                    return "Please enter a valid last name using only alphabetic characters.";
                }
                //DateOfBirth
                case "ASK_Adult1_DOB":
                    if (_helper.IsValidDate(message))
                    {
                        quoteDetailsRequestDto.Travelers[0].DateOfBirth = message;

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_Adult1_Email", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "Thanks! Now, can you please provide your email address?";
                    }
                    else
                    {
                        return "Please provide the date in the format dd/mm/yyyy.";
                    }
                //Email Address
                case "ASK_Adult1_Email":
                    if (_helper.IsValidEmail(message))
                    {
                        quoteDetailsRequestDto.Travelers[0].Email = message;

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_Adult1_MobileNo", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "Thanks! Now, can you please provide your mobile number?";
                    }
                    else
                    {
                        return "Please provide a valid email address.";
                    }
                //Mobile Number
                case "ASK_Adult1_MobileNo":
                    if (_helper.IsValidPhoneNumber(message))
                    {
                        quoteDetailsRequestDto.Travelers[0].Phone = message;

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_Adult1_Address1", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "Thanks! Please provide your Street Address.";
                    }
                    else
                    {
                        return "Please provide a valid mobile number.";
                    }
                //Address
                case "ASK_Adult1_Address1":
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        quoteDetailsRequestDto.Travelers[0].Address = new AddressDto();
                        quoteDetailsRequestDto.Travelers[0].Address.Address = message.Trim();

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_Adult1_Suburb", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "Thanks! Now, please provide your Suburb.";
                    }
                    else
                    {
                        return "Street address cannot be empty. Please provide a valid street address.";
                    }
                case "ASK_Adult1_Suburb":
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        quoteDetailsRequestDto.Travelers[0].Address.City = message.Trim();

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_Adult1_PostalCode", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return "Thanks! Now, please provide your Postal Code";
                    }
                    else
                    {
                        return "Suburb cannot be empty. Please provide a valid suburb.";
                    }
                case "ASK_Adult1_PostalCode":
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        quoteDetailsRequestDto.Travelers[0].Address.PostalCode = message.Trim();
                        //Prepopulate the state (NSW)
                        string state1 = quotePricingRequestDto.State;

                        //Transition to the next state
                        userStates[connectionId] = ("ASK_Confirm_Adult1_Address", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                        return $"Thanks! Please confirm your address details: Address: {quoteDetailsRequestDto.Travelers[0].Address.Address}, {quoteDetailsRequestDto.Travelers[0].Address.City}, {state1}, {quoteDetailsRequestDto.Travelers[0].Address.PostalCode} \nDo you confirm the address is correct? (y/n)";
                    }
                    else
                    {
                        return "Postal code cannot be empty. Please provide a valid postal code.";
                    }
                //Confirm Address
                case "ASK_Confirm_Adult1_Address":
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (message.Trim().ToLower() == "y")
                        {
                            //Get QuoteDetails
                            quoteDetailsResponseDto = await _quoteService.GetQuoteDetails(quoteDetailsRequestDto, quoteResponseDto);

                            //Transition to the next state
                            userStates[connectionId] = ("ASK_Confirm_And_Finalize_Quote", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                            return $"Are you sure, you wish to confirm and finalize this quote? (y/n)";
                        }
                        else if (message.Trim().ToLower() == "n")
                        {
                            //Transition to the next state
                            userStates[connectionId] = ("ASK_Adult1_Address1", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                            return "You need to re-enter the address details! Please provide your Street Address.";
                        }
                        else
                        {
                            return "Please enter a valid response (y/n).";
                        }
                    }
                    else
                    {
                        return "Please enter a valid response (y/n).";
                    }
                case "ASK_Confirm_And_Finalize_Quote":
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (message.Trim().ToLower() == "y")
                        {
                            //** Marking "step2" as completed
                            await MarkSectionAsComplete("step2");

                            //Transition to the next state
                            userStates[connectionId] = ("ASK_OrderSummary", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);

                            //Show OrderSummary
                            return GenerateOrderSummaryHtml(quoteDetailsResponseDto);
                        }
                        else if (message.Trim().ToLower() == "n")
                        {
                            //Transition to the next state
                            userStates[connectionId] = ("ASK_Adult1_Address1", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);
                            return "You need to re-enter the address details! Please provide your Street Address.";
                        }
                        else
                        {
                            return "Please enter a valid response (y/n).";
                        }
                    }
                    else
                    {
                        return "Please enter a valid response (y/n).";
                    }


                //*********************
                //STEP3: Order Summary
                //*********************
                //Confirm And Finalize Quote
                case "ASK_OrderSummary":
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (message.Trim().ToLower() == "y")
                        {
                            //** Marking "step1" as completed
                            await MarkSectionAsComplete("step3");

                            //Transition to the next state
                            userStates[connectionId] = ("ASK_Payment", quoteRequestDto, quotePricingRequestDto, quoteResponseDto, tierList, selectedQuoteType, quoteDetailsRequestDto, quoteDetailsResponseDto);

                            //Show OrderSummary
                            return "Payment Gateway functionality is not available at the moment. Please proceed with other actions or contact our support team";
                        }
                        else if (message.Trim().ToLower() == "n")
                        {
                            //Transition to the next state
                            userStates.TryRemove(connectionId, out _);
                            return "Thank you for showing your interest in this quote. Our executive will soon get in touch with you. Thank you.";
                        }
                        else
                        {
                            return "Please enter a valid response (y/n).";
                        }
                    }
                    else
                    {
                        return "Please enter a valid response (y/n).";
                    }

                   
                //****************
                //STEP3: Payment
                //***************
                case "ASK_Payment":
                    userStates.TryRemove(connectionId, out _);
                    return "Payment Gateway functionality is not available at the moment. Please proceed with other actions or contact our support team.";

      
                //--------
                //Default
                //--------
                default:
                    // Handle unexpected states by restarting the conversation
                    userStates.TryRemove(connectionId, out _);
                    return "Oops! Something went wrong. Let's start over.";
            }

        }



        //------------
        //Show Quotes
        //------------
        private string ShowQuotes(List<QuoteTierDto> tierList)
        {
            var html = new StringBuilder("Thank you! Here are the available cover options:<br><br>");
            html.Append("<div style='display: flex; justify-content: space-evenly; flex-wrap: wrap; max-width: 600px; margin: 0 auto;'>");

            foreach (var tier in tierList)
            {
                string tierType = tier.Type switch
                {
                    "Tier1" => "Essentials Cover",
                    "Tier2" => "Basic Cover",
                    "Tier3" => "Top Cover",
                    _ => "Unknown"
                };

                html.Append($@"
                    <label style='flex: 1 0 30%; margin: 10px; cursor: pointer;'>
                    <input type='radio' name='tierSelection' value='{tierType}' style='display: none;'/>
                    <div style='border: 2px solid #007bff; border-radius: 8px; padding: 10px; background-color: #f9f9f9; text-align: center; box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);'>
                        <h3 style='color: #007bff; font-size: 1.1em; margin: 10px 0;'>{tierType}</h3>
                        <p style='color: grey;'>Standard Price:</p>
                        <p><strong style='font-weight: bold; font-size: 1.2em;'>{tier.DefaultExcess}</strong></p>
                    </div>
                    </label>
                ");
            }
            html.Append("</div><br>");
            html.Append("Please type your choice.");

            return html.ToString();
        }

        //------------------
        //Show OrderSummary
        //------------------
        //Traveller Details
        public string GeneratePersonalDetailsHtml(QuoteResponseDto quoteDetails)
        {
            var primaryTraveler = quoteDetails.Travelers?.FirstOrDefault();

            if (primaryTraveler == null)
                return "<p>No traveler details available.</p>";

            return $@"
        <div style='border: 2px solid #007bff; border-radius: 8px; padding: 15px; margin-bottom: 20px; background-color: #f9f9f9;'>
            <h3 style='color: #007bff; margin-bottom: 10px;'>Primary Traveler Details</h3>
            <p><strong>Name:</strong> {primaryTraveler.FirstName} {primaryTraveler.LastName}</p>
            <p><strong>Email:</strong> {primaryTraveler.Email}</p>
            <p><strong>Mobile:</strong> {primaryTraveler.Phone}</p>
            <p><strong>Address:</strong> {primaryTraveler.Address.Address}, {primaryTraveler.Address.City}, {primaryTraveler.Address.PostalCode}</p>
        </div>";
        }

        //Policy Details
        public string GeneratePolicyDetailsHtml(QuoteResponseDto quoteDetails)
        {
            var destinations = quoteDetails.Destinations != null
                ? string.Join(", ", quoteDetails.Destinations.Select(d => d.CountryName))
                : "N/A";

            var tripType = quoteDetails.IsSingleTrip ? "Single Trip" : "Multi Trip";

            // If second value is 0, take only the first value
            string[] resultAges = quoteDetails.Ages.Length > 1 && quoteDetails.Ages[1] == "0" ? new[] { quoteDetails.Ages[0] } : quoteDetails.Ages;            

            return $@"
                <div style='border: 2px solid #007bff; border-radius: 8px; padding: 15px; margin-bottom: 20px; background-color: #f9f9f9;'>
                    <h3 style='color: #007bff; margin-bottom: 10px;'>Policy Details</h3>
                    <p><strong>Quote Number:</strong> {quoteDetails.QuoteNumber}</p>
                    <p><strong>Destination(s):</strong> {destinations}</p>
                    <p><strong>Trip Type:</strong> {tripType}</p>
                    <p><strong>Travel Dates:</strong> {quoteDetails.FromDate} to {quoteDetails.ToDate}</p>
                    <p><strong>Special Activities:</strong> 
                        {(quoteDetails.IsCruise ? "Cruise" : "N/A")}, 
                        {(quoteDetails.IsSking ? "Ski/Winter Sports" : "N/A")}
                    </p>
                    <p><strong>Ages of Travelers:</strong> {string.Join(", ", quoteDetails.Ages)}</p>
                </div>
                <div style='border: 2px solid #007bff; border-radius: 8px; padding: 15px; background-color: #f9f9f9;'>
                    <h3 style='color: #007bff; margin-bottom: 10px;'>Policy Summary</h3>
                    <p><strong>Policy Name:</strong> {quoteDetails.QuoteName}</p>
                    <p><strong>Top Cover Policy:</strong> {quoteDetails.ProductAlias}</p>
                    <p><strong>Policy Excess:</strong> {quoteDetails.Excess}</p>
                    <p><strong>Policy Status:</strong> {quoteDetails.PolicyStatus}</p>
                </div>";
        }

        //Order Sumamry
        public string GenerateOrderSummaryHtml(QuoteResponseDto quoteDetails)
        {
            var personalDetailsHtml = GeneratePersonalDetailsHtml(quoteDetails);
            var policyDetailsHtml = GeneratePolicyDetailsHtml(quoteDetails);

            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #007bff; text-align: center; margin-bottom: 20px;'>Order Summary</h2>
                {personalDetailsHtml}
                {policyDetailsHtml}
                <br/>
                <p>Would you like to confirm and proceed to payment? (y/n)</p>
            </div>";
        }

    }
}