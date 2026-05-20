using FluentValidation;
using GarageMagicCore.DTOs.Deck;

namespace GarageMagicCore.Validators.Deck;

public class CreateDeckDtoValidator : AbstractValidator<CreateDeckDto>
{
    public CreateDeckDtoValidator()
    {
        RuleFor(x => x.DeckName)
            .NotEmpty()
            .WithMessage("Deck name is required")
            .Length(1, 100)
            .WithMessage("Deck name must be between 1 and 100 characters");
        
        RuleFor(x => x.CommanderName)
            .NotEmpty()
            .WithMessage("Commander name is required")
            .Length(1, 100)
            .WithMessage("Commander name must be between 1 and 100 characters");
        
        RuleFor(x => x.ColorIdentity)
            .MaximumLength(10)
            .WithMessage("Color identity must not exceed 10 characters")
            .Matches("^[WUBRGC]*$")
            .WithMessage("Color identity must only contain W, U, B, R, G, or C")
            .When(x => !string.IsNullOrEmpty(x.ColorIdentity));
    }
}

public class UpdateDeckDtoValidator : AbstractValidator<UpdateDeckDto>
{
    public UpdateDeckDtoValidator()
    {
        RuleFor(x => x.DeckName)
            .Length(1, 100)
            .WithMessage("Deck name must be between 1 and 100 characters")
            .When(x => !string.IsNullOrEmpty(x.DeckName));
        
        RuleFor(x => x.CommanderName)
            .Length(1, 100)
            .WithMessage("Commander name must be between 1 and 100 characters")
            .When(x => !string.IsNullOrEmpty(x.CommanderName));
        
        RuleFor(x => x.ColorIdentity)
            .MaximumLength(10)
            .WithMessage("Color identity must not exceed 10 characters")
            .Matches("^[WUBRGC]*$")
            .WithMessage("Color identity must only contain W, U, B, R, G, or C")
            .When(x => !string.IsNullOrEmpty(x.ColorIdentity));
    }
}

