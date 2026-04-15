using FinOps.GLCodingEngine.Core.Enums;

namespace FinOps.GLCodingEngine.Services;

// AGENTIC: Self-assessment mechanism — scores how confident the AI Agent is.
// Score directly controls UI: HIGH=ready, MEDIUM=flagged, LOW=manual.

public static class ConfidenceScorer
{
    public static (int Score, ConfidenceLevel Level) Calculate(
        bool vendorExactMatch, bool vendorFuzzyMatch, bool keywordMatch,
        bool costCenterResolved, bool taxCodeResolved, bool locationResolved,
        bool companyCodeResolved, bool categoryResolved)
    {
        int score = 0;
        if (vendorExactMatch)       score += 30;  // AGENTIC: Vendor identity = strongest signal
        else if (vendorFuzzyMatch)  score += 15;
        if (keywordMatch)           score += 25;  // AGENTIC: Keyword = second strongest
        if (costCenterResolved)     score += 10;
        if (taxCodeResolved)        score += 10;
        if (locationResolved)       score += 10;
        if (companyCodeResolved)    score += 10;
        if (categoryResolved)       score += 5;

        var level = score switch
        {
            >= 90 => ConfidenceLevel.HIGH,
            >= 60 => ConfidenceLevel.MEDIUM,
            > 0   => ConfidenceLevel.LOW,
            _     => ConfidenceLevel.UNRESOLVED
        };
        return (score, level);
    }
}
