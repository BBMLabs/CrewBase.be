using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : DomainEventBase;

public sealed record UserEmailVerifiedDomainEvent(Guid UserId) : DomainEventBase;

public sealed record UserLockedOutDomainEvent(Guid UserId, DateTimeOffset LockedUntilUtc) : DomainEventBase;

public sealed record CredentialPasswordChangedDomainEvent(Guid UserId) : DomainEventBase;

public sealed record RefreshTokenReuseDetectedDomainEvent(Guid UserId, Guid FamilyId) : DomainEventBase;
