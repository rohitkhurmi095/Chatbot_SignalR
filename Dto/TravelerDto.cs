namespace TravelInsuranceAdvisor.Dto
{
    public class TravelerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string DateOfBirth { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public AddressDto Address { get; set; }
        public Guid? TravelerId { get; set; }
        public bool MedicalRequired { get; set; }
    }
}
