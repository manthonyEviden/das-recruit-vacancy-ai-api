using Microsoft.AspNetCore.Mvc;

using TestWebAPI.LLMWrapper;
using TestWebAPI.LLMExecutable;
namespace TestWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LLMController : ControllerBase
    {
        [HttpGet(Name = "RunLLM")]
        public IActionResult RunLLM()
        {
            LLMExec llmcode = new(); // call class constructor to llmexec
            string llmoutput=llmcode.RunLLM("Apprenticeship vacacny in Nursing requiring staff aged 18+");
            return Ok(llmoutput);
        }

    }
}