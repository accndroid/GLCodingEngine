using System.Text.Json;
using FinOps.GLCodingEngine.Core.Enums;
using FinOps.GLCodingEngine.Core.Interfaces;
using FinOps.GLCodingEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace FinOps.GLCodingEngine.Services;

// AGENTIC: Main AI Agent orchestrator — Perceive → Reason → Act → Record loop

public sealed class GLCodingService : IGLCodingEngine
{
    private readonly IGLCodingRepository _repo;
    private readonly ILogger<GLCodingService> _logger;
    private readonly AIGLCodingAgent _aiAgent;
    public GLCodingService(IGLCodingRepository repo, ILogger<GLCodingService> logger, AIGLCodingAgent aiAgent)
    {
        _repo = repo; 
        _logger = logger; 
        _aiAgent = aiAgent;
    }

    public async Task<Dictionary<string, object>> SuggestAsync__OLD(GLCodingRequest request)
    {
        try
        {
            // AGENTIC: PERCEIVE — tokenize description into keywords
            var keywords = KeywordExtractor.Extract(request.LineDescription);

            // AGENTIC: REASON — query vendor mapping rules (knowledge base)
            var vendorMappings = await _repo.GetVendorMappingsAsync(request.VendorCode, request.VendorName);
            bool vendorExact = vendorMappings.Any(m => m.VendorCode == request.VendorCode);
            bool vendorFuzzy = !vendorExact && vendorMappings.Count > 0;

            // AGENTIC: MATCH — find best rule by keyword intersection (priority-ordered)
            VendorGLMapping? bestMatch = null; string? matchedKeyword = null;
            foreach (var mapping in vendorMappings.OrderBy(m => m.Priority))
            {
                var kwMatch = KeywordExtractor.FindMatch(keywords, mapping.DescKeyword);
                if (kwMatch != null) { bestMatch = mapping; matchedKeyword = kwMatch; break; }
            }

            // AGENTIC: RESOLVE — fill each dimension independently
            string? glCode = bestMatch?.GLCode, glDesc = null, ccCode = bestMatch?.CostCenterCode, ccName = null;
            string? taxCode = null, locCode = null, compCode = null, catCode = bestMatch?.CategoryCode, catName = null;
            var matchedKeywords = new List<string>(); var unresolvedFields = new List<string>();
            if (matchedKeyword != null) matchedKeywords.Add(matchedKeyword);

            // AGENTIC: GL fallback — vendor rule miss → category keyword match
            if (glCode == null)
                foreach (var kw in keywords)
                {
                    var cat = await _repo.FindCategoryByKeywordAsync(kw);
                    if (cat != null) { glCode = cat.DefaultGLCode; catCode ??= cat.Code; catName = cat.Name; matchedKeywords.Add(kw); break; }
                }

            if (glCode != null) { var gl = await _repo.GetGLAccountAsync(glCode); glDesc = gl?.GLDescription; } else unresolvedFields.Add("GLCode");
            if (ccCode != null) { var cc = await _repo.GetCostCenterAsync(ccCode); ccName = cc?.Name; } else unresolvedFields.Add("CostCenterCode");
            if (request.TaxType != null && request.TaxRate.HasValue) { var t = await _repo.FindTaxCodeAsync(request.TaxType, request.TaxRate.Value); taxCode = t?.TaxCode; }
            if (taxCode == null) unresolvedFields.Add("TaxCode");
            if (!string.IsNullOrWhiteSpace(request.BillToLocation)) { var l = await _repo.FindLocationAsync(request.BillToLocation); locCode = l?.Code; }
            if (locCode == null) unresolvedFields.Add("LocationCode");
            if (!string.IsNullOrWhiteSpace(request.BillingEntity)) { var co = await _repo.FindCompanyCodeAsync(request.BillingEntity); compCode = co?.Code; }
            if (compCode == null) unresolvedFields.Add("CompanyCode");
            if (catCode != null && catName == null) { var cats = await _repo.GetCategoriesAsync(); catName = cats.FirstOrDefault(c => c.Code == catCode)?.Name; }
            if (catCode == null) unresolvedFields.Add("CategoryCode");

            // AGENTIC: ASSESS — score confidence in own suggestion
            var (score, level) = ConfidenceScorer.Calculate(vendorExact, vendorFuzzy, matchedKeyword != null,
                ccCode != null, taxCode != null, locCode != null, compCode != null, catCode != null);

            // AGENTIC: ACT — produce suggestion
            var suggestion = new GLCodingSuggestion
            {
                InvoiceId = request.InvoiceId,
                LineNumber = request.LineNumber,
                CodingMode = "NON_PO",
                GLCode = glCode,
                GLDescription = glDesc,
                CostCenterCode = ccCode,
                CostCenterName = ccName,
                TaxCode = taxCode,
                LocationCode = locCode,
                CompanyCode = compCode,
                CategoryCode = catCode,
                CategoryName = catName,
                Confidence = score,
                ConfidenceLevel = level,
                MatchedRuleId = bestMatch?.RuleId,
                MatchedKeywords = matchedKeywords,
                UnresolvedFields = unresolvedFields
            };

            // AGENTIC: RECORD — persist for audit and future learning
            await _repo.UpsertCodingLineAsync(request, suggestion);
            await _repo.UpdateCodingStatusAsync(request.InvoiceId, request.LineNumber, "AI_SUGGESTED");
            await _repo.InsertAuditAsync(new CodingAuditEntry
            {
                InvoiceId = request.InvoiceId,
                LineNumber = request.LineNumber,
                Action = AuditAction.AI_SUGGESTED,
                NewValue = glCode,
                Confidence = score,
                MatchedRuleId = bestMatch?.RuleId,
                ActorId = "SYSTEM",
                ActorName = "AI Agent",
                Details = JsonSerializer.Serialize(suggestion)
            });

            return BuildResult(true, $"AI suggestion — {level} ({score}%)", suggestion);
        }
        catch (Exception ex) { _logger.LogError(ex, "SuggestAsync failed"); return BuildResult(false, ex.Message, null, ex.StackTrace); }
    }


