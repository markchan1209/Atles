﻿using Atlas.Domain.Forums.Commands;
using Atlas.Domain.PermissionSets;
using FluentValidation;

namespace Atlas.Domain.Forums.Validators
{
    public class CreateForumValidator : AbstractValidator<CreateForum>
    {
        public CreateForumValidator(IForumRules rules, IPermissionSetRules permissionSetRules)
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Forum name is required.")
                .Length(1, 50).WithMessage("Forum name length must be between 1 and 50 characters.")
                .MustAsync((c, p, cancellation) => rules.IsNameUniqueAsync(c.SiteId, c.CategoryId, p))
                    .WithMessage(c => $"A forum with name {c.Name} already exists.");

            RuleFor(c => c.Description)
                .Length(1, 200).WithMessage("Forum description length must be between 1 and 200 characters.")
                .When(c => !string.IsNullOrWhiteSpace(c.Description));

            // TODO: Validate Category

            RuleFor(c => c.PermissionSetId)
                .MustAsync((c, p, cancellation) => permissionSetRules.IsValid(c.SiteId, p.Value))
                    .WithMessage(c => $"Permission set with id {c.PermissionSetId} does not exist.")
                    .When(c => c.PermissionSetId != null);
        }
    }
}