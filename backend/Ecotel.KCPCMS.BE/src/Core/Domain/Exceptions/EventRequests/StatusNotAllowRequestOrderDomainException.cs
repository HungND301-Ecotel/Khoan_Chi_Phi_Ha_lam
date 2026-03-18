using Shared.Domain;

namespace Domain.Exceptions.EventRequests;

public class StatusNotAllowRequestOrderDomainException(
    string fieldName,
    object? specs,
    string message) : DomainException("StatusNotAllow", fieldName, specs, message);