    public async Task<Dictionary<string, object>> SuggestAsync(GLCodingRequest request)
    {
        try
        {
            // 1. GATHER CONTEXT (Perceive phase)
            // Get rules specific to this vendor to keep the LLM context window small and relevant
            var vendorMappings = await _repo.GetVendorMappingsAsync(request.VendorCode, request.VendorName);
            var categories = await _repo.GetCategoriesAsync();

            var contextData = new
            {
                VendorRules = vendorMappings.Select(v => new { v.RuleId, v.VendorName, v.DescKeyword, v.GLCode, v.CostCenterCode, v.CategoryCode }),
                FallbackCategories = categories.Select(c => new { c.Code, c.Name, c.Keywords, c.DefaultGLCode })
            };
            var contextJson = JsonSerializer.Serialize(contextData);

            // 2. PROMPT THE AGENT (Reason phase)
            var aiResponse = await _aiAgent.PredictAsync(request.VendorName, request.LineDescription, contextJson);

            if (aiResponse == null) throw new Exception("AI Agent returned a null response.");

            // 3. RESOLVE & VALIDATE AGAINST DB (Act phase)
            // Even though AI suggested codes, we query the DB to get the descriptions and ensure they actually exist.
            string? glDesc = null, ccName = null, catName = null, taxCode = null, locCode = null, compCode = null;
            var unresolvedFields = new List<string>();

            if (aiResponse.GlCode != null) { var gl = await _repo.GetGLAccountAsync(aiResponse.GlCode); glDesc = gl?.GLDescription; } else unresolvedFields.Add("GLCode");
            if (aiResponse.CostCenterCode != null) { var cc = await _repo.GetCostCenterAsync(aiResponse.CostCenterCode); ccName = cc?.Name; } else unresolvedFields.Add("CostCenterCode");
            if (aiResponse.CategoryCode != null) { var cat = await _repo.GetCategoriesAsync(); catName = cat.FirstOrDefault(c => c.Code == aiResponse.CategoryCode)?.Name; } else unresolvedFields.Add("CategoryCode");

            // Deterministic fields (Tax, Location, Company) are still resolved via hard logic to ensure strict compliance
            if (request.TaxType != null && request.TaxRate.HasValue) { var t = await _repo.FindTaxCodeAsync(request.TaxType, request.TaxRate.Value); taxCode = t?.TaxCode; }
            if (taxCode == null) unresolvedFields.Add("TaxCode");

            if (!string.IsNullOrWhiteSpace(request.BillToLocation)) { var l = await _repo.FindLocationAsync(request.BillToLocation); locCode = l?.Code; }
            if (locCode == null) unresolvedFields.Add("LocationCode");

            if (!string.IsNullOrWhiteSpace(request.BillingEntity)) { var co = await _repo.FindCompanyCodeAsync(request.BillingEntity); compCode = co?.Code; }
            if (compCode == null) unresolvedFields.Add("CompanyCode");

            // Map AI numerical score to your Enum
            var level = aiResponse.ConfidenceScore switch
            {
                >= 90 => ConfidenceLevel.HIGH,
                >= 60 => ConfidenceLevel.MEDIUM,
                > 0 => ConfidenceLevel.LOW,
                _ => ConfidenceLevel.UNRESOLVED
            };

            var suggestion = new GLCodingSuggestion
            {
                InvoiceId = request.InvoiceId,
                LineNumber = request.LineNumber,
                CodingMode = "NON_PO",
                GLCode = aiResponse.GlCode,
                GLDescription = glDesc,
                CostCenterCode = aiResponse.CostCenterCode,
                CostCenterName = ccName,
                TaxCode = taxCode,
                LocationCode = locCode,
                CompanyCode = compCode,
                CategoryCode = aiResponse.CategoryCode,
                CategoryName = catName,
                Confidence = aiResponse.ConfidenceScore,
                ConfidenceLevel = level,
                MatchedRuleId = aiResponse.MatchedRuleId,
                UnresolvedFields = unresolvedFields,
                MatchedKeywords = new List<string> { $"AI Reasoning: {aiResponse.Reasoning}" } // Repurposing this to show the CF Verifier why the AI chose it
            };

            // 4. RECORD (Audit phase)
            await _repo.UpsertCodingLineAsync(request, suggestion);
            await _repo.UpdateCodingStatusAsync(request.InvoiceId, request.LineNumber, "AI_SUGGESTED");
            await _repo.InsertAuditAsync(new CodingAuditEntry
            {
                InvoiceId = request.InvoiceId,
                LineNumber = request.LineNumber,
                Action = AuditAction.AI_SUGGESTED,
                NewValue = aiResponse.GlCode,
                Confidence = aiResponse.ConfidenceScore,
                MatchedRuleId = aiResponse.MatchedRuleId,
                ActorId = "SYSTEM",
                ActorName = "GenAI Agent",
                Details = JsonSerializer.Serialize(suggestion)
            });

            return BuildResult(true, $"AI suggestion — {level} ({aiResponse.ConfidenceScore}%)", suggestion);
        }
        catch (Exception ex) { _logger.LogError(ex, "SuggestAsync failed"); return BuildResult(false, ex.Message, null, ex.StackTrace); }
    }

