using FluentValidation;
using SnackSpot.Api.Models.DTOs.Snacks;

namespace SnackSpot.Api.Validators;

public class CreateSnackRequestValidator : AbstractValidator<CreateSnackRequest>
{
    public CreateSnackRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 200);

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.StoreId)
            .NotEmpty();

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .When(x => x.Price.HasValue);

        RuleFor(x => x.ImageUrls)
            .Must(urls => urls == null || urls.Length <= 10)
            .WithMessage("Maximum 10 images allowed.");

        RuleForEach(x => x.ImageUrls)
            .MaximumLength(500)
            .When(x => x.ImageUrls is not null);

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Length <= 20)
            .WithMessage("Maximum 20 tags allowed.");

        RuleForEach(x => x.Tags)
            .Length(1, 50)
            .When(x => x.Tags is not null);
    }
}
