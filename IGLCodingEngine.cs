using FinOps.GLCodingEngine.Core.Models;

namespace FinOps.GLCodingEngine.Core.Interfaces;

public interface IGLCodingEngine
{
    Task<Dictionary<string, object>> SuggestAsync(GLCodingRequest request);
    Task<Dictionary<string, object>> InheritFromPOAsync(GLCodingRequest request);
    Task<Dictionary<string, object>> ApplyAsync(ApplyCodingRequest request);
    Task<Dictionary<string, object>> ValidateAsync(string invoiceId);
    Task<Dictionary<string, object>> BulkSuggestAsync(BulkCodingRequest request);
    Task<Dictionary<string, object>> BulkApplyAsync(List<ApplyCodingRequest> requests);
    Task<Dictionary<string, object>> GetCodingStatusAsync(string invoiceId);
    Task<Dictionary<string, object>> GetAuditTrailAsync(string invoiceId);
    Task<Dictionary<string, object>> GetMasterDataAsync(string masterType);
}

public interface IGLCodingRepository
{
    Task<List<VendorGLMapping>> GetVendorMappingsAsync(string? vendorCode, string? vendorName);
    Task<List<CategoryRecord>> GetCategoriesAsync();
    Task<GLAccount?> GetGLAccountAsync(string glCode);
    Task<CostCenter?> GetCostCenterAsync(string code);
    Task<TaxCodeRecord?> FindTaxCodeAsync(string taxType, decimal taxRate);
    Task<LocationRecord?> FindLocationAsync(string locationText);
    Task<CompanyCodeRecord?> FindCompanyCodeAsync(string entityName);
    Task<CategoryRecord?> FindCategoryByKeywordAsync(string keyword);
    Task<List<GLAccount>> GetAllGLAccountsAsync();
    Task<List<CostCenter>> GetAllCostCentersAsync();
    Task<List<TaxCodeRecord>> GetAllTaxCodesAsync();
    Task<List<LocationRecord>> GetAllLocationsAsync();
    Task<List<CompanyCodeRecord>> GetAllCompanyCodesAsync();
    Task<List<CategoryRecord>> GetAllCategoriesAsync();
    Task UpsertCodingLineAsync(GLCodingRequest request, GLCodingSuggestion suggestion);
    Task UpdateFinalCodingAsync(ApplyCodingRequest request);
    Task<List<GLCodingSuggestion>> GetCodingLinesAsync(string invoiceId);
    Task UpdateCodingStatusAsync(string invoiceId, int lineNumber, string status);
    Task InsertAuditAsync(CodingAuditEntry entry);
    Task<List<CodingAuditEntry>> GetAuditTrailAsync(string invoiceId);
}