    // AGENTIC: PO path = deterministic, no AI reasoning, confidence always 100%
    public async Task<Dictionary<string, object>> InheritFromPOAsync(GLCodingRequest request)
    {
        try
        {
            if (request.Mode != CodingMode.PO) return BuildResult(false, "Mode must be PO", null);
            var suggestion = new GLCodingSuggestion
            {
                InvoiceId = request.InvoiceId,
                LineNumber = request.LineNumber,
                CodingMode = "PO",
                GLCode = request.PO_GLCode,
                CostCenterCode = request.PO_CostCenter,
                TaxCode = request.PO_TaxCode,
                LocationCode = request.PO_LocationCode,
                CompanyCode = request.PO_CompanyCode,
                CategoryCode = request.PO_CategoryCode,
                Confidence = 100,
                ConfidenceLevel = ConfidenceLevel.HIGH,
                MatchedRuleId = $"PO:{request.PONumber}:{request.POLineNumber}",
                UnresolvedFields = []
            };
            if (suggestion.GLCode != null) { var gl = await _repo.GetGLAccountAsync(suggestion.GLCode); suggestion = suggestion with { GLDescription = gl?.GLDescription }; }
            await _repo.UpsertCodingLineAsync(request, suggestion);
            await _repo.UpdateCodingStatusAsync(request.InvoiceId, request.LineNumber, "AI_SUGGESTED");
            await _repo.InsertAuditAsync(new CodingAuditEntry
            {
                InvoiceId = request.InvoiceId,
                LineNumber = request.LineNumber,
                Action = AuditAction.PO_INHERITED,
                NewValue = suggestion.GLCode,
                Confidence = 100,
                MatchedRuleId = suggestion.MatchedRuleId,
                ActorId = "SYSTEM",
                ActorName = "PO Inheritance",
                Details = JsonSerializer.Serialize(suggestion)
            });
            return BuildResult(true, "PO inherited — HIGH (100%)", suggestion);
        }
        catch (Exception ex) { _logger.LogError(ex, "InheritFromPOAsync failed"); return BuildResult(false, ex.Message, null, ex.StackTrace); }
    }

