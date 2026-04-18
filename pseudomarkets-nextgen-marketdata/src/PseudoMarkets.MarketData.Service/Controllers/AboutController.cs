using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.MarketData.Service.Contracts;
using System.Reflection;

namespace PseudoMarkets.MarketData.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AboutController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public AboutController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public ActionResult<ServiceInfoResponse> Get()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        return Ok(new ServiceInfoResponse
        {
            Name = "Pseudo Markets Next Gen Market Data Service",
            Environment = _environment.EnvironmentName,
            Version = version
        });
    }
}
