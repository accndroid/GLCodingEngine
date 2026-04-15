namespace FinOps.GLCodingEngine.Core.Enums;

public enum CodingMode
{
    PO,         // GL inherited from Purchase Order
    NON_PO,     // AI suggests + CF Verifier reviews
    BULK        // Bulk coding across multiple invoices
}

public enum CodingStatus
{
    PENDING,
    AI_SUGGESTED,
    MANUALLY_CODED,
    VALIDATED,
    POSTED,
    ERROR
}

// AGENTIC: Confidence levels drive UI behavior — HIGH = auto-approve ready,
//          MEDIUM = flagged for human review, LOW = manual entry required
public enum ConfidenceLevel
{
    UNRESOLVED = 0,     // 0%      — no match, manual entry required
    LOW        = 1,     // 1-59%   — fuzzy match, flagged for review
    MEDIUM     = 2,     // 60-89%  — vendor match but weak keyword
    HIGH       = 3      // 90-100% — exact vendor + keyword, pre-filled
}

public enum AuditAction
{
    AI_SUGGESTED,
    USER_MODIFIED,
    VALIDATED,
    POSTED,
    REJECTED,
    BULK_APPLIED,
    PO_INHERITED
}
