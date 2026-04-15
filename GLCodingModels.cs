using FinOps.GLCodingEngine.Core.Enums;

namespace FinOps.GLCodingEngine.Core.Models;

// ── Input ──
public sealed record GLCodingRequest
{
    public required string InvoiceId    { get; init; }
    public required int    LineNumber   { get; init; }
    public required string ModuleCode   { get; init; }
    public required CodingMode Mode     { get; init; }
    public string?  VendorName          { get; init; }
    public string?  VendorCode          { get; init; }
    public string?  LineDescription     { get; init; }
    public decimal  LineAmount          { get; init; }
    public string   Currency            { get; init; } = "INR";
    public string?  TaxType             { get; init; }
    public decimal? TaxRate             { get; init; }
    public string?  BillToLocation      { get; init; }
    public string?  BillingEntity       { get; init; }
    public string?  PONumber            { get; init; }
    public int?     POLineNumber        { get; init; }
    public string?  PO_GLCode           { get; init; }
    public string?  PO_CostCenter       { get; init; }
    public string?  PO_TaxCode          { get; init; }
    public string?  PO_LocationCode     { get; init; }
    public string?  PO_CompanyCode      { get; init; }
    public string?  PO_CategoryCode     { get; init; }
}

public sealed record BulkCodingRequest
{
    public required List<GLCodingRequest> Lines { get; init; }
    public string? ActorId   { get; init; }
    public string? ActorName { get; init; }
}

// ── Output ──
public sealed record GLCodingSuggestion
{
    public string   InvoiceId       { get; init; } = "";
    public int      LineNumber      { get; init; }
    public string   CodingMode      { get; init; } = "";
    public string?  GLCode          { get; init; }
    public string?  GLDescription   { get; init; }
    public string?  CostCenterCode  { get; init; }
    public string?  CostCenterName  { get; init; }
    public string?  TaxCode         { get; init; }
    public string?  LocationCode    { get; init; }
    public string?  CompanyCode     { get; init; }
    public string?  CategoryCode    { get; init; }
    public string?  CategoryName    { get; init; }
    public int              Confidence      { get; init; }
    public ConfidenceLevel  ConfidenceLevel { get; init; }
    public string?          MatchedRuleId   { get; init; }
    public List<string>     MatchedKeywords  { get; init; } = [];
    public List<string>     UnresolvedFields { get; init; } = [];
}

public sealed record BulkCodingResult
{
    public int TotalLines       { get; init; }
    public int HighConfidence   { get; init; }
    public int MediumConfidence { get; init; }
    public int LowConfidence    { get; init; }
    public int Unresolved       { get; init; }
    public List<GLCodingSuggestion> Suggestions { get; init; } = [];
}

public sealed record ValidationResult
{
    public bool IsValid                 { get; init; }
    public List<ValidationError> Errors { get; init; } = [];
}

public sealed record ValidationError
{
    public string InvoiceId  { get; init; } = "";
    public int    LineNumber { get; init; }
    public string Field      { get; init; } = "";
    public string Message    { get; init; } = "";
}

public sealed record ApplyCodingRequest
{
    public required string InvoiceId      { get; init; }
    public required int    LineNumber     { get; init; }
    public string?  GLCode               { get; init; }
    public string?  CostCenterCode       { get; init; }
    public string?  TaxCode              { get; init; }
    public string?  LocationCode         { get; init; }
    public string?  CompanyCode          { get; init; }
    public string?  CategoryCode         { get; init; }
    public string?  ActorId              { get; init; }
    public string?  ActorName            { get; init; }
}

// ── Master data records ──
public sealed record GLAccount(string GLCode, string GLDescription, string AccountType, bool IsActive);
public sealed record CostCenter(string Code, string Name, bool IsActive);
public sealed record TaxCodeRecord(string TaxCode, string TaxType, decimal TaxRate, string TaxGLAccount, bool IsActive);
public sealed record LocationRecord(string Code, string Name, string City, string Country, bool IsActive);
public sealed record CompanyCodeRecord(string Code, string EntityName, string Country, string Currency, bool IsActive);
public sealed record CategoryRecord(string Code, string Name, string Keywords, string? DefaultGLCode, bool IsActive);

public sealed record VendorGLMapping
{
    public string  RuleId          { get; init; } = "";
    public string  VendorName      { get; init; } = "";
    public string? VendorCode      { get; init; }
    public string  DescKeyword     { get; init; } = "";
    public string  GLCode          { get; init; } = "";
    public string? CostCenterCode  { get; init; }
    public string? CategoryCode    { get; init; }
    public string? BusinessUnit    { get; init; }
    public int     Priority        { get; init; }
}

public sealed record CodingAuditEntry
{
    public string       InvoiceId     { get; init; } = "";
    public int          LineNumber    { get; init; }
    public AuditAction  Action        { get; init; }
    public string?      FieldName     { get; init; }
    public string?      OldValue      { get; init; }
    public string?      NewValue      { get; init; }
    public int?         Confidence    { get; init; }
    public string?      MatchedRuleId { get; init; }
    public string?      ActorId       { get; init; }
    public string?      ActorName     { get; init; }
    public string?      Details       { get; init; }
}
