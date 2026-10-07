using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using UglyToad.PdfPig;

namespace DomainCopilot.Api.Features.Corpus.Services;

public class DocumentParserService : IDocumentParserService
{
    private const int ChunkSizeWords = 350;
    private const int ChunkOverlapWords = 50;

    public async Task<List<DocumentChunk>> ParseAsync(Stream fileStream, string fileName, string documentType, string? docId, CancellationToken cancellationToken = default)
    {
        var chunks = new List<DocumentChunk>();
        var finalDocId = string.IsNullOrWhiteSpace(docId) ? fileName : docId;

        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            chunks = ParsePdf(fileStream, finalDocId, documentType);
        }
        else if (fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            chunks = await ParseTextAsync(fileStream, finalDocId, documentType, cancellationToken);
        }
        else
        {
            throw new NotSupportedException($"File extension for '{fileName}' is not supported.");
        }

        return chunks;
    }

    private List<DocumentChunk> ParsePdf(Stream fileStream, string docId, string category)
    {
        var chunks = new List<DocumentChunk>();
        using var pdfDocument = PdfDocument.Open(fileStream);
        var chunkIndex = 0;

        foreach (var page in pdfDocument.GetPages())
        {
            var text = page.Text;
            var pageChunks = ChunkText(text, $"Page {page.Number}", docId, category, page.Number, ref chunkIndex);
            chunks.AddRange(pageChunks);
        }

        return chunks;
    }

    private async Task<List<DocumentChunk>> ParseTextAsync(Stream fileStream, string docId, string category, CancellationToken cancellationToken)
    {
        var chunks = new List<DocumentChunk>();
        var chunkIndex = 0;
        using var reader = new StreamReader(fileStream);

        var currentTitle = "General";
        var currentContent = new System.Text.StringBuilder();

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (line.StartsWith("# ") || line.StartsWith("## ") || line.StartsWith("### "))
            {
                if (currentContent.Length > 0)
                {
                    chunks.AddRange(ChunkText(currentContent.ToString(), currentTitle, docId, category, null, ref chunkIndex));
                    currentContent.Clear();
                }
                currentTitle = line.TrimStart('#', ' ').Trim();
            }
            else
            {
                currentContent.AppendLine(line);
            }
        }

        if (currentContent.Length > 0)
        {
            chunks.AddRange(ChunkText(currentContent.ToString(), currentTitle, docId, category, null, ref chunkIndex));
        }

        return chunks;
    }

    private List<DocumentChunk> ChunkText(string text, string sectionTitle, string docId, string category, int? pageNumber, ref int chunkIndex)
    {
        var chunks = new List<DocumentChunk>();
        var words = text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 0) return chunks;

        for (int i = 0; i < words.Length; i += (ChunkSizeWords - ChunkOverlapWords))
        {
            var chunkWords = words.Skip(i).Take(ChunkSizeWords).ToArray();
            var chunkContent = string.Join(" ", chunkWords);
            
            chunkContent = Sanitize(chunkContent);

            if (string.IsNullOrWhiteSpace(chunkContent)) continue;

            chunks.Add(new DocumentChunk
            {
                DocId = docId,
                Category = category,
                SectionTitle = Sanitize(sectionTitle),
                Content = chunkContent,
                PageNumber = pageNumber,
                ChunkIndex = chunkIndex++,
                WordCount = chunkWords.Length
            });
            
            if (i + ChunkSizeWords >= words.Length)
            {
                break;
            }
        }

        return chunks;
    }

    private string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input.Replace("<|im_start|>", "").Replace("<|im_end|>", "");
    }
}
