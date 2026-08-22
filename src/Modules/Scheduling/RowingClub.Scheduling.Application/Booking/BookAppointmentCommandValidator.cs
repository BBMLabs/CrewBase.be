using FluentValidation;

namespace RowingClub.Scheduling.Application.Booking;

public sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Phone).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Email).EmailAddress().MaximumLength(254)
            .When(c => !string.IsNullOrWhiteSpace(c.Email));
        RuleFor(c => c.BoatClass).NotEmpty();
        RuleFor(c => c.Note).MaximumLength(1000);
        RuleFor(c => c.ReminderMinutes).GreaterThanOrEqualTo(0).LessThanOrEqualTo(10080)
            .When(c => c.ReminderMinutes.HasValue);
    }
}
