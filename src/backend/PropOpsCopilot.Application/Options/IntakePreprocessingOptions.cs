namespace PropOpsCopilot.Application.Options;

public sealed class IntakePreprocessingOptions
{
    public const string SectionName = "IntakePreprocessing";

    public string TimeZoneId { get; set; } = "Australia/Sydney";

    public int BusinessHoursStartHour { get; set; } = 8;

    public int BusinessHoursEndHour { get; set; } = 18;
}
