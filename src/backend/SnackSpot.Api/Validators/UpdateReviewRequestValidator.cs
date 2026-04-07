using FluentValidation;
using SnackSpot.Api.Models.DTOs.Reviews;

namespace SnackSpot.Api.Validators;

public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .When(x => x.Rating.HasValue);

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .When(x => x.Comment is not null);
    }
}
