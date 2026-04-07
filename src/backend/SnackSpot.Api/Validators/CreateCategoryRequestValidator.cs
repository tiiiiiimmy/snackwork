using FluentValidation;
using SnackSpot.Api.Models.DTOs.Categories;

namespace SnackSpot.Api.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.Icon)
            .MaximumLength(50)
            .When(x => x.Icon is not null);
    }
}
