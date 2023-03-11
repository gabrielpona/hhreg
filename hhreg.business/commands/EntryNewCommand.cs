using System.ComponentModel;
using Spectre.Console.Cli;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

namespace hhreg.business;

public sealed class EntryNewCommand : Command<EntryNewCommand.Settings>
{
    private readonly ITimeRepository _timeRepository;
    private readonly ILogger _logger;

    public EntryNewCommand(ITimeRepository timeRepository, ILogger logger)
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
        public string[] Entries { get; init; } = Array.Empty<string>();


        public override ValidationResult Validate()
        {
            if (!IsToday && Day == null) 
            {
                return ValidationResult.Error(HhregMessages.YouShouldInformADayToLog);
            }

            if (!IsToday && !DateOnly.TryParse(Day, out var _)) 
            {
                return ValidationResult.Error(string.Format(HhregMessages.CouldNotParseAsAValidDateFormat, Day));
            }
                
            if (Entries.Length == 0 && Justification == null) 
            {
                return ValidationResult.Error(HhregMessages.YouShouldInformAtLeastOneTimeEntryOrSetAJustificative);
            }

            foreach(var entry in Entries) 
            {
                if (TimeSpan.TryParse(entry, out var time)) 
                {
                    if (time < TimeSpan.Zero) 
                    {
                        return ValidationResult.Error(HhregMessages.EntryTimesMustBePositive);
                    }
                } 
                else 
                {
                    return ValidationResult.Error(string.Format(HhregMessages.CouldNotParseAsAValidTimeFormat, entry));
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

        var dayEntry = _timeRepository.GetOrCreateDay(inputDay.ToString("yyyy-MM-dd"), 
            settings.Justification, settings.DayType);

        _timeRepository.CreateTime(dayEntry.Id, settings.Entries);
        
        var dayText = settings.DayType == DayType.Work ? string.Join(" / ", settings.Entries) : settings.Justification;
        _logger.WriteLine($@"Day entry [green]SUCCESSFULLY[/] created!");
        _logger.WriteLine($"[yellow]{inputDay}[/]: {dayText}");
        return 0;
    }
}