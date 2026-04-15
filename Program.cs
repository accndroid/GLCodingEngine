using FinOps.GLCodingEngine;
using FinOps.GLCodingEngine.Core.Enums;
using FinOps.GLCodingEngine.Core.Interfaces;
using FinOps.GLCodingEngine.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ════════════════════════════════════════════════════════════════
//  FinOps GL Coding Engine — Console Demo
//  Covers: Non-PO (HIGH/MEDIUM/LOW), PO Inherit, Bulk, Override,
//          Validation (pass + fail), Audit Trail
// ════════════════════════════════════════════════════════════════

const string CONN = "";
var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddGLCodingEngine(CONN);
var sp = services.BuildServiceProvider();
var engine = sp.GetRequiredService<IGLCodingEngine>();

Header("FinOps GL Coding Engine — Demo");
Console.WriteLine("  Scenarios: Non-PO (HIGH/MED/LOW), PO Inherit, Bulk, Override, Validate, Audit\n");

// ═══ 1. NON-PO HIGH — known vendor + exact keyword ═══
await RunScenario("1. NON-PO — HIGH confidence (known vendor + exact keyword)", async () =>
{
    // AGENTIC: Agent receives raw invoice line → extracts keywords → queries vendor rules
    //          → matches "Legal" → resolves all 6 dimensions → scores 90%+
    var r = await engine.SuggestAsync(new GLCodingRequest
    {
        InvoiceId = "DEMO-001", LineNumber = 1, ModuleCode = "AP", Mode = CodingMode.NON_PO,
        VendorName = "XYZ Legal Associates", VendorCode = "V2001",
        LineDescription = "Legal advisory for contract review Q3 2024", LineAmount = 200000,
        TaxType = "GST", TaxRate = 18, BillToLocation = "Mumbai", BillingEntity = "FAO India"
    });
    PrintResult(r);
});

// ═══ 2. NON-PO MEDIUM — vendor hit, weak keyword ═══
await RunScenario("2. NON-PO — MEDIUM confidence (vendor hit, weak keyword)", async () =>
{
    // AGENTIC: Vendor "Dell" known but "migration" misses all Dell rules
    //          → falls back to category match ("cloud" → CAT05) → score drops
    var r = await engine.SuggestAsync(new GLCodingRequest
    {
        InvoiceId = "DEMO-002", LineNumber = 1, ModuleCode = "AP", Mode = CodingMode.NON_PO,
        VendorName = "Dell Technologies", VendorCode = "V3001",
        LineDescription = "Cloud migration assessment and planning", LineAmount = 500000,
        TaxType = "GST", TaxRate = 18, BillToLocation = "Bangalore", BillingEntity = "FAO India"
    });
    PrintResult(r);
});

// ═══ 3. NON-PO LOW — unknown vendor, no keyword ═══
await RunScenario("3. NON-PO — LOW confidence (unknown vendor, no keyword)", async () =>
{
    // AGENTIC: Cold-start problem — no vendor rules, no category match
    //          → only deterministic fields (tax, location, company) resolve
    var r = await engine.SuggestAsync(new GLCodingRequest
    {
        InvoiceId = "DEMO-003", LineNumber = 1, ModuleCode = "AP", Mode = CodingMode.NON_PO,
        VendorName = "New Startup LLC", VendorCode = "V9999",
        LineDescription = "Annual subscription for WidgetPro platform", LineAmount = 95000,
        TaxType = "GST", TaxRate = 18, BillToLocation = "Pune", BillingEntity = "FAO India"
    });
    PrintResult(r);
});

// ═══ 4. PO-BASED — inherit from Purchase Order ═══
await RunScenario("4. PO-BASED — Inherit GL from Purchase Order", async () =>
{
    // AGENTIC: Deterministic path — GL codes pre-decided during procurement
    //          → no AI reasoning → confidence always 100%
    var r = await engine.InheritFromPOAsync(new GLCodingRequest
    {
        InvoiceId = "DEMO-004", LineNumber = 1, ModuleCode = "AP", Mode = CodingMode.PO,
        VendorName = "ABC Realty Pvt Ltd", VendorCode = "V1001",
        LineDescription = "Office rent Mumbai HQ October 2024", LineAmount = 450000,
        PONumber = "PO-2024-100", POLineNumber = 1,
        PO_GLCode = "610001", PO_CostCenter = "CC300", PO_TaxCode = "GST18",
        PO_LocationCode = "LOC01", PO_CompanyCode = "C001", PO_CategoryCode = "CAT01",
        TaxType = "GST", TaxRate = 18, BillToLocation = "Mumbai", BillingEntity = "FAO India"
    });
    PrintResult(r);
});

