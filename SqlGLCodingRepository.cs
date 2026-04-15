using FinOps.GLCodingEngine.Core.Interfaces;
using FinOps.GLCodingEngine.Core.Models;
using FinOps.GLCodingEngine.Core.Enums;
using System.Data.SqlClient;


namespace FinOps.GLCodingEngine.Data;

public sealed class SqlGLCodingRepository : IGLCodingRepository
{
    private readonly string _cs;
    public SqlGLCodingRepository(string connectionString) => _cs = connectionString;

    public async Task<List<VendorGLMapping>> GetVendorMappingsAsync(string? vendorCode, string? vendorName)
    {
        await using var c = new SqlConnection(_cs); await c.OpenAsync();
        await using var cmd = new SqlCommand(@"SELECT RuleId,VendorName,VendorCode,DescKeyword,GLCode,CostCenterCode,CategoryCode,BusinessUnit,Priority
            FROM GL_VendorMappings WHERE IsActive=1 AND (EffectiveTo IS NULL OR EffectiveTo>=GETUTCDATE())
            AND (@VC IS NULL OR VendorCode=@VC OR VendorName LIKE '%'+@VN+'%') ORDER BY Priority", c);
        cmd.Parameters.AddWithValue("@VC", (object?)vendorCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@VN", (object?)vendorName ?? DBNull.Value);
        var list = new List<VendorGLMapping>(); await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new() { RuleId=r.GetString(0),VendorName=r.GetString(1),
            VendorCode=r.IsDBNull(2)?null:r.GetString(2),DescKeyword=r.GetString(3),GLCode=r.GetString(4),
            CostCenterCode=r.IsDBNull(5)?null:r.GetString(5),CategoryCode=r.IsDBNull(6)?null:r.GetString(6),
            BusinessUnit=r.IsDBNull(7)?null:r.GetString(7),Priority=r.GetInt32(8) });
        return list;
    }

    public async Task<GLAccount?> GetGLAccountAsync(string glCode)
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); await using var cmd=new SqlCommand("SELECT GLCode,GLDescription,AccountType,IsActive FROM GL_ChartOfAccounts WHERE GLCode=@C AND IsActive=1",c); cmd.Parameters.AddWithValue("@C",glCode); await using var r=await cmd.ExecuteReaderAsync(); return await r.ReadAsync()?new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetBoolean(3)):null; }

    public async Task<CostCenter?> GetCostCenterAsync(string code)
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); await using var cmd=new SqlCommand("SELECT CostCenterCode,CostCenterName,IsActive FROM GL_CostCenters WHERE CostCenterCode=@C AND IsActive=1",c); cmd.Parameters.AddWithValue("@C",code); await using var r=await cmd.ExecuteReaderAsync(); return await r.ReadAsync()?new(r.GetString(0),r.GetString(1),r.GetBoolean(2)):null; }

    public async Task<TaxCodeRecord?> FindTaxCodeAsync(string taxType, decimal taxRate)
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); await using var cmd=new SqlCommand("SELECT TaxCode,TaxType,TaxRate,TaxGLAccount,IsActive FROM GL_TaxCodes WHERE TaxType=@T AND TaxRate=@R AND IsActive=1",c); cmd.Parameters.AddWithValue("@T",taxType); cmd.Parameters.AddWithValue("@R",taxRate); await using var r=await cmd.ExecuteReaderAsync(); return await r.ReadAsync()?new(r.GetString(0),r.GetString(1),r.GetDecimal(2),r.GetString(3),r.GetBoolean(4)):null; }

    public async Task<LocationRecord?> FindLocationAsync(string locationText)
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); await using var cmd=new SqlCommand("SELECT LocationCode,LocationName,City,Country,IsActive FROM GL_Locations WHERE IsActive=1 AND (City LIKE '%'+@T+'%' OR LocationName LIKE '%'+@T+'%')",c); cmd.Parameters.AddWithValue("@T",locationText); await using var r=await cmd.ExecuteReaderAsync(); return await r.ReadAsync()?new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetBoolean(4)):null; }

    public async Task<CompanyCodeRecord?> FindCompanyCodeAsync(string entityName)
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); await using var cmd=new SqlCommand("SELECT CompanyCode,EntityName,Country,Currency,IsActive FROM GL_CompanyCodes WHERE IsActive=1 AND EntityName LIKE '%'+@N+'%'",c); cmd.Parameters.AddWithValue("@N",entityName); await using var r=await cmd.ExecuteReaderAsync(); return await r.ReadAsync()?new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetBoolean(4)):null; }

    public async Task<CategoryRecord?> FindCategoryByKeywordAsync(string keyword)
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); await using var cmd=new SqlCommand("SELECT CategoryCode,CategoryName,Keywords,DefaultGLCode,IsActive FROM GL_ProductCategories WHERE IsActive=1 AND Keywords LIKE '%'+@K+'%'",c); cmd.Parameters.AddWithValue("@K",keyword); await using var r=await cmd.ExecuteReaderAsync(); return await r.ReadAsync()?new(r.GetString(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.GetBoolean(4)):null; }

    public async Task<List<CategoryRecord>> GetCategoriesAsync()
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); var l=new List<CategoryRecord>(); await using var cmd=new SqlCommand("SELECT CategoryCode,CategoryName,Keywords,DefaultGLCode,IsActive FROM GL_ProductCategories WHERE IsActive=1",c); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) l.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.GetBoolean(4))); return l; }

    public async Task<List<GLAccount>> GetAllGLAccountsAsync() { await using var c=new SqlConnection(_cs); await c.OpenAsync(); var l=new List<GLAccount>(); await using var cmd=new SqlCommand("SELECT GLCode,GLDescription,AccountType,IsActive FROM GL_ChartOfAccounts WHERE IsActive=1 ORDER BY GLCode",c); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) l.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetBoolean(3))); return l; }
    public async Task<List<CostCenter>> GetAllCostCentersAsync() { await using var c=new SqlConnection(_cs); await c.OpenAsync(); var l=new List<CostCenter>(); await using var cmd=new SqlCommand("SELECT CostCenterCode,CostCenterName,IsActive FROM GL_CostCenters WHERE IsActive=1 ORDER BY CostCenterCode",c); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) l.Add(new(r.GetString(0),r.GetString(1),r.GetBoolean(2))); return l; }
    public async Task<List<TaxCodeRecord>> GetAllTaxCodesAsync() { await using var c=new SqlConnection(_cs); await c.OpenAsync(); var l=new List<TaxCodeRecord>(); await using var cmd=new SqlCommand("SELECT TaxCode,TaxType,TaxRate,TaxGLAccount,IsActive FROM GL_TaxCodes WHERE IsActive=1 ORDER BY TaxCode",c); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) l.Add(new(r.GetString(0),r.GetString(1),r.GetDecimal(2),r.GetString(3),r.GetBoolean(4))); return l; }
    public async Task<List<LocationRecord>> GetAllLocationsAsync() { await using var c=new SqlConnection(_cs); await c.OpenAsync(); var l=new List<LocationRecord>(); await using var cmd=new SqlCommand("SELECT LocationCode,LocationName,City,Country,IsActive FROM GL_Locations WHERE IsActive=1 ORDER BY LocationCode",c); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) l.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetBoolean(4))); return l; }
    public async Task<List<CompanyCodeRecord>> GetAllCompanyCodesAsync() { await using var c=new SqlConnection(_cs); await c.OpenAsync(); var l=new List<CompanyCodeRecord>(); await using var cmd=new SqlCommand("SELECT CompanyCode,EntityName,Country,Currency,IsActive FROM GL_CompanyCodes WHERE IsActive=1 ORDER BY CompanyCode",c); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) l.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetBoolean(4))); return l; }
    public async Task<List<CategoryRecord>> GetAllCategoriesAsync() { await using var c=new SqlConnection(_cs); await c.OpenAsync(); var l=new List<CategoryRecord>(); await using var cmd=new SqlCommand("SELECT CategoryCode,CategoryName,Keywords,DefaultGLCode,IsActive FROM GL_ProductCategories WHERE IsActive=1 ORDER BY CategoryCode",c); await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) l.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.GetBoolean(4))); return l; }

    public async Task UpsertCodingLineAsync(GLCodingRequest request, GLCodingSuggestion suggestion)
    {
        await using var c = new SqlConnection(_cs); await c.OpenAsync();
        await using var cmd = new SqlCommand(@"MERGE GL_CodingLines AS t USING (SELECT @InvId AS InvoiceId, @Ln AS LineNumber) AS s
            ON t.InvoiceId=s.InvoiceId AND t.LineNumber=s.LineNumber
            WHEN MATCHED THEN UPDATE SET AI_GLCode=@GL,AI_CostCenterCode=@CC,AI_TaxCode=@Tax,AI_LocationCode=@Loc,
                AI_CompanyCode=@Comp,AI_CategoryCode=@Cat,AI_Confidence=@Conf,AI_MatchedRuleId=@Rule,AI_SuggestedAt=SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT (InvoiceId,LineNumber,ModuleCode,CodingMode,VendorName,VendorCode,LineDescription,LineAmount,Currency,
                TaxType,TaxRate,BillToLocation,BillingEntity,PONumber,POLineNumber,AI_GLCode,AI_CostCenterCode,AI_TaxCode,AI_LocationCode,
                AI_CompanyCode,AI_CategoryCode,AI_Confidence,AI_MatchedRuleId,AI_SuggestedAt,CodingStatus) VALUES
                (@InvId,@Ln,@Mod,@Mode,@VN,@VC,@Desc,@Amt,@Cur,@TT,@TR,@BL,@BE,@PO,@POL,@GL,@CC,@Tax,@Loc,@Comp,@Cat,@Conf,@Rule,SYSUTCDATETIME(),'AI_SUGGESTED');", c);
        cmd.Parameters.AddWithValue("@InvId",request.InvoiceId); cmd.Parameters.AddWithValue("@Ln",request.LineNumber);
        cmd.Parameters.AddWithValue("@Mod",request.ModuleCode); cmd.Parameters.AddWithValue("@Mode",request.Mode.ToString());
        cmd.Parameters.AddWithValue("@VN",(object?)request.VendorName??DBNull.Value); cmd.Parameters.AddWithValue("@VC",(object?)request.VendorCode??DBNull.Value);
        cmd.Parameters.AddWithValue("@Desc",(object?)request.LineDescription??DBNull.Value); cmd.Parameters.AddWithValue("@Amt",request.LineAmount);
        cmd.Parameters.AddWithValue("@Cur",request.Currency); cmd.Parameters.AddWithValue("@TT",(object?)request.TaxType??DBNull.Value);
        cmd.Parameters.AddWithValue("@TR",(object?)request.TaxRate??DBNull.Value); cmd.Parameters.AddWithValue("@BL",(object?)request.BillToLocation??DBNull.Value);
        cmd.Parameters.AddWithValue("@BE",(object?)request.BillingEntity??DBNull.Value); cmd.Parameters.AddWithValue("@PO",(object?)request.PONumber??DBNull.Value);
        cmd.Parameters.AddWithValue("@POL",(object?)request.POLineNumber??DBNull.Value);
        cmd.Parameters.AddWithValue("@GL",(object?)suggestion.GLCode??DBNull.Value); cmd.Parameters.AddWithValue("@CC",(object?)suggestion.CostCenterCode??DBNull.Value);
        cmd.Parameters.AddWithValue("@Tax",(object?)suggestion.TaxCode??DBNull.Value); cmd.Parameters.AddWithValue("@Loc",(object?)suggestion.LocationCode??DBNull.Value);
        cmd.Parameters.AddWithValue("@Comp",(object?)suggestion.CompanyCode??DBNull.Value); cmd.Parameters.AddWithValue("@Cat",(object?)suggestion.CategoryCode??DBNull.Value);
        cmd.Parameters.AddWithValue("@Conf",suggestion.Confidence); cmd.Parameters.AddWithValue("@Rule",(object?)suggestion.MatchedRuleId??DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateFinalCodingAsync(ApplyCodingRequest request)
    {
        await using var c = new SqlConnection(_cs); await c.OpenAsync();
        await using var cmd = new SqlCommand(@"UPDATE GL_CodingLines SET Final_GLCode=@GL,Final_CostCenterCode=@CC,Final_TaxCode=@Tax,
            Final_LocationCode=@Loc,Final_CompanyCode=@Comp,Final_CategoryCode=@Cat,
            CodingStatus='MANUALLY_CODED',ModifiedBy=@A,ModifiedAt=SYSUTCDATETIME() WHERE InvoiceId=@I AND LineNumber=@L", c);
        cmd.Parameters.AddWithValue("@I",request.InvoiceId); cmd.Parameters.AddWithValue("@L",request.LineNumber);
        cmd.Parameters.AddWithValue("@GL",(object?)request.GLCode??DBNull.Value); cmd.Parameters.AddWithValue("@CC",(object?)request.CostCenterCode??DBNull.Value);
        cmd.Parameters.AddWithValue("@Tax",(object?)request.TaxCode??DBNull.Value); cmd.Parameters.AddWithValue("@Loc",(object?)request.LocationCode??DBNull.Value);
        cmd.Parameters.AddWithValue("@Comp",(object?)request.CompanyCode??DBNull.Value); cmd.Parameters.AddWithValue("@Cat",(object?)request.CategoryCode??DBNull.Value);
        cmd.Parameters.AddWithValue("@A",(object?)request.ActorId??DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<GLCodingSuggestion>> GetCodingLinesAsync(string invoiceId)
    {
        await using var c = new SqlConnection(_cs); await c.OpenAsync();
        await using var cmd = new SqlCommand(@"SELECT cl.InvoiceId,cl.LineNumber,cl.CodingMode,
            COALESCE(cl.Final_GLCode,cl.AI_GLCode),coa.GLDescription,COALESCE(cl.Final_CostCenterCode,cl.AI_CostCenterCode),cc.CostCenterName,
            COALESCE(cl.Final_TaxCode,cl.AI_TaxCode),COALESCE(cl.Final_LocationCode,cl.AI_LocationCode),COALESCE(cl.Final_CompanyCode,cl.AI_CompanyCode),
            COALESCE(cl.Final_CategoryCode,cl.AI_CategoryCode),pc.CategoryName,cl.AI_Confidence,cl.AI_MatchedRuleId,cl.CodingStatus
            FROM GL_CodingLines cl LEFT JOIN GL_ChartOfAccounts coa ON coa.GLCode=COALESCE(cl.Final_GLCode,cl.AI_GLCode)
            LEFT JOIN GL_CostCenters cc ON cc.CostCenterCode=COALESCE(cl.Final_CostCenterCode,cl.AI_CostCenterCode)
            LEFT JOIN GL_ProductCategories pc ON pc.CategoryCode=COALESCE(cl.Final_CategoryCode,cl.AI_CategoryCode)
            WHERE cl.InvoiceId=@I ORDER BY cl.LineNumber", c);
        cmd.Parameters.AddWithValue("@I", invoiceId);
        var list = new List<GLCodingSuggestion>(); await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) { var conf=r.IsDBNull(12)?0:r.GetInt32(12);
            list.Add(new() { InvoiceId=r.GetString(0),LineNumber=r.GetInt32(1),CodingMode=r.GetString(2),
                GLCode=r.IsDBNull(3)?null:r.GetString(3),GLDescription=r.IsDBNull(4)?null:r.GetString(4),
                CostCenterCode=r.IsDBNull(5)?null:r.GetString(5),CostCenterName=r.IsDBNull(6)?null:r.GetString(6),
                TaxCode=r.IsDBNull(7)?null:r.GetString(7),LocationCode=r.IsDBNull(8)?null:r.GetString(8),
                CompanyCode=r.IsDBNull(9)?null:r.GetString(9),CategoryCode=r.IsDBNull(10)?null:r.GetString(10),
                CategoryName=r.IsDBNull(11)?null:r.GetString(11),Confidence=conf,
                ConfidenceLevel=conf>=90?ConfidenceLevel.HIGH:conf>=60?ConfidenceLevel.MEDIUM:conf>0?ConfidenceLevel.LOW:ConfidenceLevel.UNRESOLVED,
                MatchedRuleId=r.IsDBNull(13)?null:r.GetString(13) }); }
        return list;
    }

    public async Task UpdateCodingStatusAsync(string invoiceId, int lineNumber, string status)
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); await using var cmd=new SqlCommand("UPDATE GL_CodingLines SET CodingStatus=@S WHERE InvoiceId=@I AND LineNumber=@L",c);
      cmd.Parameters.AddWithValue("@I",invoiceId); cmd.Parameters.AddWithValue("@L",lineNumber); cmd.Parameters.AddWithValue("@S",status); await cmd.ExecuteNonQueryAsync(); }

    public async Task InsertAuditAsync(CodingAuditEntry entry)
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); await using var cmd=new SqlCommand(@"INSERT INTO GL_CodingAudit (InvoiceId,LineNumber,Action,FieldName,OldValue,NewValue,Confidence,MatchedRuleId,ActorId,ActorName,Details) VALUES (@I,@L,@A,@F,@O,@N,@C,@R,@AI,@AN,@D)",c);
      cmd.Parameters.AddWithValue("@I",entry.InvoiceId); cmd.Parameters.AddWithValue("@L",entry.LineNumber); cmd.Parameters.AddWithValue("@A",entry.Action.ToString());
      cmd.Parameters.AddWithValue("@F",(object?)entry.FieldName??DBNull.Value); cmd.Parameters.AddWithValue("@O",(object?)entry.OldValue??DBNull.Value);
      cmd.Parameters.AddWithValue("@N",(object?)entry.NewValue??DBNull.Value); cmd.Parameters.AddWithValue("@C",(object?)entry.Confidence??DBNull.Value);
      cmd.Parameters.AddWithValue("@R",(object?)entry.MatchedRuleId??DBNull.Value); cmd.Parameters.AddWithValue("@AI",(object?)entry.ActorId??DBNull.Value);
      cmd.Parameters.AddWithValue("@AN",(object?)entry.ActorName??DBNull.Value); cmd.Parameters.AddWithValue("@D",(object?)entry.Details??DBNull.Value);
      await cmd.ExecuteNonQueryAsync(); }

    public async Task<List<CodingAuditEntry>> GetAuditTrailAsync(string invoiceId)
    { await using var c=new SqlConnection(_cs); await c.OpenAsync(); await using var cmd=new SqlCommand("SELECT InvoiceId,LineNumber,Action,FieldName,OldValue,NewValue,Confidence,MatchedRuleId,ActorId,ActorName,Details FROM GL_CodingAudit WHERE InvoiceId=@I ORDER BY Id",c);
      cmd.Parameters.AddWithValue("@I",invoiceId); var l=new List<CodingAuditEntry>(); await using var r=await cmd.ExecuteReaderAsync();
      while(await r.ReadAsync()) l.Add(new() { InvoiceId=r.GetString(0),LineNumber=r.GetInt32(1),Action=Enum.Parse<AuditAction>(r.GetString(2)),
          FieldName=r.IsDBNull(3)?null:r.GetString(3),OldValue=r.IsDBNull(4)?null:r.GetString(4),NewValue=r.IsDBNull(5)?null:r.GetString(5),
          Confidence=r.IsDBNull(6)?null:r.GetInt32(6),MatchedRuleId=r.IsDBNull(7)?null:r.GetString(7),ActorId=r.IsDBNull(8)?null:r.GetString(8),
          ActorName=r.IsDBNull(9)?null:r.GetString(9),Details=r.IsDBNull(10)?null:r.GetString(10) }); return l; }
}
