using FluentValidation;
using SnackSpot.Api.Models.DTOs.Reviews;

namespace SnackSpot.Api.Validators;

public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .When(x => x.Comment is not null);
    }
}