// ═══ 5. BULK — mixed PO + Non-PO batch ═══
await RunScenario("5. BULK — Mixed PO + Non-PO (4 lines, 2 invoices)", async () =>
{
    // AGENTIC: Same agent pipeline per line → aggregate confidence stats
    var r = await engine.BulkSuggestAsync(new BulkCodingRequest { Lines = [
        new() { InvoiceId="DEMO-005", LineNumber=1, ModuleCode="AP", Mode=CodingMode.NON_PO,
                VendorName="Deloitte India", VendorCode="V5001",
                LineDescription="Statutory audit FY 2023-24", LineAmount=800000,
                TaxType="GST", TaxRate=18, BillToLocation="Mumbai", BillingEntity="FAO India" },
        new() { InvoiceId="DEMO-005", LineNumber=2, ModuleCode="AP", Mode=CodingMode.NON_PO,
                VendorName="Deloitte India", VendorCode="V5001",
                LineDescription="Tax advisory for transfer pricing", LineAmount=350000,
                TaxType="GST", TaxRate=18, BillToLocation="Mumbai", BillingEntity="FAO India" },
        new() { InvoiceId="DEMO-006", LineNumber=1, ModuleCode="AP", Mode=CodingMode.PO,
                VendorName="ABC Realty", VendorCode="V1001", LineDescription="Warehouse rent Pune", LineAmount=180000,
                PONumber="PO-2024-200", POLineNumber=1, PO_GLCode="610002", PO_CostCenter="CC600",
                PO_TaxCode="GST18", PO_LocationCode="LOC03", PO_CompanyCode="C001", PO_CategoryCode="CAT01",
                TaxType="GST", TaxRate=18, BillToLocation="Pune", BillingEntity="FAO India" },
        new() { InvoiceId="DEMO-006", LineNumber=2, ModuleCode="AP", Mode=CodingMode.NON_PO,
                VendorName="RandomCorp", VendorCode="V0000",
                LineDescription="Miscellaneous charges", LineAmount=25000,
                TaxType="GST", TaxRate=18, BillToLocation="Chennai", BillingEntity="FAO India" },
    ] });
    PrintResult(r);
    if (r["Result"] is BulkCodingResult bulk)
    { Console.ForegroundColor=ConsoleColor.Cyan;
      Console.WriteLine($"\n  Bulk summary: {bulk.TotalLines} lines → {bulk.HighConfidence} HIGH, {bulk.MediumConfidence} MED, {bulk.LowConfidence} LOW, {bulk.Unresolved} unresolved");
      Console.ResetColor(); }
});

// ═══ 6. CF VERIFIER OVERRIDE ═══
await RunScenario("6. CF VERIFIER OVERRIDE — Human corrects AI suggestion", async () =>
{
    // AGENTIC: Human-in-the-loop feedback — delta (AI=null → Human=640002)
    //          captured in audit for future retraining
    var r = await engine.ApplyAsync(new ApplyCodingRequest
    {
        InvoiceId="DEMO-003", LineNumber=1, GLCode="640002", CostCenterCode="CC400",
        TaxCode="GST18", LocationCode="LOC03", CompanyCode="C001", CategoryCode="CAT05",
        ActorId="USR-RAVI", ActorName="Ravi Sharma (CF Verifier)"
    });
    PrintResult(r);
});

// ═══ 7. VALIDATION PASS ═══
await RunScenario("7. VALIDATION PASS — DEMO-001 (all fields coded by AI)", async () =>
{
    var r = await engine.ValidateAsync("DEMO-001"); PrintResult(r);
    if (r["Result"] is ValidationResult v) { Console.ForegroundColor=v.IsValid?ConsoleColor.Green:ConsoleColor.Red; Console.WriteLine($"  → IsValid: {v.IsValid}  Errors: {v.Errors.Count}"); Console.ResetColor(); }
});

