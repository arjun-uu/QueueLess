using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController(ILogger<TestController> logger, IEmailService emailService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetOutput()
    {
        string output = "Hello From Controller";

        logger.LogInformation(
            "Output generated: {Output}",
            output);


        return Ok(output);
    }
}
