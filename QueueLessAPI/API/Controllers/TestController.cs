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

    [HttpPost("send-email")]
    public async Task<IActionResult> SendTestEmail([FromQuery] string email)
    {
        try
        {
            await emailService.SendEmailAsync(
                email,
                "QueueLess Test Email",
                "<h2>Hello from QueueLess!</h2><p>This is a test email.</p>");

            logger.LogInformation(
                "Test email sent to {Email}",
                email);

            return Ok("Test email sent successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send test email to {Email}",
                email);

            return StatusCode(500, "Failed to send test email.");
        }
    }

}