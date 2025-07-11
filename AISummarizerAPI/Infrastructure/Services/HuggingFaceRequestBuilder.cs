using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AISummarizerAPI.Infrastructure.Interfaces;
using AISummarizerAPI.Models.HuggingFace;

namespace AISummarizerAPI.Infrastructure.Services;

public class HuggingFaceRequestBuilder : IHuggingFaceRequestBuilder
{
    private readonly JsonSerializerOptions _jsonOptions;

    public HuggingFaceRequestBuilder()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public HuggingFaceSummarizationRequest CreateSummarizationRequest(string text)
    {
        var dynamicMaxLength = Math.Max(50, Math.Min(200, text.Length / 4));
        var dynamicMinLength = Math.Max(30, dynamicMaxLength / 3);

        return new HuggingFaceSummarizationRequest
        {
            Inputs = text,
            Parameters = new SummarizationParameters
            {
                MaxLength = dynamicMaxLength,
                MinLength = dynamicMinLength,
                DoSample = false,
                Temperature = 0.3
            },
            Options = new ApiOptions
            {
                WaitForModel = true,
                UseCache = false
            }
        };
    }

    public StringContent CreateHttpContent(HuggingFaceSummarizationRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}