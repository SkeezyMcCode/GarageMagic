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
            .NotEmpty()
            .WithMessage("Match date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Match date cannot be in the future");
        
        RuleFor(x => x.Participants)
            .NotEmpty()
            .WithMessage("At least one participant is required")
            .Must((dto, participants) => ValidateParticipantCount(dto.MatchType, participants.Count))
            .WithMessage("Invalid number of participants for the match type");
        
        RuleFor(x => x.WinnerUserIds)
            .NotEmpty()
            .WithMessage("At least one winner is required")
            .Must((dto, winnerIds) => winnerIds.All(id => dto.Participants.Any(p => p.UserId == id)))
            .WithMessage("All winners must be participants in the match");
        
        RuleFor(x => x.Participants)
            .Must(participants => participants.Select(p => p.UserId).Distinct().Count() == participants.Count)
            .WithMessage("Duplicate participants are not allowed");
        
        // Sheriff mode validations
        RuleFor(x => x.Participants)
            .Must(participants => participants.Any(p => p.HiddenRole == HiddenRole.Sheriff))
            .WithMessage("Sheriff mode requires one Sheriff")
            .When(x => x.MatchType == Models.MatchType.FivePlayerSheriff || x.MatchType == Models.MatchType.SixPlayerSheriff);
        
        RuleFor(x => x.Participants)
            .Must(participants => participants.Count(p => p.HiddenRole == HiddenRole.Sheriff) == 1)
            .WithMessage("There can only be one Sheriff")
            .When(x => x.MatchType == Models.MatchType.FivePlayerSheriff || x.MatchType == Models.MatchType.SixPlayerSheriff);
    }
    
    private bool ValidateParticipantCount(Models.MatchType matchType, int participantCount)
    {
        return matchType switch
        {
            Models.MatchType.OneVsOneVsOne => participantCount == 3,
            Models.MatchType.OneVsOneVsOneVsOne => participantCount == 4,
            Models.MatchType.FivePlayerSheriff => participantCount == 5,
            Models.MatchType.SixPlayerSheriff => participantCount == 6,
            _ => false
        };
    }
}



