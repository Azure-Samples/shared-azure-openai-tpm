using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Newtonsoft.Json.Linq;

namespace Plugins;

public class OrchestratorPlugin
{
    IKernel _kernel;

    public OrchestratorPlugin(IKernel kernel)
    {
        _kernel = kernel;
    }

    [SKFunction, Description("Routes the request to the appropriate function.")]
    public async Task<string> RouteRequest(SKContext context)
    {
        // Save the original user request
        string request = context.Variables["input"];

        // Add the list of available functions to the context
        context.Variables["options"] = "Sqrt, Add";

        // Retrieve the intent from the user request
        var GetIntent = _kernel.Skills.GetFunction("OrchestratorPlugin", "GetIntent");
        var CreateResponse = _kernel.Skills.GetFunction("OrchestratorPlugin", "CreateResponse");
        await GetIntent.InvokeAsync(context);
        string intent = context.Variables["input"].Trim();

        // Prepare the functions to be called in the pipeline
        var GetNumbers = _kernel.Skills.GetFunction("OrchestratorPlugin", "GetNumbers");
        var ExtractNumbersFromJson = _kernel.Skills.GetFunction("OrchestratorPlugin", "ExtractNumbersFromJson");
        ISKFunction MathFunction;

        // Prepare the math function based on the intent
        switch (intent)
        {
            case "Sqrt":
                MathFunction = _kernel.Skills.GetFunction("MathPlugin", "Sqrt");
                break;
            case "Add":
                MathFunction = _kernel.Skills.GetFunction("MathPlugin", "Add");
                break;
            default:
                return "I'm sorry, I don't understand.";
        }

        // Create a new context object with the original request
        var pipelineContext = new ContextVariables(request);
        pipelineContext["original_request"] = request;

        // Run the functions in a pipeline
        var output = await _kernel.RunAsync(
            pipelineContext,
            GetNumbers,
            ExtractNumbersFromJson,
            MathFunction,
            CreateResponse);

        return output.Variables["input"];
    }

    [SKFunction, Description("Extracts numbers from JSON")]
    public SKContext ExtractNumbersFromJson(SKContext context)
    {
        JObject numbers = JObject.Parse(context.Variables["input"]);

        // loop through numbers and add them to the context
        foreach (var number in numbers)
        {
            if (number.Key == "number1")
            {
                // add the first number to the input variable
                context.Variables["input"] = number.Value!.ToString();
                continue;
            }
            else
            {
                // add the rest of the numbers to the context
                context.Variables[number.Key] = number.Value!.ToString();
            }
        }
        return context;
    }
}