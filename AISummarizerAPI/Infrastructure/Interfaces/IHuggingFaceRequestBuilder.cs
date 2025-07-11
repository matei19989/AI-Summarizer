using AISummarizerAPI.Models.HuggingFace;

namespace AISummarizerAPI.Infrastructure.Interfaces;

public interface IHuggingFaceRequestBuilder
{
    HuggingFaceSummarizationRequest CreateSummarizationRequest(string text);
    StringContent CreateHttpContent(HuggingFaceSummarizationRequest request);
}