
using Application.Common.Exceptions;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController(
    ILogger<TestController> logger,
    IEmailService emailService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetOutput()
    {
        const string output = "Hello From Controller";

        logger.LogInformation(
            "Output generated: {Output}",
            output);

        return Ok(output);
    }

    [HttpGet("not-found")]
    public IActionResult TestNotFound()
    {
        throw new NotFoundException(
            "The requested test resource was not found.");
    }

    [HttpPost("send-email")]
    public async Task<IActionResult> SendTestEmail(
        [FromQuery] string email)
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
}