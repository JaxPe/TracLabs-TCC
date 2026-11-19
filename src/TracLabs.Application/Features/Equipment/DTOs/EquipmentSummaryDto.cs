using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Equipment.DTOs;

public record EquipmentSummaryDto(
    Guid Id,
    string LabTag,
    string Name,
    string Model,
    string Manufacturer,
    string SerialNumber,
    string InstrumentType,
    EquipmentStatus Status,
    string CurrentLocation,
    Guid ClientId,
    string? ClientName,
    decimal? NominalCapacity = null,
    string? Unit = null,
    decimal? RangeMin = null,
    decimal? RangeMax = null);
