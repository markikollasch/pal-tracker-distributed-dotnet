using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DefaultRoute
{
    [Route("")]
    [Authorize(Policy="pal-dotnet")]
    public class DefaultController : Controller
    {
        [HttpGet]
        public string Default() => "Noop!";
    }
}