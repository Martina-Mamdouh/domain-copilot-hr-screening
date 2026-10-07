using System;
using System.Text.RegularExpressions;

namespace DomainCopilot.Api.Infrastructure.Security;

public class PromptInjectionGuard : IPromptInjectionGuard
{
    // A list of regex patterns representing common prompt injection attacks.
    private static readonly string[] MaliciousPatterns = 
    {
        @"(?i)ignore\s+(all\s+)?previous\s+instructions",
        @"(?i)system\s+override",
        @"(?i)forget\s+(all\s+)?previous\s+commands",
        @"(?i)<\|im_start\|>",
        @"(?i)<\|system\|>",
        @"(?i)you\s+are\s+now",
        @"(?i)act\s+as\s+a\s+(different\s+)?(AI|assistant|model)"
    };

    public bool IsPotentiallyMalicious(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        foreach (var pattern in MaliciousPatterns)
        {
            if (Regex.IsMatch(input, pattern))
            {
                return true;
            }
        }
        return false;
    }

    public string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var sanitized = input;
        foreach (var pattern in MaliciousPatterns)
        {
            // Replace malicious patterns with a generic placeholder or strip them out.
            sanitized = Regex.Replace(sanitized, pattern, "[REDACTED_INJECTION_ATTEMPT]");
        }
        
        return sanitized;
    }
}
