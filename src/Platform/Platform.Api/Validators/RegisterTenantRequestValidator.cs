using FluentValidation;
using Platform.Application.DTOs;

namespace Platform.Api.Validators;

public class RegisterTenantRequestValidator : AbstractValidator<RegisterTenantRequest>
{
    public RegisterTenantRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters");

        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Legal name is required")
            .MaximumLength(200).WithMessage("Legal name must not exceed 200 characters");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required")
            .EmailAddress().WithMessage("Invalid email address");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Admin password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters");
    }
}
