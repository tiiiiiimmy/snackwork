using FluentValidation;
using SnackSpot.Api.Models.DTOs.Categories;

namespace SnackSpot.Api.Validators;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .Length(2, 100)
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.Icon)
            .MaximumLength(50)
            .When(x => x.Icon is not null);
    }
}
