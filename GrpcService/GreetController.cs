using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GrpcService1
{
    [Route("api/greet")]
    [ApiController]
    public class GreetController : ControllerBase
    {

        [HttpGet("ping")]
        public async Task<ActionResult<string>> Ping()
        {
            await Task.CompletedTask;
            return new ActionResult<string>("Pong");
        }
    }
}
