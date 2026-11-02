namespace Business
{
    using AnFood.Application.Bussiness.LogsBusiness;
    using AnFood.Application.Bussiness.Payments.PaymentTransactions;
    using AnFood.Application.Bussiness.KitchenBussiness;
    using AnFood.API.Handlers;
    using AnFood.Application.Bussiness.AppLogger;
    using AnFood.Application.Bussiness.AuditLogger;
    using AnFood.Application.Bussiness.ProductBussiness;
    using AnFood.Application.Bussiness.ProductGroupBussiness;
    using AnFood.Application.Bussiness.UnitsBussiness;
    using AnFood.Application.Bussiness.BranchBussiness;
    using AnFood.Application.Bussiness.EmployeeBussiness;
    using AnFood.Application.Bussiness.SystemSettingBusiness;
    using AnFood.Application.Bussiness.SystemBussiness.AccountBussiness;
    using AnFood.Application.Bussiness.MinIoProcess;
    using AnFood.Application.Bussiness.CityBussiness;
    using AnFood.Application.Bussiness.DistrictBussiness;
    using AnFood.Application.Bussiness.WardBussiness;
    using AnFood.Application.Bussiness.MenuGroupBussiness;
    using AnFood.Application.Bussiness.MenuItemBussiness;
    using AnFood.Application.Bussiness.ServicePreferenceBussiness;
    using AnFood.Application.Bussiness.TaxRateBussiness;
    using AnFood.Application.Bussiness.MaterialGroupBussiness;
    using AnFood.Application.Bussiness.MaterialBussiness;
    using AnFood.Application.Bussiness.WarehouseBussiness;
    using AnFood.Application.Bussiness.WorkShiftBussiness;
    using AnFood.Application.Bussiness.ShiftBussiness;
    using AnFood.Application.Bussiness.CashDrawerBussiness;
    using AnFood.Application.Bussiness.PreparationAreaBussiness;
    using AnFood.Application.Bussiness.ReasonBussiness;
    using AnFood.Application.Bussiness.PaymentTypeBussiness;
    using AnFood.Application.Bussiness.FloorPlanBussiness;
    using AnFood.Application.Bussiness.DiningTableBussiness;
    using AnFood.Application.Bussiness.BusinessPartnerGroupBussiness;
    using AnFood.Application.Bussiness.BusinessPartnerBussiness;
    using AnFood.Application.Bussiness.SalesBussiness;
    using AnFood.Application.Bussiness.PromotionBussiness;
    using AnFood.Application.Bussiness.ReportBussiness;
    using AnFood.Persistence;
    using AnFood.Persistence.Context;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods add project services.
    /// </summary>
    public static class ProjectServiceCollectionExtensions
    {
        public static IServiceCollection AddProjectServices(this IServiceCollection services) =>
            services
            // Core System & Logging
            .AddScoped<IDatabaseFactory, DatabaseFactory>()
            .AddScoped<ILogsHandler, LogsHandler>()
            .AddScoped<IAppLogger, AppLogger>()
            .AddScoped<IAuditLogger, AuditLogger>()
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<IReadOnlyUnitOfWork, ReadOnlyUnitOfWork>()
            .AddScoped<IMinioStorage, MinioStorage>()
            .AddScoped<ISystemSettingHandler, SystemSettingHandler>()

            // Core Catalogs
            .AddScoped<IProductHandler, ProductHandler>()
            .AddScoped<IProductGroupHandler, ProductGroupHandler>()
            .AddScoped<IUnitHandler, UnitHandler>()
            .AddScoped<IBranchHandler, BranchHandler>()
            .AddScoped<IEmployeeHandler, EmployeeHandler>()

            // Core SSO Account Integration
            .AddScoped<IAccountingHandler, AccountingHandler>()

            // Geographic location services
            .AddScoped<ICityHandler, CityHandler>()
            .AddScoped<IDistrictHandler, DistrictHandler>()
            .AddScoped<IWardHandler, WardHandler>()

            // Sales Software Catalogs (ID2)
            .AddScoped<IMenuGroupHandler, MenuGroupHandler>()
            .AddScoped<IMenuItemHandler, MenuItemHandler>()
            .AddScoped<AnFood.Application.Bussiness.MenuVisibility.MenuVisibilityService>()
            .AddScoped<IServicePreferenceHandler, ServicePreferenceHandler>()
            .AddScoped<ITaxRateHandler, TaxRateHandler>()
            .AddScoped<IMaterialGroupHandler, MaterialGroupHandler>()
            .AddScoped<IMaterialHandler, MaterialHandler>()
            .AddScoped<IWarehouseHandler, WarehouseHandler>()
            .AddScoped<IWorkShiftHandler, WorkShiftHandler>()
            .AddScoped<IShiftHandler, ShiftHandler>()
            .AddScoped<ICashDrawerHandler, CashDrawerHandler>()
            .AddScoped<IPreparationAreaHandler, PreparationAreaHandler>()
            .AddScoped<IReasonHandler, ReasonHandler>()
            .AddScoped<IPaymentTypeHandler, PaymentTypeHandler>()
            .AddScoped<IFloorPlanHandler, FloorPlanHandler>()
            .AddScoped<IDiningTableHandler, DiningTableHandler>()
            .AddScoped<IBusinessPartnerGroupHandler, BusinessPartnerGroupHandler>()
            .AddScoped<IBusinessPartnerHandler, BusinessPartnerHandler>()
            .AddScoped<ISalesHandler, SalesHandler>()
            .AddScoped<ISalesOrderService, SalesOrderService>()
            .AddScoped<PaymentTransactionHandler>()
            .AddScoped<IPaymentTransactionHandler, PaymentTransactionHandlerRealtime>()
            .AddScoped<IKitchenHandler, KitchenHandler>()
            .AddScoped<IKitchenHubSender, AnFood.API.Services.KitchenHubSender>()
            .AddScoped<IPromotionHandler, PromotionHandler>()
            .AddScoped<IReportHandler, ReportHandler>()
            .AddSingleton<AnFood.Application.Services.Mqtt.IMessageGatewayService, AnFood.Application.Services.Mqtt.MessageGatewayService>()
            .AddSingleton<AnFood.Application.Services.Mqtt.IMqttSpeakerService, AnFood.Application.Services.Mqtt.MqttSpeakerService>()
            ;
    }
}
