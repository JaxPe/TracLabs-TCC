using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Clients.DTOs;

public record ClientDto(
    Guid Id,
    string Name,
    string TaxId,
    TaxIdType TaxIdType,
    string Email,
    string Phone,
    string ContactPerson,
    string Street,
    string City,
    string State,
    string PostalCode,
    bool IsActive,
    DateTime CreatedAt);
