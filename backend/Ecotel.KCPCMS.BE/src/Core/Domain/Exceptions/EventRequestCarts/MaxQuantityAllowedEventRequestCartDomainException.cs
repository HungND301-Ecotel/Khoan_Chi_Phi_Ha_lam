using Shared.Domain;

namespace Domain.Exceptions.EventRequestCarts;

public class MaxQuantityAllowedEventRequestCartDomainException(
    string fieldName,
    object? specs,
    string message) : DomainException("MaxDomainAllowed", fieldName, specs, message);