using NexusBank.Api.Models;

namespace NexusBank.Api.Interface
{
    public interface ICustomerProfileService
    {
        Task CreateProfileAsync(CustomerProfile profile);
        Task<CustomerProfile?> GetProfileAsync(string customerId);
    }
}
