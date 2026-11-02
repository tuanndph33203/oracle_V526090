using AnFood.Domain.Datatables;
using AnFood.Domain.Datatables.LogsSystem;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AnFood.Persistence.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        protected DataContext(DbContextOptions options) : base(options) { }

        // Product Catalog Core
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductGroup> ProductGroups { get; set; }
        public virtual DbSet<Unit> Units { get; set; }
        public virtual DbSet<Branchs> Branchs { get; set; }
        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<Cities> Cities { get; set; }
        public virtual DbSet<Districts> Districts { get; set; }
        public virtual DbSet<Wards> Wards { get; set; }

        // Logs and Settings
        public virtual DbSet<SystemAuditLog> SystemAuditLogs { get; set; }
        public virtual DbSet<SystemAppLog> SystemAppLogs { get; set; }
        public virtual DbSet<SystemSetting> SystemSettings { get; set; }

        // New Sales Catalog DbSets
        public virtual DbSet<MenuGroup> MenuGroups { get; set; }
        public virtual DbSet<MenuItem> MenuItems { get; set; }
        public virtual DbSet<MenuVisibilityRule> MenuVisibilityRules { get; set; }
        public virtual DbSet<MenuItemGroup> MenuItemGroups { get; set; }
        public virtual DbSet<ServicePreference> ServicePreferences { get; set; }
        public virtual DbSet<MenuItemServicePreference> MenuItemServicePreferences { get; set; }
        public virtual DbSet<MenuItemBranch> MenuItemBranches { get; set; }
        public virtual DbSet<TaxRate> TaxRates { get; set; }
        public virtual DbSet<MaterialGroup> MaterialGroups { get; set; }
        public virtual DbSet<Material> Materials { get; set; }
        public virtual DbSet<MaterialBranch> MaterialBranches { get; set; }
        public virtual DbSet<Warehouse> Warehouses { get; set; }
        public virtual DbSet<WarehouseBranch> WarehouseBranches { get; set; }
        public virtual DbSet<WorkShift> WorkShifts { get; set; }
        public virtual DbSet<WorkShiftBranch> WorkShiftBranches { get; set; }
        public virtual DbSet<Shift> Shifts { get; set; }
        public virtual DbSet<ShiftCashCount> ShiftCashCounts { get; set; }
        public virtual DbSet<ShiftExtensionHistory> ShiftExtensionHistories { get; set; }
        public virtual DbSet<CashDrawer> CashDrawers { get; set; }
        public virtual DbSet<CashDrawerBranch> CashDrawerBranches { get; set; }
        public virtual DbSet<PreparationArea> PreparationAreas { get; set; }
        public virtual DbSet<PreparationAreaBranch> PreparationAreaBranches { get; set; }
        public virtual DbSet<Reason> Reasons { get; set; }
        public virtual DbSet<PaymentType> PaymentTypes { get; set; }
        public virtual DbSet<FloorPlan> FloorPlans { get; set; }
        public virtual DbSet<DiningTable> DiningTables { get; set; }
        public virtual DbSet<BusinessPartner> BusinessPartners { get; set; }
        public virtual DbSet<SalesOrder> SalesOrders { get; set; }
        public virtual DbSet<SalesOrderTable> SalesOrderTables { get; set; }
        public virtual DbSet<SalesOrderItem> SalesOrderItems { get; set; }
        public virtual DbSet<SalesOrderPayment> SalesOrderPayments { get; set; }
        public virtual DbSet<BusinessPartnerGroup> BusinessPartnerGroups { get; set; }
        public virtual DbSet<PaymentHistories> PaymentHistories { get; set; }
        public virtual DbSet<PaymentNotification> PaymentNotifications { get; set; }
        public virtual DbSet<KitchenRequest> KitchenRequests { get; set; }
        public virtual DbSet<KitchenRequestDetail> KitchenRequestDetails { get; set; }
        public virtual DbSet<TableQrOrderToken> TableQrOrderTokens { get; set; }
        public virtual DbSet<BranchQrOrderToken> BranchQrOrderTokens { get; set; }
        public virtual DbSet<QrStaffCall> QrStaffCalls { get; set; }
        public virtual DbSet<PrintAgent> PrintAgents { get; set; }
        public virtual DbSet<PrintAgentPrinter> PrintAgentPrinters { get; set; }
        public virtual DbSet<PrintAreaMapping> PrintAreaMappings { get; set; }

        // E-Invoice Module
        public virtual DbSet<BranchConfigs> BranchConfigs { get; set; }
        public virtual DbSet<InvoiceRules> InvoiceRules { get; set; }
        public virtual DbSet<InvoiceRuleBranches> InvoiceRuleBranches { get; set; }
        public virtual DbSet<InvoiceRuleDetails> InvoiceRuleDetails { get; set; }
        public virtual DbSet<Invoices> Invoices { get; set; }
        public virtual DbSet<InvoiceDetails> InvoiceDetails { get; set; }

        // Promotions
        public virtual DbSet<Promotion> Promotions { get; set; }
        public virtual DbSet<PromotionBranch> PromotionBranches { get; set; }
        public virtual DbSet<PromotionCustomerGroup> PromotionCustomerGroups { get; set; }
        public virtual DbSet<PromotionWeekDay> PromotionWeekDays { get; set; }
        public virtual DbSet<PromotionTimeRange> PromotionTimeRanges { get; set; }
        public virtual DbSet<PromotionDiscountConfig> PromotionDiscountConfigs { get; set; }
        public virtual DbSet<PromotionDiscountItem> PromotionDiscountItems { get; set; }
        public virtual DbSet<PromotionGiftCondition> PromotionGiftConditions { get; set; }
        public virtual DbSet<PromotionGiftItem> PromotionGiftItems { get; set; }

        public string Prefix { get; set; } = string.Empty;
        public string DatabaseType { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public bool IsTesting { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<MenuVisibilityRule>().HasIndex(x => new { x.BranchCode, x.IsEnabled });

            // Unique indexes for new entities
            modelBuilder.Entity<MenuGroup>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<MenuItem>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<MenuItemGroup>().HasIndex(x => new { x.MenuItemId, x.MenuGroupId }).IsUnique();
            modelBuilder.Entity<MenuItemGroup>().HasIndex(x => x.MenuGroupId);
            modelBuilder.Entity<ServicePreference>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<MenuItemServicePreference>().HasIndex(x => new { x.MenuItemId, x.ServicePreferenceId }).IsUnique();
            modelBuilder.Entity<MenuItemBranch>().HasIndex(x => new { x.MenuItemId, x.BranchId }).IsUnique();
            modelBuilder.Entity<TaxRate>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<MaterialGroup>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Material>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<MaterialBranch>().HasIndex(x => new { x.MaterialId, x.BranchId }).IsUnique();
            modelBuilder.Entity<Warehouse>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<WarehouseBranch>().HasIndex(x => new { x.WarehouseId, x.BranchId }).IsUnique();
            modelBuilder.Entity<WorkShift>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<WorkShiftBranch>().HasIndex(x => new { x.WorkShiftId, x.BranchId }).IsUnique();
            modelBuilder.Entity<Shift>().HasIndex(x => new { x.BranchCode, x.CashierId, x.Status });
            modelBuilder.Entity<ShiftCashCount>().HasIndex(x => x.ShiftId);
            modelBuilder.Entity<ShiftExtensionHistory>().HasIndex(x => x.ShiftId);
            modelBuilder.Entity<CashDrawer>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<CashDrawerBranch>().HasIndex(x => new { x.CashDrawerId, x.BranchId }).IsUnique();
            modelBuilder.Entity<PreparationArea>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<PreparationAreaBranch>().HasIndex(x => new { x.PreparationAreaId, x.BranchId }).IsUnique();
            modelBuilder.Entity<Reason>().HasIndex(x => new { x.Type, x.Code }).IsUnique();
            modelBuilder.Entity<PaymentType>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<FloorPlan>().HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
            modelBuilder.Entity<DiningTable>().HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
            modelBuilder.Entity<SalesOrder>().HasIndex(x => new { x.BranchCode, x.OrderCode }).IsUnique();
            modelBuilder.Entity<SalesOrderTable>().HasIndex(x => new { x.BranchCode, x.OrderId, x.TableId }).IsUnique();
            modelBuilder.Entity<SalesOrderItem>().HasIndex(x => new { x.BranchCode, x.OrderId, x.LineNo }).IsUnique();
            modelBuilder.Entity<KitchenRequest>().HasIndex(x => new { x.BranchCode, x.RequestCode }).IsUnique();
            modelBuilder.Entity<KitchenRequestDetail>().HasIndex(x => new { x.KitchenRequestId, x.OrderDetailId }).IsUnique();
            modelBuilder.Entity<TableQrOrderToken>().HasIndex(x => x.Token).IsUnique();
            modelBuilder.Entity<BranchQrOrderToken>().HasIndex(x => x.Token).IsUnique();
            modelBuilder.Entity<BranchQrOrderToken>().HasIndex(x => new { x.BranchId, x.IsActive });
            modelBuilder.Entity<TableQrOrderToken>().HasIndex(x => new { x.TableId, x.IsActive });
            modelBuilder.Entity<QrStaffCall>().HasIndex(x => new { x.BranchCode, x.CreatedAt });
            modelBuilder.Entity<PrintAgent>().HasIndex(x => x.BranchId);
            modelBuilder.Entity<PrintAgentPrinter>().HasIndex(x => new { x.AgentId, x.PrinterName }).IsUnique();
            modelBuilder.Entity<PrintAreaMapping>().HasIndex(x => new { x.BranchId, x.PreparationAreaId }).IsUnique();

            // E-Invoice Module Configs
            modelBuilder.Entity<InvoiceRuleBranches>().HasKey(x => new { x.RuleCode, x.BranchCode });
            modelBuilder.Entity<Invoices>().HasIndex(x => x.OrderId).IsUnique();
            modelBuilder.Entity<InvoiceDetails>().HasIndex(x => x.InvoiceId);

            // Promotions Configurations
            modelBuilder.Entity<Promotion>().HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<PromotionBranch>().HasIndex(x => new { x.PromotionId, x.BranchId }).IsUnique();
            modelBuilder.Entity<PromotionCustomerGroup>().HasIndex(x => new { x.PromotionId, x.CustomerGroupId }).IsUnique();
            modelBuilder.Entity<PromotionWeekDay>().HasIndex(x => new { x.PromotionId, x.DayOfWeek }).IsUnique();
            modelBuilder.Entity<PromotionTimeRange>().HasIndex(x => new { x.PromotionId, x.StartTime, x.EndTime });

            modelBuilder.Entity<Employee>().HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL AND [Status] != 0");
            modelBuilder.Entity<BusinessPartner>().HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL AND [Status] != 0");
            modelBuilder.Entity<BusinessPartner>().HasIndex(x => x.IdentityNumber).IsUnique().HasFilter("[IdentityNumber] IS NOT NULL AND [Status] != 0");
            modelBuilder.Entity<BusinessPartner>().HasIndex(x => x.TaxCode).IsUnique().HasFilter("[TaxCode] IS NOT NULL AND [Status] != 0");
            modelBuilder.Entity<BusinessPartnerGroup>().HasIndex(x => x.Code).IsUnique().HasFilter("[Status] != 0");
            modelBuilder.Entity<PaymentNotification>().HasIndex(x => x.BranchCode);

            // Restrict cascade delete by default
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        public override int SaveChanges()
        {
            NormalizeDocumentDateOnly();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            NormalizeDocumentDateOnly();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeDocumentDateOnly();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            NormalizeDocumentDateOnly();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void NormalizeDocumentDateOnly()
        {
            var trackedEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in trackedEntries)
            {
                foreach (var property in entry.Properties.Where(p => p.Metadata.Name == "DocumentDate"))
                {
                    if (property.CurrentValue is DateTime dt)
                    {
                        property.CurrentValue = dt.Date;
                    }
                }
            }
        }
    }
}
