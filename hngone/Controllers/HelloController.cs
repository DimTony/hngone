using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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

            
            var client = _httpClientFactory.CreateClient();
            var ipResponse = await client.GetStringAsync("https://api.ipify.org?format=json");
            var ipAddress = System.Text.Json.JsonDocument.Parse(ipResponse).RootElement.GetProperty("ip").GetString();

            
            var apiKey = _configuration["API_KEY"];
            var weatherResponse = await client.GetStringAsync($"https://api.weatherapi.com/v1/current.json?q={ipAddress}&key={apiKey}");
            var weatherJson = System.Text.Json.JsonDocument.Parse(weatherResponse);
            var location = weatherJson.RootElement.GetProperty("location").GetProperty("region").GetString();
            var temperature = weatherJson.RootElement.GetProperty("current").GetProperty("temp_c").GetDecimal();

            return Ok(new
            {
                client_ip = ipAddress,
                location = location,
                greeting = $"Hello, {visitorName}!, the temperature is {temperature} degrees Celsius in {location}"
            });
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            return StatusCode(500, "Error fetching IP or weather info");
        }
    }
}