    // AGENTIC: Bulk = same agent pipeline per line, then aggregate stats
    public async Task<Dictionary<string, object>> BulkSuggestAsync(BulkCodingRequest request)
    {
        try
        {
            var suggestions = new List<GLCodingSuggestion>();
            foreach (var line in request.Lines)
            {
                var r = line.Mode == CodingMode.PO ? await InheritFromPOAsync(line) : await SuggestAsync(line);
                if (r["Success"] is true && r["Result"] is GLCodingSuggestion s) suggestions.Add(s);
            }
            var bulk = new BulkCodingResult
            {
                TotalLines = suggestions.Count,
                HighConfidence = suggestions.Count(s => s.ConfidenceLevel == ConfidenceLevel.HIGH),
                MediumConfidence = suggestions.Count(s => s.ConfidenceLevel == ConfidenceLevel.MEDIUM),
                LowConfidence = suggestions.Count(s => s.ConfidenceLevel == ConfidenceLevel.LOW),
                Unresolved = suggestions.Count(s => s.ConfidenceLevel == ConfidenceLevel.UNRESOLVED),
                Suggestions = suggestions
            };
            return BuildResult(true, $"Bulk: {bulk.HighConfidence} HIGH, {bulk.MediumConfidence} MED, {bulk.LowConfidence} LOW, {bulk.Unresolved} unresolved", bulk);
        }
        catch (Exception ex) { return BuildResult(false, ex.Message, null, ex.StackTrace); }
    }

    // AGENTIC: Human-in-the-loop — delta between AI and human = training signal
    public async Task<Dictionary<string, object>> ApplyAsync(ApplyCodingRequest request)
    {
        try
        {
            await _repo.UpdateFinalCodingAsync(request);
            await _repo.UpdateCodingStatusAsync(request.InvoiceId, request.LineNumber, "MANUALLY_CODED");
            await _repo.InsertAuditAsync(new CodingAuditEntry
            {
                InvoiceId = request.InvoiceId,
                LineNumber = request.LineNumber,
                Action = AuditAction.USER_MODIFIED,
                NewValue = request.GLCode,
                ActorId = request.ActorId,
                ActorName = request.ActorName,
                Details = JsonSerializer.Serialize(request)
            });
            return BuildResult(true, "GL coding applied by CF Verifier", request);
        }
        catch (Exception ex) { return BuildResult(false, ex.Message, null, ex.StackTrace); }
    }

