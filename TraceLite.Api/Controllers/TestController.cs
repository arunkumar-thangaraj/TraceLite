using Microsoft.AspNetCore.Mvc;

namespace TraceLite.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TestController(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("external")]
    public async Task<IActionResult> ExternalCall()
    {
        var client = _httpClientFactory
            .CreateClient("TraceLiteClient");

        var response = await client.GetAsync(
            "https://jsonplaceholder.typicode.com/posts/1");

        var content = await response.Content.ReadAsStringAsync();

        return Ok(new
        {
            StatusCode = (int)response.StatusCode,
            Content = content
        });
    }
}