using System;

namespace DomainCopilot.Api.Core.Common;

public static class CosineSimilarity
{
    public static float Calculate(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Vectors must have the same length.");
        }

        if (vectorA.Length == 0)
        {
            return 0f;
        }

        float dotProduct = 0f;
        float magnitudeA = 0f;
        float magnitudeB = 0f;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        if (magnitudeA == 0f || magnitudeB == 0f)
        {
            return 0f;
        }

        return (float)(dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB)));
    }
}
