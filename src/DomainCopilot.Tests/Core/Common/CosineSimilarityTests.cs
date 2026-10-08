using System;
using DomainCopilot.Api.Core.Common;
using FluentAssertions;
using Xunit;

namespace DomainCopilot.Tests.Core.Common;

public class CosineSimilarityTests
{
    [Fact]
    public void Calculate_IdenticalVectors_ShouldReturnOne()
    {
        float[] v1 = { 1f, 2f, 3f };
        float[] v2 = { 1f, 2f, 3f };

        var result = CosineSimilarity.Calculate(v1, v2);

        result.Should().BeApproximately(1.0f, 0.0001f);
    }

    [Fact]
    public void Calculate_OrthogonalVectors_ShouldReturnZero()
    {
        float[] v1 = { 1f, 0f };
        float[] v2 = { 0f, 1f };

        var result = CosineSimilarity.Calculate(v1, v2);

        result.Should().BeApproximately(0.0f, 0.0001f);
    }

    [Fact]
    public void Calculate_InverseVectors_ShouldReturnNegativeOne()
    {
        float[] v1 = { 1f, 2f, 3f };
        float[] v2 = { -1f, -2f, -3f };

        var result = CosineSimilarity.Calculate(v1, v2);

        result.Should().BeApproximately(-1.0f, 0.0001f);
    }

    [Fact]
    public void Calculate_DifferentLengths_ShouldThrowArgumentException()
    {
        float[] v1 = { 1f, 2f };
        float[] v2 = { 1f, 2f, 3f };

        Action act = () => CosineSimilarity.Calculate(v1, v2);

        act.Should().Throw<ArgumentException>().WithMessage("Vectors must have the same length.");
    }
}
