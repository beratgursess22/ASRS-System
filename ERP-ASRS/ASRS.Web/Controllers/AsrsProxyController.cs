using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[ApiController]
[Route("api/asrs")]
public class AsrsProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AsrsProxyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("rack-state")]
    public Task<IActionResult> RackState()
        => RelayGetAsync("api/asrs/rack-state");

    [HttpGet("system-status")]
    public Task<IActionResult> SystemStatus()
        => RelayGetAsync("api/asrs/system-status");

    [HttpPost("retrieve")]
    public Task<IActionResult> Retrieve([FromBody] RetrieveRequest req)
        => RelayPostAsync("api/asrs/retrieve", req);

    [HttpPost("rfid-scan")]
    public Task<IActionResult> RfidScan([FromBody] RfidScanRequest req)
        => RelayPostAsync("api/asrs/rfid-scan", req);

    private async Task<IActionResult> RelayGetAsync(string path)
    {
        var client = _httpClientFactory.CreateClient("AsrsApi");
        try
        {
            var res = await client.GetAsync(path);
            return await ToActionResultAsync(res);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, $"ASRS_API_UNREACHABLE: {ex.Message}");
        }
    }

    private async Task<IActionResult> RelayPostAsync<TBody>(string path, TBody body)
    {
        var client = _httpClientFactory.CreateClient("AsrsApi");
        try
        {
            var res = await client.PostAsJsonAsync(path, body);
            return await ToActionResultAsync(res);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, $"ASRS_API_UNREACHABLE: {ex.Message}");
        }
    }

    private static async Task<IActionResult> ToActionResultAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = contentType
        };
    }
}

public record RetrieveRequest(int Row, int Col);
public record RfidScanRequest(string CardUid);
