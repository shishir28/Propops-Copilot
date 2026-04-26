namespace PropOpsCopilot.Infrastructure.Identity;

public static class PortalRoles
{
    public const string PropertyManager = "PropertyManager";
    public const string Dispatcher = "Dispatcher";
    public const string Tenant = "Tenant";
    public const string PropertyOwner = "PropertyOwner";
    public const string Vendor = "Vendor";

    public static readonly string[] Staff = [PropertyManager, Dispatcher];
    public static readonly string[] RequestCreators = [PropertyManager, Dispatcher, Tenant, PropertyOwner];
    public static readonly string[] All = [PropertyManager, Dispatcher, Tenant, PropertyOwner, Vendor];
}
