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


    [HttpPost("send")]
    public async Task<IActionResult> SendEmail(
        [FromQuery] string email)
    {
        await emailService.SendEmailAsync(
            email,
            "Test Email - Queueless",
            """
            <h1>Hello from Queueless! 👋</h1>

            <p>This is a test email sent from your ASP.NET Core Web API.</p>

            <p>If you're reading this, your email service is working.</p>
            """);

        return Ok(new
        {
            message = "Email sent successfully"
        });
    }
}