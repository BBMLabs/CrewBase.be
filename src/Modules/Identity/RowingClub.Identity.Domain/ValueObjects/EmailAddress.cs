using System.Text.RegularExpressions;
using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.ValueObjects;

public sealed partial class EmailAddress : ValueObject
{
    /// <summary>
    /// Also used by Application-layer FluentValidation rules so malformed input is rejected as a
    /// 400 there, before it ever reaches <see cref="Create"/> - FluentValidation's built-in
    /// <c>EmailAddress()</c> rule is deliberately loose (ASP.NET Core [EmailAddress]-compatible)
    /// and lets some this pattern would reject, at which point they'd surface as a 409
    /// "email_invalid" DomainException instead of a 400 validation error.
    /// </summary>
    public const string Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new DomainException("email_required", "E-posta adresi zorunludur.");
        }

        var normalized = rawValue.Trim().ToLowerInvariant();

        if (normalized.Length > 254 || !EmailRegex().IsMatch(normalized))
        {
            throw new DomainException("email_invalid", "E-posta adresi formatı geçersiz.");
        }

        return new EmailAddress(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(Pattern)]
    private static partial Regex EmailRegex();
}