// ═══ 8. VALIDATION FAIL ═══
await RunScenario("8. VALIDATION FAIL — DEMO-006 (line 2 missing GL/CC)", async () =>
{
    var r = await engine.ValidateAsync("DEMO-006"); PrintResult(r);
    if (r["Result"] is ValidationResult v) { Console.ForegroundColor=ConsoleColor.Red; foreach (var e in v.Errors) Console.WriteLine($"    Line {e.LineNumber} | {e.Field}: {e.Message}"); Console.ResetColor(); }
});

// ═══ 9. AUDIT TRAIL ═══
await RunScenario("9. AUDIT TRAIL — DEMO-003 (AI suggested → Human overrode)", async () =>
{
    // AGENTIC: Audit = learning corpus. Layer 1: AI suggested (gaps). Layer 2: Human corrected.
    var r = await engine.GetAuditTrailAsync("DEMO-003");
    if (r["Result"] is List<CodingAuditEntry> trail)
        foreach (var e in trail) { Console.ForegroundColor=e.Action==AuditAction.AI_SUGGESTED?ConsoleColor.Yellow:ConsoleColor.Cyan;
            Console.WriteLine($"  [{e.Action}] Line {e.LineNumber} | Actor: {e.ActorName} | GL: {e.NewValue ?? "(null)"} | Confidence: {e.Confidence?.ToString() ?? "n/a"}"); Console.ResetColor(); }
});

// ═══ 10. CODING STATUS ═══
await RunScenario("10. CODING STATUS — DEMO-005 final state", async () =>
{
    var r = await engine.GetCodingStatusAsync("DEMO-005");
    if (r["Result"] is List<GLCodingSuggestion> lines)
        foreach (var l in lines) { Console.ForegroundColor=l.ConfidenceLevel==ConfidenceLevel.HIGH?ConsoleColor.Green:l.ConfidenceLevel==ConfidenceLevel.MEDIUM?ConsoleColor.Yellow:ConsoleColor.Red;
            Console.WriteLine($"  Line {l.LineNumber} | GL:{l.GLCode ?? "?"} ({l.GLDescription ?? "?"}) | CC:{l.CostCenterCode ?? "?"} | Tax:{l.TaxCode ?? "?"} | {l.ConfidenceLevel} ({l.Confidence}%) | Rule:{l.MatchedRuleId ?? "none"}"); Console.ResetColor(); }
});

Header("Demo complete — all 10 scenarios executed");

// ═══ Helpers ═══
static void Header(string text) { Console.ForegroundColor=ConsoleColor.White; Console.WriteLine($"\n{"".PadRight(64,'═')}\n  {text}\n{"".PadRight(64,'═')}"); Console.ResetColor(); }
static async Task RunScenario(string title, Func<Task> action) { Console.ForegroundColor=ConsoleColor.White; Console.WriteLine($"\n── {title} ──"); Console.ResetColor(); try { await action(); } catch(Exception ex) { Console.ForegroundColor=ConsoleColor.Red; Console.WriteLine($"  ERROR: {ex.Message}"); Console.ResetColor(); } }
static void PrintResult(Dictionary<string,object> result) { var ok=result["Success"] is true; Console.ForegroundColor=ok?ConsoleColor.Green:ConsoleColor.Red; Console.Write($"  {(ok?"✓":"✗")} "); Console.ResetColor(); Console.WriteLine(result["Message"]);
    if(result["Result"] is GLCodingSuggestion s) { Console.ForegroundColor=ConsoleColor.DarkGray; Console.WriteLine($"    GL:{s.GLCode??"?"} | CC:{s.CostCenterCode??"?"} | Tax:{s.TaxCode??"?"} | Loc:{s.LocationCode??"?"} | Co:{s.CompanyCode??"?"} | Cat:{s.CategoryCode??"?"}");
        if(s.UnresolvedFields.Count>0){Console.ForegroundColor=ConsoleColor.DarkYellow;Console.WriteLine($"    Unresolved: {string.Join(", ",s.UnresolvedFields)}");}
        if(s.MatchedKeywords.Count>0) Console.WriteLine($"    Matched keywords: {string.Join(", ",s.MatchedKeywords)}"); Console.ResetColor(); } }
