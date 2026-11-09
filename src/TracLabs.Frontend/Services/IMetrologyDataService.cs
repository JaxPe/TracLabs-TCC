using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Services;

public interface IMetrologyDataService
{
    // Clientes
    Task<List<ClientDto>> GetClientsAsync(string? search = null);
    Task<ClientDto> SaveClientAsync(ClientDto client);

    // Equipamentos dos Clientes
    Task<List<ClientEquipmentDto>> GetClientEquipmentAsync(string? search = null, string? clientId = null);
    Task<ClientEquipmentDto?> GetClientEquipmentByIdAsync(string id);
    Task<ClientEquipmentDto> SaveClientEquipmentAsync(ClientEquipmentDto equipment);
    Task<ClientEquipmentDto> UpdateEquipmentStatusAsync(string equipmentId, EquipmentStatus newStatus);

    // Padrões do Laboratório
    Task<List<StandardEquipmentDto>> GetStandardEquipmentAsync(string? search = null, StandardRole? role = null);
    Task<List<StandardEquipmentDto>> GetExpiringStandardsAsync(int daysThreshold = 30);
    Task<StandardEquipmentDto> SaveStandardEquipmentAsync(StandardEquipmentDto standard);

    // Dashboard & Métricas
    Task<DashboardMetricsDto> GetDashboardMetricsAsync();
    Task<List<CalibrationTimelineEventDto>> GetRecentEventsAsync();

    // Calibração e Certificados
    Task<VolumeCalibrationResultDto?> CalculateVolumeCalibrationAsync(VolumeCalibrationCalculationRequest request);
    Task<CalibrationRecordDto> SaveCalibrationAsync(CalibrationRecordDto calibration);
    Task<CalibrationRecordDto?> GetCalibrationByIdAsync(string id);
    Task<List<CertificateDto>> GetCertificatesAsync(CertificateStatus? status = null);
    Task<CertificateDto?> GetCertificateByIdAsync(string id);
    Task<CertificateDto> CreateCertificateFromCalibrationAsync(CalibrationRecordDto calibration, bool showDetailedPoints = false);
    Task<CertificateDto> UpdateCertificateStatusAsync(string id, CertificateStatus newStatus, string? reason = null);
    Task<CertificateDto?> GenerateCertificatePdfAsync(string certificateId, bool showDetailedPoints = false);

    // Emissão e Download de PDF
    Task<byte[]?> GetCertificatePdfAsync(Guid certificateId);
    Task<byte[]?> GetCertificatePdfAsync(string certificateId);

    // Padrões do Laboratório - Ciclo de Vida e Rastreabilidade (ISO 17025 6.4.13)
    Task<List<StandardCertificateDto>> GetStandardCertificatesAsync(Guid standardId);
    Task<List<StandardCalibrationUsageDto>> GetStandardCalibrationsUsedAsync(Guid standardId);
    Task UpdateStandardAsync(StandardEquipmentDto standard);
    Task SendStandardToCalibrationAsync(Guid standardId, string laboratory, DateTime expectedReturn);
    Task RegisterStandardCertificateAsync(Guid standardId, StandardCertificateDto certificate);
}
