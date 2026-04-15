-- FinOps GL Coding Engine — Schema & Seed Data (dbo.GL_* tables, no separate schema)

IF OBJECT_ID('dbo.GL_ChartOfAccounts','U') IS NULL
CREATE TABLE dbo.GL_ChartOfAccounts (Id INT IDENTITY(1,1) PRIMARY KEY, GLCode NVARCHAR(20) NOT NULL UNIQUE, GLDescription NVARCHAR(200) NOT NULL, AccountType NVARCHAR(20) NOT NULL, ParentGLCode NVARCHAR(20) NULL, IsActive BIT NOT NULL DEFAULT 1, TenantId INT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
IF OBJECT_ID('dbo.GL_CostCenters','U') IS NULL
CREATE TABLE dbo.GL_CostCenters (Id INT IDENTITY(1,1) PRIMARY KEY, CostCenterCode NVARCHAR(20) NOT NULL UNIQUE, CostCenterName NVARCHAR(100) NOT NULL, ParentCode NVARCHAR(20) NULL, ManagerName NVARCHAR(100) NULL, IsActive BIT NOT NULL DEFAULT 1, TenantId INT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
IF OBJECT_ID('dbo.GL_TaxCodes','U') IS NULL
CREATE TABLE dbo.GL_TaxCodes (Id INT IDENTITY(1,1) PRIMARY KEY, TaxCode NVARCHAR(20) NOT NULL UNIQUE, TaxType NVARCHAR(20) NOT NULL, TaxRate DECIMAL(5,2) NOT NULL, TaxGLAccount NVARCHAR(20) NOT NULL, Country NVARCHAR(10) NOT NULL DEFAULT 'IN', Description NVARCHAR(200) NULL, IsActive BIT NOT NULL DEFAULT 1, TenantId INT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
IF OBJECT_ID('dbo.GL_Locations','U') IS NULL
CREATE TABLE dbo.GL_Locations (Id INT IDENTITY(1,1) PRIMARY KEY, LocationCode NVARCHAR(20) NOT NULL UNIQUE, LocationName NVARCHAR(100) NOT NULL, City NVARCHAR(50) NOT NULL, State NVARCHAR(50) NULL, Country NVARCHAR(10) NOT NULL DEFAULT 'IN', IsActive BIT NOT NULL DEFAULT 1, TenantId INT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
IF OBJECT_ID('dbo.GL_CompanyCodes','U') IS NULL
CREATE TABLE dbo.GL_CompanyCodes (Id INT IDENTITY(1,1) PRIMARY KEY, CompanyCode NVARCHAR(20) NOT NULL UNIQUE, EntityName NVARCHAR(200) NOT NULL, Country NVARCHAR(10) NOT NULL, Currency NVARCHAR(3) NOT NULL, TaxRegNumber NVARCHAR(50) NULL, IsActive BIT NOT NULL DEFAULT 1, TenantId INT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
IF OBJECT_ID('dbo.GL_ProductCategories','U') IS NULL
CREATE TABLE dbo.GL_ProductCategories (Id INT IDENTITY(1,1) PRIMARY KEY, CategoryCode NVARCHAR(20) NOT NULL UNIQUE, CategoryName NVARCHAR(100) NOT NULL, Keywords NVARCHAR(500) NOT NULL, DefaultGLCode NVARCHAR(20) NULL, IsActive BIT NOT NULL DEFAULT 1, TenantId INT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
IF OBJECT_ID('dbo.GL_VendorMappings','U') IS NULL
CREATE TABLE dbo.GL_VendorMappings (Id INT IDENTITY(1,1) PRIMARY KEY, RuleId NVARCHAR(20) NOT NULL UNIQUE, VendorName NVARCHAR(200) NOT NULL, VendorCode NVARCHAR(50) NULL, DescKeyword NVARCHAR(100) NOT NULL, GLCode NVARCHAR(20) NOT NULL, CostCenterCode NVARCHAR(20) NULL, CategoryCode NVARCHAR(20) NULL, BusinessUnit NVARCHAR(50) NULL, Priority INT NOT NULL DEFAULT 100, IsActive BIT NOT NULL DEFAULT 1, TenantId INT NOT NULL DEFAULT 1, EffectiveFrom DATE NOT NULL DEFAULT '2024-01-01', EffectiveTo DATE NULL, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO
IF OBJECT_ID('dbo.GL_CodingLines','U') IS NULL
CREATE TABLE dbo.GL_CodingLines (Id BIGINT IDENTITY(1,1) PRIMARY KEY, InvoiceId NVARCHAR(50) NOT NULL, LineNumber INT NOT NULL, ModuleCode NVARCHAR(10) NOT NULL, CodingMode NVARCHAR(10) NOT NULL, VendorName NVARCHAR(200) NULL, VendorCode NVARCHAR(50) NULL, LineDescription NVARCHAR(500) NULL, LineAmount DECIMAL(18,2) NOT NULL, Currency NVARCHAR(3) NOT NULL DEFAULT 'INR', TaxType NVARCHAR(20) NULL, TaxRate DECIMAL(5,2) NULL, BillToLocation NVARCHAR(100) NULL, BillingEntity NVARCHAR(200) NULL, PONumber NVARCHAR(50) NULL, POLineNumber INT NULL, AI_GLCode NVARCHAR(20) NULL, AI_CostCenterCode NVARCHAR(20) NULL, AI_TaxCode NVARCHAR(20) NULL, AI_LocationCode NVARCHAR(20) NULL, AI_CompanyCode NVARCHAR(20) NULL, AI_CategoryCode NVARCHAR(20) NULL, AI_Confidence INT NULL, AI_MatchedRuleId NVARCHAR(20) NULL, AI_SuggestedAt DATETIME2 NULL, Final_GLCode NVARCHAR(20) NULL, Final_CostCenterCode NVARCHAR(20) NULL, Final_TaxCode NVARCHAR(20) NULL, Final_LocationCode NVARCHAR(20) NULL, Final_CompanyCode NVARCHAR(20) NULL, Final_CategoryCode NVARCHAR(20) NULL, CodingStatus NVARCHAR(20) NOT NULL DEFAULT 'PENDING', ModifiedBy NVARCHAR(100) NULL, ModifiedAt DATETIME2 NULL, PostedAt DATETIME2 NULL, TenantId INT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT UQ_GL_CodingLines UNIQUE (InvoiceId, LineNumber));
GO
IF OBJECT_ID('dbo.GL_CodingAudit','U') IS NULL
CREATE TABLE dbo.GL_CodingAudit (Id BIGINT IDENTITY(1,1) PRIMARY KEY, InvoiceId NVARCHAR(50) NOT NULL, LineNumber INT NOT NULL, Action NVARCHAR(30) NOT NULL, FieldName NVARCHAR(30) NULL, OldValue NVARCHAR(100) NULL, NewValue NVARCHAR(100) NULL, Confidence INT NULL, MatchedRuleId NVARCHAR(20) NULL, ActorId NVARCHAR(100) NULL, ActorName NVARCHAR(200) NULL, Details NVARCHAR(MAX) NULL, TenantId INT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME());
GO

-- Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GL_CodingLines_Inv') CREATE INDEX IX_GL_CodingLines_Inv ON dbo.GL_CodingLines(InvoiceId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GL_CodingLines_Status') CREATE INDEX IX_GL_CodingLines_Status ON dbo.GL_CodingLines(CodingStatus);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_GL_CodingAudit_Inv') CREATE INDEX IX_GL_CodingAudit_Inv ON dbo.GL_CodingAudit(InvoiceId, LineNumber);
GO

-- Clear transaction tables for fresh demo
DELETE FROM dbo.GL_CodingAudit; DELETE FROM dbo.GL_CodingLines;
GO

-- Seed data
IF NOT EXISTS (SELECT 1 FROM dbo.GL_ChartOfAccounts) INSERT INTO dbo.GL_ChartOfAccounts (GLCode,GLDescription,AccountType) VALUES
('410001','Product Sales Revenue','Revenue'),('410002','Service Revenue','Revenue'),('510001','Cost of Goods Sold','Expense'),('610001','Office Rent','Expense'),('610002','Warehouse Rent','Expense'),('620010','Consulting Expense','Expense'),('620020','Legal & Professional Fees','Expense'),('620030','Audit Fees','Expense'),('630001','Marketing Expense','Expense'),('630002','Advertising Expense','Expense'),('640001','IT Infrastructure','Expense'),('640002','Software Licenses','Expense'),('650001','Travel & Conveyance','Expense'),('650002','Employee Training','Expense'),('660001','Office Supplies','Expense'),('210001','GST Input Tax Credit','Asset'),('210002','GST Payable','Liability'),('210003','VAT Payable','Liability'),('220001','Accounts Payable Control','Liability');

IF NOT EXISTS (SELECT 1 FROM dbo.GL_CostCenters) INSERT INTO dbo.GL_CostCenters (CostCenterCode,CostCenterName,ManagerName) VALUES
('CC100','Finance','Rajesh Kumar'),('CC200','Marketing','Priya Sharma'),('CC300','Administration','Amit Desai'),('CC400','IT','Sneha Patel'),('CC500','Human Resources','Vikram Singh'),('CC600','Operations','Neha Gupta'),('CC700','Sales','Rohit Mehta'),('CC800','Legal','Anita Joshi'),('CC900','Procurement','Suresh Iyer');

IF NOT EXISTS (SELECT 1 FROM dbo.GL_TaxCodes) INSERT INTO dbo.GL_TaxCodes (TaxCode,TaxType,TaxRate,TaxGLAccount,Country,Description) VALUES
('GST0','GST',0.00,'210001','IN','GST Exempt'),('GST5','GST',5.00,'210001','IN','GST 5%'),('GST12','GST',12.00,'210001','IN','GST 12%'),('GST18','GST',18.00,'210001','IN','GST 18%'),('GST28','GST',28.00,'210001','IN','GST 28%'),('IGST18','GST',18.00,'210001','IN','IGST 18%'),('VAT20','VAT',20.00,'210003','UK','UK VAT Standard'),('EXEMPT','EXEMPT',0.00,'210001','IN','Tax Exempt');

IF NOT EXISTS (SELECT 1 FROM dbo.GL_Locations) INSERT INTO dbo.GL_Locations (LocationCode,LocationName,City,State,Country) VALUES
('LOC01','Mumbai HQ','Mumbai','Maharashtra','IN'),('LOC02','Bangalore Tech Hub','Bangalore','Karnataka','IN'),('LOC03','Pune Office','Pune','Maharashtra','IN'),('LOC04','Delhi NCR','Gurugram','Haryana','IN'),('LOC05','Chennai Center','Chennai','Tamil Nadu','IN'),('LOC06','London Office','London',NULL,'UK');

IF NOT EXISTS (SELECT 1 FROM dbo.GL_CompanyCodes) INSERT INTO dbo.GL_CompanyCodes (CompanyCode,EntityName,Country,Currency) VALUES
('C001','FAO India Pvt Ltd','IN','INR'),('C002','FAO US Inc','US','USD'),('C003','FAO UK Ltd','UK','GBP');

IF NOT EXISTS (SELECT 1 FROM dbo.GL_ProductCategories) INSERT INTO dbo.GL_ProductCategories (CategoryCode,CategoryName,Keywords,DefaultGLCode) VALUES
('CAT01','Rent & Lease','rent,lease,tenancy,premises','610001'),('CAT02','Legal Services','legal,advisory,counsel,litigation','620020'),('CAT03','Consulting Services','consulting,advisory,strategy,engagement','620010'),('CAT04','Marketing Expense','campaign,promotion,branding,media','630001'),('CAT05','IT Services','software,hardware,cloud,hosting,saas','640001'),('CAT06','Audit & Assurance','audit,assurance,statutory,compliance','620030'),('CAT07','Travel','travel,flight,hotel,cab,transport','650001'),('CAT08','Training','training,workshop,certification,course','650002'),('CAT09','Office Supplies','stationery,supplies,printing,courier','660001'),('CAT10','Advertising','advertising,ads,digital,social media','630002');

IF NOT EXISTS (SELECT 1 FROM dbo.GL_VendorMappings) INSERT INTO dbo.GL_VendorMappings (RuleId,VendorName,VendorCode,DescKeyword,GLCode,CostCenterCode,CategoryCode,BusinessUnit,Priority) VALUES
('VGL001','ABC Realty Pvt Ltd','V1001','Rent','610001','CC300','CAT01','Admin',10),('VGL002','ABC Realty Pvt Ltd','V1001','Maintenance','640001','CC300','CAT05','Admin',20),('VGL003','XYZ Legal Associates','V2001','Legal','620020','CC800','CAT02','Legal',10),('VGL004','XYZ Legal Associates','V2001','Advisory','620020','CC800','CAT02','Legal',20),('VGL005','XYZ Legal Associates','V2001','Compliance','620030','CC800','CAT06','Legal',30),('VGL006','Dell Technologies','V3001','Consulting','620010','CC400','CAT03','IT',10),('VGL007','Dell Technologies','V3001','Hardware','640001','CC400','CAT05','IT',20),('VGL008','Dell Technologies','V3001','Software','640002','CC400','CAT05','IT',30),('VGL009','WPP Group','V4001','Campaign','630001','CC200','CAT04','Marketing',10),('VGL010','WPP Group','V4001','Advertising','630002','CC200','CAT10','Marketing',20),('VGL011','Deloitte India','V5001','Audit','620030','CC100','CAT06','Finance',10),('VGL012','Deloitte India','V5001','Tax','620020','CC100','CAT02','Finance',20),('VGL013','Deloitte India','V5001','Consulting','620010','CC100','CAT03','Finance',30),('VGL014','MakeMyTrip Business','V7001','Flight','650001','CC500','CAT07','HR',10),('VGL015','MakeMyTrip Business','V7001','Hotel','650001','CC500','CAT07','HR',20),('VGL016','Staples India','V8001','Stationery','660001','CC300','CAT09','Admin',10);
GO

PRINT 'GL Coding Engine: 9 GL_* tables created, seed data loaded.';
GO
