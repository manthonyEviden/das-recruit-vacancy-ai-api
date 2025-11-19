using Microsoft.AspNetCore.Mvc;


using TestWebAPI.HelperObjects;
using TestWebAPI.LLMExecutable;
namespace TestWebAPI.Controllers
{    
    [ApiController]
    [Route("api/[controller]")]
    public class LLMController : ControllerBase
    {
        [HttpPost(Name = "RunLLM")]
        public IActionResult RunLLM([FromBody] InputObject inputvacancy)
        {
            
            Console.WriteLine(inputvacancy);
            Console.WriteLine("Title: " + inputvacancy.Title);
            LLMExec llmcode = new(); // call class constructor to llmexec
            string llmoutput=llmcode.ExecLLM(inputvacancy);
            return Ok(llmoutput);
        }

    }
}