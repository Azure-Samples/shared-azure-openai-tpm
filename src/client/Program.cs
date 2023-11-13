#region Using Directives
using System.Text.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using OpenAiRestApi.Services;
using Grpc.Net.Client;
using Grpc.Core;
using Newtonsoft.Json.Linq;
using System.IO;
#endregion


namespace OpenAiRestApi.Client;
class Program
{
    #region Private Constants
    private const string LoggingSection= "Logging";
    private const string DevelopmentEnvironment = "Development";
    private const int DefaultDelay = 30;
    #endregion

    #region Private Static Fields
    private static Dictionary<string, Dictionary<string, string>> _environments = new Dictionary<string, Dictionary<string, string>>();
    private static string _environment = default!;
    private static int _delay = default;
    private static IConfigurationRoot _configuration = default!;
    private static IConfigurationSection _logging = default!;
    private static ILoggerFactory _loggerFactory = default!;
    private static string _restServiceUrl = default!;
    private static string _grpcServiceUrl = default!;
    private static List<Test> _testList = new List<Test>
        {
            new Test
            {
                Name = "REST Echo",
                Description = "Calls the Echo method.",
                ActionAsync = RestEcho
            },
            new Test
            {
                Name = "REST Chat",
                Description = "Calls the GetChatCompletionsAsync method.",
                ActionAsync = RestChat
            },
            new Test
            {
                Name = "REST Stream",
                Description = "Calls the GetChatCompletionsStreamingAsync method.",
                ActionAsync = RestStream
            },
            new Test
            {
                Name = "gRPC Echo",
                Description = "Calls the Echo method.",
                ActionAsync = GrpcEcho
            },
            new Test
            {
                Name = "gRPC Chat",
                Description = "Calls the GetChatCompletionsAsync method.",
                ActionAsync = GrpcChat
            },
            new Test
            {
                Name = "gRPC Stream",
                Description = "Calls the GetChatCompletionsStreamingAsync method.",
                ActionAsync = GrpcStream
            }
        };
    private static string Line = new string('-', 120);
    #endregion

    #region Main Method
    public static async Task Main()
    {
        try
        {
            // Initialization
            CreateConfiguration();
            CreateLoggerFactory();
            SelectEnvironment();

            int i;

            // Execute commands
            while ((i = SelectTest()) != _testList.Count + 1)
            {
                try
                {
                    WriteLine(Line);
                    await _testList[i - 1]!.ActionAsync();
                    WriteLine(Line);
                }
                catch (Exception ex)
                {
                    PrintException(ex);
                }
            }
        }
        catch (Exception ex)
        {
            PrintException(ex);
            WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
    #endregion

    #region Rest Test Methods
    private static async Task RestEcho()
    {
        // Enter a tenant
        Console.Write("Enter a tenant: ");
        var tenant = Console.ReadLine();

        // Validate the tenant
        if (string.IsNullOrWhiteSpace(tenant))
        {
            Console.WriteLine("The tenant cannot be empty.");
            return;
        }

        // Enter an integer value
        Console.Write("Enter an integer value: ");
        var valueAsString = Console.ReadLine();

        // Validate the question
        int value;
        if (!int.TryParse(valueAsString, out value))
        {
            Console.WriteLine("The value cannot be empty.");
            return;
        }

        // Create a client
        using HttpClient httpClient = new HttpClient();
        var uri = new Uri($"{_restServiceUrl}/openai/echo?tenant={tenant}&value={value}");
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Call the Echo method
        var response = await httpClient.SendAsync(requestMessage);

        if (response.IsSuccessStatusCode)
        {
            // Process the response
            Console.Write("Result: ");
            var stream = await response.Content.ReadAsStreamAsync();
            using StreamReader reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                Console.WriteLine(line);
            }
        }
        else
        {
            // Handle the error scenario
            PrintMessage("Error", $"Request failed with status code {response.StatusCode}");
        }
    }

    private static async Task RestChat()
    {
        // Enter a tenant
        Console.Write("Enter a tenant: ");
        var tenant = Console.ReadLine();

        // Validate the tenant
        if (string.IsNullOrWhiteSpace(tenant))
        {
            Console.WriteLine("The tenant cannot be empty.");
            return;
        }

        // Enter a question
        Console.Write("Enter a question: ");
        var question = Console.ReadLine();

        // Validate the question
        if (string.IsNullOrWhiteSpace(question))
        {
            Console.WriteLine("The question cannot be empty.");
            return;
        }

        // Create a message
        var message = new Message
        {
            Role = "user",
            Content = question
        };

        // Serialize an array of messages
        var json = JsonSerializer.Serialize(new Message[] { message });

        // Create a client
        using HttpClient httpClient = new HttpClient();

        // Create content
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Add the tenant header
        content.Headers.Add("x-tenant", tenant);

        // Call the Stream method
        var response = await httpClient.PostAsync($"{_restServiceUrl}/openai/chat", content);

        if (response.IsSuccessStatusCode)
        {
            // Process the response
            var stream = await response.Content.ReadAsStreamAsync();
            using StreamReader reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                Console.WriteLine(line);
            }
        }
        else
        {
            // Handle the error scenario
            PrintMessage("Error", $"Request failed with status code {response.StatusCode}");
        }
    }

