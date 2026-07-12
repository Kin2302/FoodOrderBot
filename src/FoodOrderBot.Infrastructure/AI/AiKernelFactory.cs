using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace FoodOrderBot.Infrastructure.AI;

/// <summary>
/// Singleton factory — tạo và cache Semantic Kernel per task/model.
/// Không tạo mới Kernel mỗi request (tốn tài nguyên).
/// </summary>
public class AiKernelFactory(IConfiguration config)
{
    private readonly ConcurrentDictionary<string, Kernel> _kernels = new();

    private readonly string _apiKey = config["Groq:ApiKey"]
        ?? throw new InvalidOperationException("Groq:ApiKey chưa cấu hình.");

    private readonly string _baseUrl = config["Groq:BaseUrl"]
        ?? "https://api.groq.com/openai/v1";

    /// <summary>
    /// Lấy Kernel cho task cụ thể. Tạo mới nếu chưa có, cache lại cho lần sau.
    /// </summary>
    /// <param name="taskName">Tên task: "IntentClassifier" | "OrderParser" | "ChatBot" | "Upsell" | "Sentiment"</param>
    public Kernel GetKernel(string taskName)
    {
        return _kernels.GetOrAdd(taskName, name =>
        {
            // Đọc model theo task, fallback về 8b nếu không cấu hình
            var modelId = config[$"Groq:Models:{name}"] ?? "llama-3.1-8b-instant";

            return Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: _apiKey,
                    endpoint: new Uri(_baseUrl))
                .Build();
        });
    }

    /// <summary>Lấy model ID đang dùng cho task (dùng cho logging)</summary>
    public string GetModelId(string taskName)
        => config[$"Groq:Models:{taskName}"] ?? "llama-3.1-8b-instant";
}
