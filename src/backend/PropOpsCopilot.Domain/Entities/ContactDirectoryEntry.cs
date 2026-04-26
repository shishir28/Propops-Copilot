namespace PropOpsCopilot.Domain.Entities;

public sealed class ContactDirectoryEntry
{
    private ContactDirectoryEntry()
    {
    }

    public Guid Id { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string EmailAddress { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;

    public string PropertyName { get; private set; } = string.Empty;

    public string UnitNumber { get; private set; } = string.Empty;

    public string TenantName { get; private set; } = string.Empty;

    public static ContactDirectoryEntry Create(
        string fullName,
        string emailAddress,
        string phoneNumber,
        string propertyName,
        string unitNumber,
        string tenantName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name is required.", nameof(propertyName));
        }

        if (string.IsNullOrWhiteSpace(emailAddress) && string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Either an email address or phone number is required.");
        }

        return new ContactDirectoryEntry
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            EmailAddress = emailAddress.Trim().ToLowerInvariant(),
            PhoneNumber = NormalizePhoneNumber(phoneNumber),
            PropertyName = propertyName.Trim(),
            UnitNumber = unitNumber.Trim(),
            TenantName = string.IsNullOrWhiteSpace(tenantName) ? fullName.Trim() : tenantName.Trim()
        };
    }

    private static string NormalizePhoneNumber(string phoneNumber) =>
        new(phoneNumber.Where(char.IsDigit).ToArray());
}