    static private async Task RestStream()
    {
        // Enter a tenant
        Console.Write("Enter a tenant: ");
        var tenant = Console.ReadLine();

        // Validate the tenant
        if (string.IsNullOrWhiteSpace(tenant))
        {
            Console.WriteLine("The tenant cannot be empty.");
            return;
        }

        // Enter a question
        Console.Write("Enter a question: ");
        var question = Console.ReadLine();

        // Validate the question
        if (string.IsNullOrWhiteSpace(question))
        {
            Console.WriteLine("The question cannot be empty.");
            return;
        }
        
        // Create a message
        var message = new Message
        {
            Role = "user",
            Content = question
        };

        // Serialize an array of messages
        var json = JsonSerializer.Serialize(new Message[] { message });

        // Create content
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Add the tenant header
        content.Headers.Add("x-tenant", tenant);

        // Create a client
        using HttpClient httpClient = new();
        
        // Set the request headers
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        // Create request message
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_restServiceUrl}/openai/stream")
        {
            Content = content
        };

        // use in-memory data
        using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        await foreach (var token in JsonSerializer.DeserializeAsyncEnumerable<string>(responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
        {
            if (string.IsNullOrEmpty(token))
            {
                continue;
            }
            Console.Write(token);

            // Simulate a delay
            if (string.Compare(_environment, DevelopmentEnvironment, StringComparison.OrdinalIgnoreCase) == 0)
            {
                await Task.Delay(_delay);
            }
        }
        Console.WriteLine();
    }
    #endregion

    #region Grpc Test Methods
    private static async Task GrpcEcho()
    {
        // Enter a tenant
        Console.Write("Enter a tenant: ");
        var tenant = Console.ReadLine();

        // Validate the tenant
        if (string.IsNullOrWhiteSpace(tenant))
        {
            Console.WriteLine("The tenant cannot be empty.");
            return;
        }

        // Enter an integer value
        Console.Write("Enter an integer value: ");
        var valueAsString = Console.ReadLine();

        // Validate the question
        int value;
        if (!int.TryParse(valueAsString, out value))
        {
            Console.WriteLine("The value cannot be empty.");
            return;
        }

        // Check if the gRPC service is running on HTTP
        var uri = new Uri(_grpcServiceUrl);
        if (uri.Scheme == "http")
        {
            // This switch must be set before creating the GrpcChannel/HttpClient.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
        
        // Create a channel
        var channel = GrpcChannel.ForAddress(_grpcServiceUrl); 

        // Create a client
        var client = new OpenAIServiceGrpc.OpenAIServiceGrpcClient(channel);

        // Create a request
        var request = new EchoRequest
        {
            Tenant = tenant,
            Value = value
        };
        
        // Call the Echo method
        var response = await client.EchoAsync(request);

        // Print the response
        Console.WriteLine($"Result: {response.Message}");

        // Shutdown the gRPC channel
        channel.ShutdownAsync().Wait();
    }

    private static async Task GrpcChat()
    {
        // Enter a tenant
        Console.Write("Enter a tenant: ");
        var tenant = Console.ReadLine();

        // Validate the tenant
        if (string.IsNullOrWhiteSpace(tenant))
        {
            Console.WriteLine("The tenant cannot be empty.");
            return;
        }

        // Enter a question
        Console.Write("Enter a question: ");
        var question = Console.ReadLine();

        // Validate the question
        if (string.IsNullOrWhiteSpace(question))
        {
            Console.WriteLine("The question cannot be empty.");
            return;
        }

        // Create a request
        var request = new GetChatCompletionsRequest()
        {   
            Tenant = tenant
        };

        // Create a message
        request.Conversation.Add(new GrpcMessage
        {
            Role = "user",
            Content = question
        });

        // Check if the gRPC service is running on HTTP
        var uri = new Uri(_grpcServiceUrl);
        if (uri.Scheme == "http")
        {
            // This switch must be set before creating the GrpcChannel/HttpClient.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        // Create a channel
        var channel = GrpcChannel.ForAddress(_grpcServiceUrl);

        // Create a client
        var client = new OpenAIServiceGrpc.OpenAIServiceGrpcClient(channel);

        // Call the GetChatCompletions method
        var response = await client.GetChatCompletionsAsync(request);

        // Print the response
        if (response != null)
        {
            Console.WriteLine(response.Result);
        }

        // Shutdown the gRPC channel
        channel.ShutdownAsync().Wait();
    }

    static private async Task GrpcStream()
    {
        // Enter a tenant
        Console.Write("Enter a tenant: ");
        var tenant = Console.ReadLine();

        // Validate the tenant
        if (string.IsNullOrWhiteSpace(tenant))
        {
            Console.WriteLine("The tenant cannot be empty.");
            return;
        }

        // Enter a question
        Console.Write("Enter a question: ");
        var question = Console.ReadLine();

        // Validate the question
        if (string.IsNullOrWhiteSpace(question))
        {
            Console.WriteLine("The question cannot be empty.");
            return;
        }

        // Create a request
        var request = new GetChatCompletionsRequest()
        {
            Tenant = tenant
        };

        // Create a message
        request.Conversation.Add(new GrpcMessage
        {
            Role = "user",
            Content = question
        });

        // Check if the gRPC service is running on HTTP
        var uri = new Uri(_grpcServiceUrl);
        if (uri.Scheme == "http")
        {
            // This switch must be set before creating the GrpcChannel/HttpClient.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        // Create a channel
        var channel = GrpcChannel.ForAddress(_grpcServiceUrl);

        // Create a client
        var client = new OpenAIServiceGrpc.OpenAIServiceGrpcClient(channel);

        // Make the gRPC streaming call
        using (var streamingCall = client.GetChatCompletionsStreaming(request))
        {
            // Read the streaming response
            while (await streamingCall.ResponseStream.MoveNext())
            {
                var response = streamingCall.ResponseStream.Current;

                // Process the response message
                Console.Write(response.Message);

                // Simulate a delay
                if (string.Compare(_environment, DevelopmentEnvironment, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    await Task.Delay(_delay);
                }
            }
            Console.WriteLine();
        }

        // Shutdown the gRPC channel
        channel.ShutdownAsync().Wait();

    }
    #endregion

    #region Private Static Methods 
    private static void CreateConfiguration()
    {
        // Load configuration
        _configuration = new ConfigurationBuilder()
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(path: "appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>()
            .Build();

        // Read the delay configuration
        var delayElement = _configuration.GetSection("Delay");
        if (delayElement != null && !int.TryParse(delayElement.Value, out _delay))
        {
            _delay = DefaultDelay;
        }

        // Read the enviroments configuration
        var environmentsSection = _configuration.GetSection("Environments");

        // Create a dictionary of environments
        foreach (var environmentSection in environmentsSection.GetChildren())
        {
            var environment = new Dictionary<string, string>();
            foreach (var property in environmentSection.GetChildren())
            {
                environment.Add(property.Key, property.Value!);
            }
            _environments.Add(environmentSection.Key, environment);
        }

        // Read the logging configuration
        _logging = _configuration.GetSection(LoggingSection);
    }

    private static void CreateLoggerFactory()
    {
        // Read debug configuration
        bool debug = false;
        bool.TryParse(_logging["Debug"], out debug);

        // Read console configuration
        bool console = false;
        bool.TryParse(_logging["Console"], out console);

        // Initialize logger
        _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConfiguration(_logging);
                if (debug)
                {
                    builder.AddDebug();
                }
                if (console)
                {
                    builder.AddConsole();
                }
            });
    }

    private static void SelectEnvironment()
    {
        // Set variable
        var optionCount = _environments.Count;

        // Create a line
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Select an environment:");
        Console.WriteLine(Line);

        for (var i = 0; i < _environments.Count; i++)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[{0}] ", (char)('a' + i));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(_environments.ElementAt(i).Key);
        }

        // Create a line
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(Line);

        // Output
        Console.Write($"Press a key between [");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("a");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"] and [");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write((char)('a' + optionCount));
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"]: ");

        // Select an option
        var key = 'z';
        while ((key < 'a') || (key > 'a' + optionCount - 1))
        {
            key = Console.ReadKey(true).KeyChar;
        }
        var index = key - 'a';

        // Select current environment
        _environment = _environments.ElementAt(index).Key;

        // Define URLs based the selected environment
        _restServiceUrl = _environments.ElementAt(index).Value["RestServiceUrl"];
        _grpcServiceUrl = _environments.ElementAt(index).Value["GrpcServiceUrl"];

        // Output
        Console.WriteLine(Line);
        Console.Write($"You selected the ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(_environment);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" environment.");
        Console.WriteLine(Line);
    }

    private static int SelectTest()
    {
        // Set variable
        var optionCount = _testList.Count;

        // Create a line
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Select a test:");
        Console.WriteLine(Line);

        for (var i = 0; i < _testList.Count; i++)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[{0}] ", (char)('a' + i));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(_testList[i].Name);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - " + _testList[i].Description);
        }

        // Add exit option
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[{0}] ", (char)('a' + optionCount));
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Exit");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" - Close the test application.");
        Console.WriteLine(Line);

        // Output
        Console.Write($"Press a key between [");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("a");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"] and [");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write((char)('a' + optionCount));
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"]: ");

        // Select an option
        var key = 'z';
        while ((key < 'a') || (key > 'a' + optionCount))
        {
            key = Console.ReadKey(true).KeyChar;
        }
        return key - 'a' + 1;
    }

    private static void PrintException(
            Exception ex,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
    {
        // Write Line
        Console.WriteLine(Line);

        InternalPrintException(ex, sourceFilePath, memberName, sourceLineNumber);

        // Write Line
        Console.WriteLine(Line);
    }

    private static void InternalPrintException(Exception ex,
                                               string sourceFilePath = "",
                                               string memberName = "",
                                               int sourceLineNumber = 0)
    {
        AggregateException? exception = ex as AggregateException;
        if (exception != null)
        {
            foreach (Exception e in exception.InnerExceptions)
            {
                if (sourceFilePath != null) InternalPrintException(e, sourceFilePath, memberName, sourceLineNumber);
            }
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{ex.GetType().Name}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(":");
        Console.ForegroundColor = ConsoleColor.Yellow;
        string? fileName = null;
        if (File.Exists(sourceFilePath))
        {
            FileInfo file = new FileInfo(sourceFilePath);
            fileName = file.Name;
        }
        Console.Write(string.IsNullOrWhiteSpace(fileName) ? "Unknown" : fileName);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(":");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(string.IsNullOrWhiteSpace(memberName) ? "Unknown" : memberName);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(":");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(sourceLineNumber.ToString(CultureInfo.InvariantCulture));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("]");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(": ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(!string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "An error occurred.");
    }

    private static void WriteLine(string text)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(text);
    }

    private static void PrintMessage(string key, string value)
    {
        PrintMessage(new Dictionary<string, string> { { key, value} });
    }

    private static void PrintMessage(Dictionary<string, string> messages)
    {
        if (messages == null || messages.Count == 0)
            return;

        foreach (var key in messages.Keys)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(key);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(": ");
            Console.WriteLine(messages[key]);
        }
    }
    #endregion
}