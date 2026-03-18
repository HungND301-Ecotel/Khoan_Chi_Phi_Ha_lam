using Shared.Domain;

namespace Domain.Exceptions;

public class SubscriptionDomainException(
    string fieldName,
    string rule,
    object? specs,
    string message) : DomainException(rule, fieldName, specs, message);