using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetOutput()
        {
            string output = "Hello From Controller";
            return Ok(output);
        }

    }
}
