using FluentValidation;
using SnackSpot.Api.Models.DTOs.Users;

namespace SnackSpot.Api.Validators;

public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        When(x => x.AvatarUrl is not null, () =>
            RuleFor(x => x.AvatarUrl).MaximumLength(500));

        When(x => x.Bio is not null, () =>
            RuleFor(x => x.Bio).MaximumLength(200));
    }
}
