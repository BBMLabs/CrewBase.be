namespace RowingClub.BuildingBlocks.Domain;

public class DomainValidationException(string errorCode, string message) : DomainException(errorCode, message);
