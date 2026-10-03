using FluentValidation;
using SariSariPOS.API.DTOs;

namespace SariSariPOS.API.Validators;

public class CreateSaleValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Cart is empty");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Qty).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(m => new[] { "cash", "gcash", "maya", "utang" }
                .Contains(m?.ToLower()))
            .WithMessage("Payment method must be cash, gcash, maya, or utang");
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AmountPaid).GreaterThanOrEqualTo(0);
    }
}