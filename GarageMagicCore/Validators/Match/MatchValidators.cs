using FluentValidation;
using GarageMagicCore.DTOs.Match;
using GarageMagicCore.Models;

namespace GarageMagicCore.Validators.Match;

public class CreateMatchDtoValidator : AbstractValidator<CreateMatchDto>
{
    public CreateMatchDtoValidator()
    {
        RuleFor(x => x.MatchType)
            .IsInEnum()
            .WithMessage("Invalid match type");
        
        RuleFor(x => x.MatchDate)
            .NotEmpty().WithMessage("Match date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Match date cannot be in the future");
        
        RuleFor(x => x.Participants)
            .NotEmpty().WithMessage("At least one participant is required")
            .Must((dto, participants) => ValidateParticipantCount(dto.MatchType, participants.Count))
            .WithMessage("Invalid number of participants for the match type");
        
        RuleFor(x => x.WinnerUserIds)
            .NotEmpty().WithMessage("At least one winner is required")
            .Must((dto, winnerIds) => winnerIds.All(id => dto.Participants.Any(p => p.UserId == id)))
            .WithMessage("All winners must be participants in the match");
        
        RuleFor(x => x.Participants)
            .Must(p => p.Select(x => x.UserId).Distinct().Count() == p.Count)
            .WithMessage("Duplicate participants are not allowed");
        
        // Sheriff mode: exactly 1 Sheriff
        RuleFor(x => x.Participants)
            .Must(p => p.Count(x => x.HiddenRole == HiddenRole.Sheriff) == 1)
            .WithMessage("Sheriff mode requires exactly one Sheriff")
            .When(IsSheriffMode);
        
        // Sheriff mode: exactly 1 Deputy
        RuleFor(x => x.Participants)
            .Must(p => p.Count(x => x.HiddenRole == HiddenRole.Deputy) == 1)
            .WithMessage("Sheriff mode requires exactly one Deputy")
            .When(IsSheriffMode);
        
        // Sheriff mode: exactly 2 Outlaws
        RuleFor(x => x.Participants)
            .Must(p => p.Count(x => x.HiddenRole == HiddenRole.Outlaw) == 2)
            .WithMessage("Sheriff mode requires exactly two Outlaws")
            .When(IsSheriffMode);
        
        // 6-player: exactly 1 Renegade
        RuleFor(x => x.Participants)
            .Must(p => p.Count(x => x.HiddenRole == HiddenRole.Renegade) == 1)
            .WithMessage("Six-player Sheriff mode requires exactly one Renegade")
            .When(x => x.MatchType == Models.MatchType.SixPlayerSheriff);
        
        // 5-player: no Renegades
        RuleFor(x => x.Participants)
            .Must(p => p.All(x => x.HiddenRole != HiddenRole.Renegade))
            .WithMessage("Renegade role is only available in six-player Sheriff mode")
            .When(x => x.MatchType == Models.MatchType.FivePlayerSheriff);
        
        // Matriarch: if set, must be an Outlaw participant
        RuleFor(x => x.MatriarchUserId)
            .Must((dto, matriarchId) =>
                matriarchId == null ||
                dto.Participants.Any(p => p.UserId == matriarchId && p.HiddenRole == HiddenRole.Outlaw))
            .WithMessage("Matriarch player must be a participant who started as an Outlaw")
            .When(IsSheriffMode);
    }
    
    private static bool IsSheriffMode(CreateMatchDto dto) =>
        dto.MatchType is Models.MatchType.FivePlayerSheriff or Models.MatchType.SixPlayerSheriff;
    
    private static bool ValidateParticipantCount(Models.MatchType matchType, int count) =>
        matchType switch
        {
            Models.MatchType.OneVsOneVsOne => count == 3,
            Models.MatchType.OneVsOneVsOneVsOne => count == 4,
            Models.MatchType.FivePlayerSheriff => count == 5,
            Models.MatchType.SixPlayerSheriff => count == 6,
            _ => false
        };
}
