using System.Text.Json;
using System.Text.Json.Nodes;

namespace GameGameGame.Content.Tools;

public static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = ContentToolJson.Options;

    public static void Main()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());
        string? line;
        while ((line = Console.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var response = HandleJsonRpcLine(dispatcher, line);
            if (response is not null)
            {
                Console.WriteLine(response.ToJsonString(JsonOptions));
                Console.Out.Flush();
            }
        }
    }

    private static JsonObject? HandleJsonRpcLine(ContentToolDispatcher dispatcher, string line)
    {
        try
        {
            var request = JsonNode.Parse(line)!.AsObject();
            var id = request["id"]?.DeepClone();
            var method = request["method"]?.GetValue<string>();

            return method switch
            {
                "initialize" => Response(id, new JsonObject
                {
                    ["protocolVersion"] = "2024-11-05",
                    ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
                    ["serverInfo"] = new JsonObject { ["name"] = "GameGameGame.Content.Tools", ["version"] = "0.1.0" }
                }),
                "notifications/initialized" => null,
                "tools/list" => Response(id, new JsonObject { ["tools"] = new JsonArray(ToolDefinitions().Select(definition => definition.DeepClone()).ToArray()) }),
                "tools/call" => CallTool(dispatcher, id, request["params"]?.AsObject()),
                _ => Error(id, -32601, $"Unsupported JSON-RPC method '{method}'.")
            };
        }
        catch (Exception ex)
        {
            return Error(null, -32603, ex.Message);
        }
    }

    private static JsonObject CallTool(ContentToolDispatcher dispatcher, JsonNode? id, JsonObject? parameters)
    {
        var name = parameters?["name"]?.GetValue<string>() ?? string.Empty;
        var argumentsNode = parameters?["arguments"] ?? new JsonObject();
        var arguments = JsonSerializer.Deserialize<JsonElement>(argumentsNode.ToJsonString());
        var result = dispatcher.Invoke(name, arguments);
        var resultText = JsonSerializer.Serialize(result, JsonOptions);
        return Response(id, new JsonObject
        {
            ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = resultText }),
            ["isError"] = !result.Ok
        });
    }

    private static JsonObject Response(JsonNode? id, JsonNode result) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["result"] = result
    };

    private static JsonObject Error(JsonNode? id, int code, string message) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject { ["code"] = code, ["message"] = message }
    };

    private static IReadOnlyList<JsonObject> ToolDefinitions() =>
        ContentToolCatalog.Names.Select(name => new JsonObject
        {
            ["name"] = name,
            ["description"] = ContentToolCatalog.Describe(name),
            ["inputSchema"] = ContentToolCatalog.InputSchema(name)
        }).ToList();
}
