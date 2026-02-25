using App.Application.ExamAttempts.Commands;
using FluentValidation;

namespace App.Application.Validators
{
    public class StartExamCommandValidator : AbstractValidator<StartExamCommand>
    {
        public StartExamCommandValidator() {
            RuleFor(x => x.ExamId)
                .NotEmpty()
                .WithMessage("Mã đề thi không được để trống")
                .WithErrorCode("EXAM_ID_REQUIRED");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Mã người dùng không được để trống ")
                .WithErrorCode("USER_ID_REQUIRED");

            RuleFor(x => x.IpAddress)
               .Matches(@"^(\d{1,3}\.){3}\d{1,3}$|^[a-f0-9:]+$")
               .When(x => !string.IsNullOrEmpty(x.IpAddress))
               .WithMessage("Invalid IP address format")
               .WithErrorCode("INVALID_IP");

            RuleFor(x => x.UserAgent)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.UserAgent))
                .WithMessage("User agent too long")
                .WithErrorCode("USER_AGENT_TOO_LONG");
        }
    }
}
