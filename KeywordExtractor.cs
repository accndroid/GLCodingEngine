namespace FinOps.GLCodingEngine.Services;

// AGENTIC: NLP-lite tokenizer — extracts matchable keywords from invoice descriptions.
// Production upgrade path: replace with LLM-based entity extraction.

public static class KeywordExtractor
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "for","the","and","of","to","in","on","at","by","with","from","as","is","was","are",
        "an","a","or","be","been","being","have","has","had","do","does","did","will","would",
        "could","should","may","might","shall","can","this","that","these","those","it","its",
        "our","their","your","my","pvt","ltd","llc","inc","corp","mr","ms","mrs",
        "jan","feb","mar","apr","jun","jul","aug","sep","oct","nov","dec",
        "q1","q2","q3","q4","fy","2023","2024","2025","2026"
    };

    // AGENTIC: Longer words first — they're more specific and yield better matches
    public static List<string> Extract(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return [];
        return description
            .Split([' ',',','.','-','/','(',')',':',';','"','\''],
                   StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 2 && !StopWords.Contains(w) && !decimal.TryParse(w, out _))
            .Select(w => w.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(w => w.Length).ToList();
    }

    // AGENTIC: Bidirectional partial match — "advisory" matches rule keyword "Advisory"
    public static string? FindMatch(List<string> extracted, string targetKeyword)
    {
        return extracted.FirstOrDefault(e =>
            e.Contains(targetKeyword, StringComparison.OrdinalIgnoreCase) ||
            targetKeyword.Contains(e, StringComparison.OrdinalIgnoreCase));
    }
}
