using Microsoft.AspNetCore.Mvc;
using NexusBank.Api.Interface;
using NexusBank.Api.Models;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerProfileService _profileService;

    public CustomersController(ICustomerProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerProfile profile)
    {
        await _profileService.CreateProfileAsync(profile);
        return Ok(new { Message = "Customer profile saved to NoSQL!", CustomerId = profile.CustomerId });
    }

    [HttpGet("{customerId}")]
    public async Task<IActionResult> Get(string customerId)
    {
        var profile = await _profileService.GetProfileAsync(customerId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }
}