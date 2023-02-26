using System.ComponentModel;
using Spectre.Console.Cli;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

namespace hhreg.business;

public sealed class EntryOverrideCommand : Command<EntryOverrideCommand.Settings>
{
    private readonly ITimeRepository _timeRepository;
    private readonly ILogger _logger;

    public EntryOverrideCommand(ITimeRepository timeRepository, ILogger logger)
    {
        _timeRepository = timeRepository;
        _logger = logger;
    }

    public sealed class Settings : CommandSettings 
    {
        [Description("Sets entry day as today")]
        [CommandOption("-t|--today")]
        public bool IsToday { get; init; }

        [Description("Sets day type (Work,Weekend,Sick,Holiday,Vacation)")]
        [CommandOption("-y|--day-type")]
        [DefaultValue(DayType.Work)]
        public DayType DayType { get; init; }

        [Description("Defines a justification")]
        [CommandOption("-j|--justification")]
        public string? Justification { get; init; }

        [Description("Defines the day")]
        [CommandOption("-d|--day")]
        public string? Day { get; init; }

        [Description("Defines time entries (format: HH:mm)")]
        [CommandArgument(0, "[entries]")]
        public string[] Entries { get; init; } = new string[]{};


        public override ValidationResult Validate()
        {
            if (!IsToday && Day == null) 
            {
                return ValidationResult.Error("You should inform a day to log (or set entry as today with -t).");
            }

            if (!IsToday && !DateOnly.TryParse(Day, out var _)) 
            {
                return ValidationResult.Error($"Could not parse '{Day}' as a valid date format.");
            }
                
            if (Entries.Length == 0 && Justification == null) 
            {
                return ValidationResult.Error("You should inform at least one time entry or set a justification with -j.");
            }

            foreach(var entry in Entries) 
            {
                if (TimeSpan.TryParse(entry, out var time)) 
                {
                    if (time < TimeSpan.Zero) 
                    {
                        return ValidationResult.Error("Entry times must be positives.");
                    }
                } 
                else 
                {
                    return ValidationResult.Error($"Could not parse '{entry}' as a valid time format.");
                }
            }
            
            return ValidationResult.Success();
        }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var inputDay = settings.IsToday 
            ? DateOnly.FromDateTime(DateTime.Today) 
            : DateOnly.Parse(settings.Day!);

        var dayEntry = _timeRepository.GetDayEntry(inputDay.ToString("yyyy-MM-dd"));
        if (dayEntry == null) throw new HhregException($"Cannot override a not yet created day '{inputDay.ToString("yyyy-MM-dd")}'");

        _timeRepository.OverrideDayEntry(dayEntry.Id, settings.Justification, settings.DayType, 
            settings.Entries);
        
        var dayText = settings.DayType == DayType.Work ? string.Join(" / ", settings.Entries) : settings.Justification;
        _logger.WriteLine($@"Day entry [green]SUCCESSFULLY[/] overridden!");
        _logger.WriteLine($"[yellow]{inputDay}[/]: {dayText}");
        return 0;
    }
}