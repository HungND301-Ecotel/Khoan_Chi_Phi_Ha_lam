using Shared.Domain;

namespace Domain.Exceptions.EventRequests;

public class RequestOrderDomainException(
    string fieldName,
    string rule,
    object? specs,
    string message) : DomainException(rule, fieldName, specs, message);