    public async Task<Dictionary<string, object>> BulkApplyAsync(List<ApplyCodingRequest> requests)
    {
        try { int n = 0; foreach (var req in requests) { var r = await ApplyAsync(req); if (r["Success"] is true) n++; } return BuildResult(true, $"Bulk apply: {n}/{requests.Count}", n); }
        catch (Exception ex) { return BuildResult(false, ex.Message, null, ex.StackTrace); }
    }

    public async Task<Dictionary<string, object>> ValidateAsync(string invoiceId)
    {
        try
        {
            var lines = await _repo.GetCodingLinesAsync(invoiceId); var errors = new List<ValidationError>();
            foreach (var l in lines)
            {
                if (string.IsNullOrEmpty(l.GLCode)) errors.Add(new() { InvoiceId = invoiceId, LineNumber = l.LineNumber, Field = "GLCode", Message = "GL code missing — cannot post to ERP" });
                if (string.IsNullOrEmpty(l.CostCenterCode)) errors.Add(new() { InvoiceId = invoiceId, LineNumber = l.LineNumber, Field = "CostCenterCode", Message = "Cost center is required" });
                if (string.IsNullOrEmpty(l.CompanyCode)) errors.Add(new() { InvoiceId = invoiceId, LineNumber = l.LineNumber, Field = "CompanyCode", Message = "Company code is required" });
            }
            if (lines.Count == 0) errors.Add(new() { InvoiceId = invoiceId, LineNumber = 0, Field = "Invoice", Message = "No coding lines found" });
            var v = new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
            if (v.IsValid) foreach (var l in lines) await _repo.UpdateCodingStatusAsync(invoiceId, l.LineNumber, "VALIDATED");
            return BuildResult(true, v.IsValid ? "Validation PASSED — ready for ERP" : $"Validation FAILED — {errors.Count} error(s)", v);
        }
        catch (Exception ex) { return BuildResult(false, ex.Message, null, ex.StackTrace); }
    }

    public async Task<Dictionary<string, object>> GetCodingStatusAsync(string invoiceId)
    { try { return BuildResult(true, "OK", await _repo.GetCodingLinesAsync(invoiceId)); } catch (Exception ex) { return BuildResult(false, ex.Message, null, ex.StackTrace); } }

    public async Task<Dictionary<string, object>> GetAuditTrailAsync(string invoiceId)
    { try { return BuildResult(true, "OK", await _repo.GetAuditTrailAsync(invoiceId)); } catch (Exception ex) { return BuildResult(false, ex.Message, null, ex.StackTrace); } }

    public async Task<Dictionary<string, object>> GetMasterDataAsync(string masterType)
    {
        try
        {
            object d = masterType.ToLower() switch
            {
                "gl" => await _repo.GetAllGLAccountsAsync(),
                "costcenter" => await _repo.GetAllCostCentersAsync(),
                "tax" => await _repo.GetAllTaxCodesAsync(),
                "location" => await _repo.GetAllLocationsAsync(),
                "company" => await _repo.GetAllCompanyCodesAsync(),
                "category" => await _repo.GetAllCategoriesAsync(),
                _ => throw new ArgumentException($"Unknown: {masterType}")
            };
            return BuildResult(true, $"Loaded: {masterType}", d);
        }
        catch (Exception ex) { return BuildResult(false, ex.Message, null, ex.StackTrace); }
    }

    private static Dictionary<string, object> BuildResult(bool success, string message, object? result, string? stackTrace = null)
    {
        var d = new Dictionary<string, object>
        {
            ["Success"] = success,
            ["Message"] = message,
            ["Result"] = result!,
            ["Audit"] = JsonSerializer.Serialize(new { Timestamp = DateTime.UtcNow, Success = success, Message = message })
        };
        if (stackTrace != null) d["StackTrace"] = stackTrace; return d;
    }
}
