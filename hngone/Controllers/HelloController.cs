using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net;

[ApiController]
[Route("api/[controller]")]
public class HelloController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public HelloController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string visitor_name = "Guest")
    {
        try
        {
            var visitorName = Uri.UnescapeDataString(visitor_name).Replace("\"", "");

            string clientIp = await GetClientIpAddress();

            if (string.IsNullOrEmpty(clientIp))
            {
                return BadRequest("Unable to determine client IP address");
            }

            var client = _httpClientFactory.CreateClient();
            var apiKey = _configuration["API_KEY"];
            var weatherResponse = await client.GetStringAsync($"https://api.weatherapi.com/v1/current.json?q={clientIp}&key={apiKey}");
            var weatherJson = System.Text.Json.JsonDocument.Parse(weatherResponse);
            var location = weatherJson.RootElement.GetProperty("location").GetProperty("region").GetString();
            var temperature = weatherJson.RootElement.GetProperty("current").GetProperty("temp_c").GetDecimal();

            return Ok(new
            {
                client_ip = clientIp,
                location = location,
                greeting = $"Hello, {visitorName}! The temperature is {temperature} degrees Celsius in {location}"
            });
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            return StatusCode(500, "Error fetching weather info");
        }
    }

    private async Task<string> GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress;

        if (ipAddress != null)
        {
            // If we're dealing with a localhost address, get the public IP
            if (IPAddress.IsLoopback(ipAddress))
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("https://api.ipify.org");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }

            // Convert IPv6 address to IPv4 if it's an IPv4-mapped IPv6 address
            if (ipAddress.IsIPv4MappedToIPv6)
            {
                ipAddress = ipAddress.MapToIPv4();
            }

            return ipAddress.ToString();
        }

        return null;
    }
}