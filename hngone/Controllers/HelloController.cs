using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;

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
            // Decode and sanitize the visitor name
            var visitorName = Uri.UnescapeDataString(visitor_name).Replace("\"", "");

            // Fetch the client's IP address from the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(ipAddress))
            {
                return StatusCode(500, "Could not determine client's IP address");
            }

            // Fetch the weather info
            var apiKey = _configuration["API_KEY"];
            var client = _httpClientFactory.CreateClient();
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
            return StatusCode(500, "Error fetching weather info");
        }
    }
}
