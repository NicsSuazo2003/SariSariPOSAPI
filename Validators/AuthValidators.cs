using FluentValidation;
using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Validators;

public class SetupRequestValidator : AbstractValidator<SetupRequest>
{
    public SetupRequestValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Pin).NotEmpty().Length(4, 6).Matches("^[0-9]+$")
            .WithMessage("PIN must be 4-6 digits");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Pin).NotEmpty();
    }
}