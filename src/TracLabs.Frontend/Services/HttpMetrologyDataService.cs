using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services.Auth;

namespace TracLabs.Frontend.Services;

public class HttpMetrologyDataService : IMetrologyDataService
{
    private readonly HttpClient _httpClient;
    private readonly IUserContextService _userContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpMetrologyDataService(HttpClient httpClient, IUserContextService userContext)
    {
        _httpClient = httpClient;
        _userContext = userContext;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private void ApplyAuthorization(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_userContext.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _userContext.Token);
        }
    }

    private async Task<HttpResponseMessage> SendWithAuthAsync(HttpRequestMessage request)
    {
        ApplyAuthorization(request);
        return await _httpClient.SendAsync(request);
    }

    private static async Task EnsureSuccessWithDetailsAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var errorBody = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;
        var formattedMessage = $"Erro na API ({statusCode} {response.ReasonPhrase})";

        if (!string.IsNullOrWhiteSpace(errorBody))
        {
            try
            {
                using var doc = JsonDocument.Parse(errorBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
                {
                    var errorLines = new List<string>();
                    foreach (var prop in errorsProp.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var err in prop.Value.EnumerateArray())
                            {
                                var text = err.GetString();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    errorLines.Add(!string.IsNullOrWhiteSpace(prop.Name) ? $"{prop.Name}: {text}" : text);
                                }
                            }
                        }
                    }

                    if (errorLines.Count > 0)
                    {
                        formattedMessage = string.Join("\n", errorLines);
                    }
                }
                else if (root.TryGetProperty("message", out var msgProp) && !string.IsNullOrWhiteSpace(msgProp.GetString()))
                {
                    formattedMessage = msgProp.GetString()!;
                }
                else if (root.TryGetProperty("detail", out var detailProp) && !string.IsNullOrWhiteSpace(detailProp.GetString()))
                {
                    formattedMessage = detailProp.GetString()!;
                }
            }
            catch
            {
                formattedMessage = errorBody;
            }
        }

        throw new HttpRequestException(formattedMessage, null, response.StatusCode);
    }

    // =========================================================================
    // 1. CLIENTES
    // =========================================================================
    public async Task<List<ClientDto>> GetClientsAsync(string? search = null)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? "api/v1/clients"
            : $"api/v1/clients?name={Uri.EscapeDataString(search)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await SendWithAuthAsync(request);

        if (!response.IsSuccessStatusCode)
            return new List<ClientDto>();

        var apiClients = await response.Content.ReadFromJsonAsync<List<ApiClientDto>>(_jsonOptions);
        if (apiClients == null) return new List<ClientDto>();

        return apiClients.Select(c => new ClientDto
        {
            Id = c.Id.ToString(),
            Name = c.Name,
            TaxId = c.TaxId,
            Email = c.Email,
            Phone = c.Phone,
            ContactPerson = c.ContactPerson ?? string.Empty,
            Street = c.Street ?? string.Empty,
            City = c.City ?? string.Empty,
            State = c.State ?? string.Empty,
            PostalCode = c.PostalCode ?? string.Empty,
            ActiveEquipmentCount = 0,
            IsActive = c.IsActive
        }).ToList();
    }

    public async Task<ClientDto> SaveClientAsync(ClientDto client)
    {
        var hasGuid = Guid.TryParse(client.Id, out var existingId);
        var isExisting = hasGuid && existingId != Guid.Empty;

        var command = new
        {
            Id = isExisting ? existingId : Guid.NewGuid(),
            Name = client.Name,
            TaxId = client.TaxId.Replace(".", "").Replace("/", "").Replace("-", "").Trim(),
            TaxIdType = client.TaxId.Length > 14 ? 0 : 1, // 0 = CNPJ, 1 = CPF
            Email = client.Email,
            Phone = client.Phone,
            ContactPerson = client.ContactPerson,
            Street = client.Street,
            City = client.City,
            State = client.State,
            PostalCode = client.PostalCode
        };

        HttpResponseMessage response;
        if (isExisting)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/clients/{existingId}")
            {
                Content = JsonContent.Create(command, options: _jsonOptions)
            };
            response = await SendWithAuthAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                using var postRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/clients")
                {
                    Content = JsonContent.Create(command, options: _jsonOptions)
                };
                response = await SendWithAuthAsync(postRequest);
            }
        }
        else
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/clients")
            {
                Content = JsonContent.Create(command, options: _jsonOptions)
            };
            response = await SendWithAuthAsync(request);
        }

        await EnsureSuccessWithDetailsAsync(response);
        var saved = await response.Content.ReadFromJsonAsync<ApiClientDto>(_jsonOptions);

        return new ClientDto
        {
            Id = saved?.Id.ToString() ?? client.Id,
            Name = saved?.Name ?? client.Name,
            TaxId = saved?.TaxId ?? client.TaxId,
            Email = saved?.Email ?? client.Email,
            Phone = saved?.Phone ?? client.Phone,
            ContactPerson = saved?.ContactPerson ?? client.ContactPerson,
            Street = saved?.Street ?? client.Street,
            City = saved?.City ?? client.City,
            State = saved?.State ?? client.State,
            PostalCode = saved?.PostalCode ?? client.PostalCode,
            ActiveEquipmentCount = client.ActiveEquipmentCount,
            IsActive = saved?.IsActive ?? true
        };
    }

    // =========================================================================
    // 2. EQUIPAMENTOS DE CLIENTES
    // =========================================================================
    public async Task<List<ClientEquipmentDto>> GetClientEquipmentAsync(string? search = null, string? clientId = null)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"labTag={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(clientId) && Guid.TryParse(clientId, out var cid))
            queryParams.Add($"clientId={cid}");

        var url = "api/v1/equipment" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await SendWithAuthAsync(request);

        if (!response.IsSuccessStatusCode)
            return new List<ClientEquipmentDto>();

        var items = await response.Content.ReadFromJsonAsync<List<ApiEquipmentSummaryDto>>(_jsonOptions);
        if (items == null) return new List<ClientEquipmentDto>();

        return items.Select(e =>
        {
            var capacity = e.NominalCapacity.GetValueOrDefault(0m);
            if (capacity <= 0) capacity = 100.0m;
            var unit = !string.IsNullOrWhiteSpace(e.Unit) ? e.Unit : "mL";
            var ema = MetrologyCalculations.ResolveMaxPermissibleError(capacity, "Classe A");

            return new ClientEquipmentDto
            {
                Id = e.Id.ToString(),
                ClientId = e.ClientId.ToString(),
                ClientName = e.ClientName ?? string.Empty,
                LabTag = e.LabTag,
                Description = e.Name,
                Manufacturer = e.Manufacturer,
                Model = e.Model,
                SerialNumber = e.SerialNumber,
                InstrumentType = e.InstrumentType,
                NominalCapacity = capacity,
                Unit = unit,
                AccuracyClass = "Classe A",
                MaxPermissibleError = ema,
                Status = e.Status,
                RangeMin = e.RangeMin,
                RangeMax = e.RangeMax,
                CurrentLocation = !string.IsNullOrWhiteSpace(e.CurrentLocation) ? e.CurrentLocation : "Bancada de Recepção"
            };
        }).ToList();
    }

    public async Task<ClientEquipmentDto?> GetClientEquipmentByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/equipment/{guid}");
        var response = await SendWithAuthAsync(request);

        if (!response.IsSuccessStatusCode) return null;

        var detail = await response.Content.ReadFromJsonAsync<ApiEquipmentDetailDto>(_jsonOptions);
        if (detail == null) return null;

        var cap = detail.NominalCapacity.GetValueOrDefault(0m);
        if (cap <= 0) cap = 100.0m;
        var emaDetail = MetrologyCalculations.ResolveMaxPermissibleError(cap, "Classe A");

        return new ClientEquipmentDto
        {
            Id = detail.Id.ToString(),
            ClientId = detail.ClientId.ToString(),
            ClientName = detail.ClientName ?? string.Empty,
            LabTag = detail.LabTag,
            Description = detail.Name,
            Manufacturer = detail.Manufacturer,
            Model = detail.Model,
            SerialNumber = detail.SerialNumber,
            InstrumentType = detail.InstrumentType,
            NominalCapacity = cap,
            Unit = !string.IsNullOrWhiteSpace(detail.Unit) ? detail.Unit : "mL",
            AccuracyClass = "Classe A",
            MaxPermissibleError = emaDetail,
            Status = detail.Status,
            RangeMin = detail.RangeMin,
            RangeMax = detail.RangeMax,
            CurrentLocation = !string.IsNullOrWhiteSpace(detail.CurrentLocation) ? detail.CurrentLocation : "Bancada de Recepção"
        };
    }

    public async Task<ClientEquipmentDto> UpdateEquipmentStatusAsync(string equipmentId, EquipmentStatus newStatus)
    {
        if (!Guid.TryParse(equipmentId, out var guid))
            throw new ArgumentException("ID de equipamento inválido", nameof(equipmentId));

        var command = new { Status = newStatus, NewStatus = newStatus };
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/v1/equipment/{guid}/status")
        {
            Content = JsonContent.Create(command, options: _jsonOptions)
        };
        var response = await SendWithAuthAsync(request);
        await EnsureSuccessWithDetailsAsync(response);

        var updated = await GetClientEquipmentByIdAsync(equipmentId);
        return updated ?? new ClientEquipmentDto { Id = equipmentId, Status = newStatus };
    }

    public async Task<ClientEquipmentDto> SaveClientEquipmentAsync(ClientEquipmentDto equipment)
    {
        var hasGuid = Guid.TryParse(equipment.Id, out var existingId);
        var isExisting = hasGuid && existingId != Guid.Empty;
        var clientGuid = Guid.TryParse(equipment.ClientId, out var cg) ? cg : Guid.Empty;

        var command = new
        {
            Id = isExisting ? existingId : Guid.NewGuid(),
            LabTag = equipment.LabTag,
            Name = equipment.Description,
            Model = equipment.Model,
            Manufacturer = equipment.Manufacturer,
            SerialNumber = equipment.SerialNumber,
            InstrumentType = equipment.InstrumentType,
            RangeMin = equipment.RangeMin,
            RangeMax = equipment.RangeMax,
            Unit = equipment.Unit,
            Resolution = (decimal?)null,
            NominalCapacity = (decimal?)equipment.NominalCapacity,
            ClientId = clientGuid,
            CurrentLocation = !string.IsNullOrWhiteSpace(equipment.CurrentLocation) ? equipment.CurrentLocation : "Bancada de Recepção",
            AcceptanceCriteria = "Conforme ISO 4787 / ISO 8655",
            Status = equipment.Status
        };

        HttpResponseMessage response;
        if (isExisting)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/equipment/{existingId}")
            {
                Content = JsonContent.Create(command, options: _jsonOptions)
            };
            response = await SendWithAuthAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                using var postRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/equipment")
                {
                    Content = JsonContent.Create(command, options: _jsonOptions)
                };
                response = await SendWithAuthAsync(postRequest);
            }
        }
        else
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/equipment")
            {
                Content = JsonContent.Create(command, options: _jsonOptions)
            };
            response = await SendWithAuthAsync(request);
        }

        await EnsureSuccessWithDetailsAsync(response);
        var saved = await response.Content.ReadFromJsonAsync<ApiEquipmentDetailDto>(_jsonOptions);

        return new ClientEquipmentDto
        {
            Id = saved?.Id.ToString() ?? equipment.Id,
            ClientId = saved?.ClientId.ToString() ?? equipment.ClientId,
            ClientName = saved?.ClientName ?? equipment.ClientName,
            LabTag = saved?.LabTag ?? equipment.LabTag,
            Description = saved?.Name ?? equipment.Description,
            Manufacturer = saved?.Manufacturer ?? equipment.Manufacturer,
            Model = saved?.Model ?? equipment.Model,
            SerialNumber = saved?.SerialNumber ?? equipment.SerialNumber,
            InstrumentType = saved?.InstrumentType ?? equipment.InstrumentType,
            NominalCapacity = saved?.NominalCapacity ?? equipment.NominalCapacity,
            Unit = saved?.Unit ?? equipment.Unit,
            AccuracyClass = equipment.AccuracyClass,
            MaxPermissibleError = equipment.MaxPermissibleError,
            Status = saved?.Status ?? equipment.Status,
            RangeMin = saved?.RangeMin ?? equipment.RangeMin,
            RangeMax = saved?.RangeMax ?? equipment.RangeMax,
            CurrentLocation = !string.IsNullOrWhiteSpace(saved?.CurrentLocation) ? saved.CurrentLocation : (!string.IsNullOrWhiteSpace(equipment.CurrentLocation) ? equipment.CurrentLocation : "Bancada de Recepção")
        };
    }

    // =========================================================================
    // 3. PADRÕES DO LABORATÓRIO (STANDARD EQUIPMENT)
    // =========================================================================
    public async Task<List<StandardEquipmentDto>> GetStandardEquipmentAsync(string? search = null, StandardRole? role = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/standardequipment");
        var response = await SendWithAuthAsync(request);

        if (!response.IsSuccessStatusCode)
            return new List<StandardEquipmentDto>();

        var items = await response.Content.ReadFromJsonAsync<List<ApiStandardEquipmentDto>>(_jsonOptions);
        if (items == null) return new List<StandardEquipmentDto>();

        var list = items.Select(MapStandardDto).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            list = list.Where(s =>
                s.LabTag.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.SerialNumber.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (role.HasValue)
        {
            list = list.Where(s => s.Role == role.Value).ToList();
        }

        return list;
    }

    public async Task<List<StandardEquipmentDto>> GetExpiringStandardsAsync(int daysThreshold = 30)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/standardequipment/expiring-soon?withinDays={daysThreshold}");
        var response = await SendWithAuthAsync(request);

        if (!response.IsSuccessStatusCode)
            return new List<StandardEquipmentDto>();

        var items = await response.Content.ReadFromJsonAsync<List<ApiStandardEquipmentDto>>(_jsonOptions);
        if (items == null) return new List<StandardEquipmentDto>();

        return items.Select(MapStandardDto).ToList();
    }

    public async Task<StandardEquipmentDto> SaveStandardEquipmentAsync(StandardEquipmentDto standard)
    {
        var hasGuid = Guid.TryParse(standard.Id, out var existingId);
        var isExisting = hasGuid && existingId != Guid.Empty;

        var command = new
        {
            Id = isExisting ? existingId : Guid.NewGuid(),
            LabTag = standard.LabTag,
            Name = string.IsNullOrWhiteSpace(standard.Name) ? standard.LabTag : standard.Name,
            Model = string.IsNullOrWhiteSpace(standard.Model) ? "N/A" : standard.Model,
            Manufacturer = string.IsNullOrWhiteSpace(standard.Manufacturer) ? "N/A" : standard.Manufacturer,
            SerialNumber = string.IsNullOrWhiteSpace(standard.SerialNumber) ? "N/A" : standard.SerialNumber,
            InstrumentType = string.IsNullOrWhiteSpace(standard.InstrumentType) ? (standard.Name ?? standard.LabTag) : standard.InstrumentType,
            Role = standard.Role,
            CurrentLocation = standard.CurrentLocation,
            CalibrationIntervalMonths = 12,
            Status = standard.Status,
            Error = standard.Error,
            Correction = standard.Correction,
            ExpandedUncertainty = standard.ExpandedUncertainty,
            CoverageFactor = standard.CoverageFactor > 0 ? standard.CoverageFactor : 2.0m,
            Resolution = standard.Resolution,
            CalibrationDueDate = standard.CalibrationDueDate != default ? standard.CalibrationDueDate : DateTime.UtcNow.AddYears(1),
            CalibrationCertificateNumber = standard.CalibrationCertificateNumber ?? string.Empty,
            CalibratingLaboratory = standard.CalibratingLaboratory ?? string.Empty,
            RangeMin = standard.RangeMin,
            RangeMax = standard.RangeMax
        };

        HttpResponseMessage response;
        if (isExisting)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/standardequipment/{existingId}")
            {
                Content = JsonContent.Create(command, options: _jsonOptions)
            };
            response = await SendWithAuthAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                using var postRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/standardequipment")
                {
                    Content = JsonContent.Create(command, options: _jsonOptions)
                };
                response = await SendWithAuthAsync(postRequest);
            }
        }
        else
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/standardequipment")
            {
                Content = JsonContent.Create(command, options: _jsonOptions)
            };
            response = await SendWithAuthAsync(request);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Erro ao salvar equipamento padrão ({response.StatusCode}): {errorBody}");
        }

        var saved = await response.Content.ReadFromJsonAsync<ApiStandardEquipmentDto>(_jsonOptions);
        return saved != null ? MapStandardDto(saved) : standard;
    }

    // =========================================================================
    // 4. DASHBOARD & MÉTRICAS
    // =========================================================================
    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/dashboard/metrics");
        var response = await SendWithAuthAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return new DashboardMetricsDto();
        }

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(_jsonOptions);
        return metrics ?? new DashboardMetricsDto();
    }

    public async Task<List<CalibrationTimelineEventDto>> GetRecentEventsAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/dashboard/recent-activity?count=10");
        var response = await SendWithAuthAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return new List<CalibrationTimelineEventDto>();
        }

        var events = await response.Content.ReadFromJsonAsync<List<CalibrationTimelineEventDto>>(_jsonOptions);
        return events ?? new List<CalibrationTimelineEventDto>();
    }

    // =========================================================================
    // 5. CALIBRAÇÃO E CERTIFICADOS
    // =========================================================================
    public async Task<VolumeCalibrationResultDto?> CalculateVolumeCalibrationAsync(VolumeCalibrationCalculationRequest request)
    {
        var command = new
        {
            NominalVolumeMl = request.NominalVolumeMl,
            Material = request.Material,
            Measurements = request.Measurements.Select(m => new
            {
                RunNumber = m.RunNumber,
                FullMassGrams = m.FullMassGrams,
                EmptyMassGrams = m.EmptyMassGrams,
                WaterTemperatureCelsius = m.WaterTemperatureCelsius,
                AmbientTemperatureCelsius = m.AmbientTemperatureCelsius,
                AtmosphericPressureHpa = m.AtmosphericPressureHpa,
                RelativeHumidityPercent = m.RelativeHumidityPercent
            }).ToList(),
            BalanceStandardId = request.BalanceStandardId,
            LiquidThermometerStandardId = request.LiquidThermometerStandardId,
            BarometerStandardId = request.BarometerStandardId,
            AmbientThermometerStandardId = request.AmbientThermometerStandardId,
            HygrometerStandardId = request.HygrometerStandardId,
            ThermoHygrometerStandardId = request.ThermoHygrometerStandardId,
            ThermalExpansionCoefficient = request.ThermalExpansionCoefficient,
            MeniscusSemiIntervalMl = request.MeniscusSemiIntervalMl,
            WaterDensityGcm3 = request.WaterDensityGcm3,
            PycnometerStandardId = request.PycnometerStandardId,
            WaterDensityUncertaintyGcm3 = request.WaterDensityUncertaintyGcm3
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/calibrations/volume/calculate")
        {
            Content = JsonContent.Create(command, options: _jsonOptions)
        };
        var response = await SendWithAuthAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<VolumeCalibrationResultDto>(_jsonOptions);
    }

    public async Task<CalibrationRecordDto> SaveCalibrationAsync(CalibrationRecordDto calibration)
    {
        var eqGuid = Guid.TryParse(calibration.EquipmentId, out var eg) ? eg : Guid.Empty;
        var balGuid = Guid.TryParse(calibration.BalanceStandardId, out var bg) ? bg : Guid.Empty;
        var liqGuid = Guid.TryParse(calibration.LiquidThermometerStandardId, out var lg) ? lg : Guid.Empty;
        var barGuid = Guid.TryParse(calibration.BarometerStandardId, out var brg) ? brg : Guid.Empty;
        var ambGuid = Guid.TryParse(calibration.AmbientThermometerStandardId, out var ag) && ag != Guid.Empty ? (Guid?)ag : null;
        var hygGuid = Guid.TryParse(calibration.HygrometerStandardId, out var hg) && hg != Guid.Empty ? (Guid?)hg : null;
        var thGuid = Guid.TryParse(calibration.ThermoHygrometerStandardId, out var tg) && tg != Guid.Empty ? (Guid?)tg : null;
        var pycGuid = Guid.TryParse(calibration.PycnometerStandardId, out var pg) && pg != Guid.Empty ? (Guid?)pg : null;

        var command = new
        {
            EquipmentId = eqGuid,
            BalanceStandardId = balGuid,
            LiquidThermometerStandardId = liqGuid,
            AmbientThermometerStandardId = ambGuid,
            BarometerStandardId = barGuid,
            HygrometerStandardId = hygGuid,
            ThermoHygrometerStandardId = thGuid,
            NextCalibrationDue = DateTime.UtcNow.AddYears(1),
            NominalVolumeMl = calibration.NominalCapacity,
            Material = calibration.Material,
            WaterDensityGcm3 = calibration.WaterDensityGcm3,
            PycnometerStandardId = pycGuid,
            Observations = calibration.StandardMethod,
            Measurements = calibration.Measurements.Select(m => new
            {
                RunNumber = m.RunNumber,
                FullMassGrams = m.FullMassGrams,
                EmptyMassGrams = m.EmptyMassGrams,
                WaterTemperatureCelsius = m.WaterTemperatureCelsius,
                AmbientTemperatureCelsius = m.AmbientTemperatureCelsius,
                AtmosphericPressureHpa = m.AtmosphericPressureHpa,
                RelativeHumidityPercent = m.RelativeHumidityPercent
            }).ToList()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/calibrations/volume")
        {
            Content = JsonContent.Create(command, options: _jsonOptions)
        };
        var response = await SendWithAuthAsync(request);
        await EnsureSuccessWithDetailsAsync(response);

        var result = await response.Content.ReadFromJsonAsync<ApiCalibrationResultDto>(_jsonOptions);
        if (result != null)
        {
            calibration.Id = result.Id.ToString();
            calibration.SystematicError = result.MeanError;
            calibration.ExpandedUncertainty = result.ExpandedUncertainty;
            calibration.CombinedUncertainty = result.CombinedStandardUncertainty;
            calibration.CoverageFactor = result.CoverageFactor;
            calibration.Result = result.Result;

            if (result.VolumeCalculationDetails != null)
            {
                calibration.CalculatedWaterDensity = result.VolumeCalculationDetails.WaterDensityGramsPerCm3;
                calibration.CalculatedAirDensity = result.VolumeCalculationDetails.AirDensityKgPerM3;
                calibration.MeanV20 = result.VolumeCalculationDetails.MeanVolumeV20Ml;
                calibration.SystematicError = result.VolumeCalculationDetails.SystematicErrorMl;
                calibration.ExpandedUncertainty = result.VolumeCalculationDetails.ExpandedUncertaintyMl;
                calibration.CoverageFactor = result.VolumeCalculationDetails.CoverageFactor;
                calibration.CombinedUncertainty = result.VolumeCalculationDetails.CombinedStandardUncertaintyMl;
                calibration.SampleStdDev = result.VolumeCalculationDetails.RepeatabilityStandardDeviationMl;
                calibration.RepeatabilityUncertainty = result.VolumeCalculationDetails.RepeatabilityUncertaintyMl;
            }
        }

        return calibration;
    }

    public async Task<CalibrationRecordDto?> GetCalibrationByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/calibrations/{guid}");
        var response = await SendWithAuthAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var apiCal = await response.Content.ReadFromJsonAsync<ApiCalibrationResultDto>(_jsonOptions);
        if (apiCal == null) return null;

        return new CalibrationRecordDto
        {
            Id = apiCal.Id.ToString(),
            EquipmentId = apiCal.EquipmentId.ToString(),
            EquipmentTag = apiCal.EquipmentLabTag ?? string.Empty,
            EquipmentDescription = apiCal.EquipmentName ?? string.Empty,
            NominalCapacity = apiCal.VolumeCalculationDetails?.NominalVolumeMl ?? 100m,
            MeanV20 = apiCal.VolumeCalculationDetails?.MeanVolumeV20Ml ?? 0m,
            SystematicError = apiCal.MeanError,
            ExpandedUncertainty = apiCal.ExpandedUncertainty,
            CoverageFactor = apiCal.CoverageFactor,
            CombinedUncertainty = apiCal.CombinedStandardUncertainty,
            Result = apiCal.Result,
            CreatedAt = apiCal.CalibrationDate
        };
    }

    public async Task<List<CertificateDto>> GetCertificatesAsync(CertificateStatus? status = null)
    {
        var url = status.HasValue
            ? $"api/v1/certificates?status={status.Value}"
            : "api/v1/certificates";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await SendWithAuthAsync(request);
        if (!response.IsSuccessStatusCode) return new List<CertificateDto>();

        var certs = await response.Content.ReadFromJsonAsync<List<ApiCertificateSummaryDto>>(_jsonOptions);
        if (certs == null) return new List<CertificateDto>();

        return certs.Select(c => new CertificateDto
        {
            Id = c.Id.ToString(),
            CertificateNumber = c.CertificateNumber,
            CalibrationRecordId = c.CalibrationRecordId.ToString(),
            EquipmentTag = c.EquipmentTag,
            EquipmentDescription = c.EquipmentName,
            ClientName = c.ClientName,
            Status = c.Status,
            CalibrationDate = c.CalibrationDate,
            IssueDate = c.IssuedAt ?? c.CalibrationDate
        }).ToList();
    }

    public async Task<CertificateDto?> GetCertificateByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/certificates/{guid}");
        var response = await SendWithAuthAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var c = await response.Content.ReadFromJsonAsync<ApiCertificateDetailDto>(_jsonOptions);
        if (c == null) return null;

        return new CertificateDto
        {
            Id = c.Id.ToString(),
            CertificateNumber = c.CertificateNumber,
            CalibrationRecordId = c.CalibrationRecordId.ToString(),
            EquipmentTag = c.EquipmentTag,
            EquipmentDescription = c.EquipmentName,
            ClientName = c.ClientName,
            Status = c.Status,
            CalibrationDate = c.CalibrationDate,
            NextCalibrationDueDate = c.NextCalibrationDue,
            IssueDate = c.IssuedAt ?? c.CalibrationDate,
            NominalVolume = c.NominalVolumeMl,
            SystematicError = c.SystematicErrorMl,
            ExpandedUncertainty = c.ExpandedUncertaintyMl,
            CoverageFactor = c.CoverageFactor,
            Result = c.Result
        };
    }

    public async Task<CertificateDto> CreateCertificateFromCalibrationAsync(CalibrationRecordDto calibration, bool showDetailedPoints = false)
    {
        var certs = await GetCertificatesAsync();
        var existing = certs.FirstOrDefault(c => c.CalibrationRecordId == calibration.Id);

        if (existing != null && Guid.TryParse(existing.Id, out var certGuid))
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/certificates/{certGuid}/generate-pdf")
            {
                Content = JsonContent.Create(new { ShowDetailedPoints = showDetailedPoints }, options: _jsonOptions)
            };
            var genRes = await SendWithAuthAsync(req);
            await EnsureSuccessWithDetailsAsync(genRes);
            return (await GetCertificateByIdAsync(existing.Id)) ?? existing;
        }

        return new CertificateDto
        {
            CalibrationRecordId = calibration.Id,
            EquipmentTag = calibration.EquipmentTag,
            ClientName = calibration.ClientName,
            Status = CertificateStatus.Draft
        };
    }

    public async Task<CertificateDto> UpdateCertificateStatusAsync(string id, CertificateStatus newStatus, string? reason = null)
    {
        if (!Guid.TryParse(id, out var certGuid))
            throw new ArgumentException("ID de certificado inválido", nameof(id));

        HttpResponseMessage response;
        if (newStatus == CertificateStatus.Issued)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/certificates/{certGuid}/issue")
            {
                Content = JsonContent.Create(new { ShowDetailedPoints = false }, options: _jsonOptions)
            };
            response = await SendWithAuthAsync(req);
        }
        else if (newStatus == CertificateStatus.Superseded)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/certificates/{certGuid}/supersede")
            {
                Content = JsonContent.Create(new { Reason = reason ?? "Substituído formalmente" }, options: _jsonOptions)
            };
            response = await SendWithAuthAsync(req);
        }
        else
        {
            // Draft
            return (await GetCertificateByIdAsync(id)) ?? new CertificateDto { Id = id, Status = newStatus };
        }

        await EnsureSuccessWithDetailsAsync(response);
        return (await GetCertificateByIdAsync(id)) ?? new CertificateDto { Id = id, Status = newStatus };
    }

    public async Task<CertificateDto?> GenerateCertificatePdfAsync(string certificateId, bool showDetailedPoints = false)
    {
        if (!Guid.TryParse(certificateId, out var certGuid)) return null;

        using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/certificates/{certGuid}/generate-pdf")
        {
            Content = JsonContent.Create(new { ShowDetailedPoints = showDetailedPoints }, options: _jsonOptions)
        };
        var response = await SendWithAuthAsync(req);
        if (!response.IsSuccessStatusCode) return null;

        return await GetCertificateByIdAsync(certificateId);
    }

    public async Task<byte[]?> GetCertificatePdfAsync(Guid certificateId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/certificates/{certificateId}/pdf");
        var response = await SendWithAuthAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }

        // Se ainda não foi gerado no banco, solicita a geração oficial ao QuestPDF na API
        using var genReq = new HttpRequestMessage(HttpMethod.Post, $"api/v1/certificates/{certificateId}/generate-pdf");
        var genResponse = await SendWithAuthAsync(genReq);
        if (genResponse.IsSuccessStatusCode)
        {
            using var retryRequest = new HttpRequestMessage(HttpMethod.Get, $"api/v1/certificates/{certificateId}/pdf");
            var retryResponse = await SendWithAuthAsync(retryRequest);
            if (retryResponse.IsSuccessStatusCode)
            {
                return await retryResponse.Content.ReadAsByteArrayAsync();
            }
        }

        return null;
    }

    public async Task<byte[]?> GetCertificatePdfAsync(string certificateId)
    {
        if (!Guid.TryParse(certificateId, out var guid)) return null;
        return await GetCertificatePdfAsync(guid);
    }

    // =========================================================================
    // 6. CICLO DE VIDA E HISTÓRICO DE PADRÕES (ISO 17025 6.4.13)
    // =========================================================================
    public async Task<List<StandardCertificateDto>> GetStandardCertificatesAsync(Guid standardId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/standardequipment/{standardId}/certificates");
        var response = await SendWithAuthAsync(request);
        if (!response.IsSuccessStatusCode) return new List<StandardCertificateDto>();

        var list = await response.Content.ReadFromJsonAsync<List<ApiStandardCertDto>>(_jsonOptions);
        if (list == null) return new List<StandardCertificateDto>();

        return list.Select(c => new StandardCertificateDto
        {
            Id = c.Id,
            StandardEquipmentId = c.StandardEquipmentId,
            CertificateNumber = c.CertificateNumber,
            CalibratingLaboratory = c.CalibratingLaboratory,
            CalibrationDate = c.CalibrationDate,
            CalibrationDueDate = c.CalibrationDueDate,
            CalibrationRangeMin = c.CalibrationRangeMin,
            CalibrationRangeMax = c.CalibrationRangeMax,
            IsActive = c.IsActive,
            FitType = c.FitType.ToString(),
            Slope = c.Slope,
            Intercept = c.Intercept,
            R2 = c.R2
        }).ToList();
    }

    public async Task<List<StandardCalibrationUsageDto>> GetStandardCalibrationsUsedAsync(Guid standardId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/standardequipment/{standardId}/usage");
        var response = await SendWithAuthAsync(request);
        if (!response.IsSuccessStatusCode) return new List<StandardCalibrationUsageDto>();

        var list = await response.Content.ReadFromJsonAsync<List<StandardCalibrationUsageDto>>(_jsonOptions);
        return list ?? new List<StandardCalibrationUsageDto>();
    }

    public async Task UpdateStandardAsync(StandardEquipmentDto standard)
    {
        await SaveStandardEquipmentAsync(standard);
    }

    public async Task SendStandardToCalibrationAsync(Guid standardId, string laboratory, DateTime expectedReturn)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/standardequipment/{standardId}/send-to-calibration")
        {
            Content = JsonContent.Create(new
            {
                Id = standardId,
                CalibratingLaboratory = laboratory,
                ExpectedReturnDate = expectedReturn
            }, options: _jsonOptions)
        };
        var response = await SendWithAuthAsync(request);
        await EnsureSuccessWithDetailsAsync(response);
    }

    public async Task RegisterStandardCertificateAsync(Guid standardId, StandardCertificateDto certificate)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/standardequipment/{standardId}/certificates")
        {
            Content = JsonContent.Create(new
            {
                StandardEquipmentId = standardId,
                CertificateNumber = certificate.CertificateNumber,
                CalibratingLaboratory = certificate.CalibratingLaboratory,
                CalibrationDate = certificate.CalibrationDate,
                CalibrationDueDate = certificate.CalibrationDueDate,
                CalibrationRangeMin = certificate.CalibrationRangeMin,
                CalibrationRangeMax = certificate.CalibrationRangeMax,
                FitType = 0, // LinearRegression
                UncertaintyPolicy = 0, // WorstCase
                Points = new List<object>()
            }, options: _jsonOptions)
        };
        var response = await SendWithAuthAsync(request);
        await EnsureSuccessWithDetailsAsync(response);
    }

    // =========================================================================
    // HELPERS DE MAPEAMENTO INTERNO
    // =========================================================================
    private static StandardEquipmentDto MapStandardDto(ApiStandardEquipmentDto s)
    {
        return new StandardEquipmentDto
        {
            Id = s.Id.ToString(),
            LabTag = s.LabTag,
            Name = s.Name,
            InstrumentType = s.InstrumentType,
            Role = s.Role ?? InferStandardRole(s.InstrumentType, s.Name),
            Manufacturer = s.Manufacturer,
            Model = s.Model,
            SerialNumber = s.SerialNumber,
            Resolution = s.Resolution,
            RangeMin = s.RangeMin,
            RangeMax = s.RangeMax,
            Status = s.Status,
            CurrentLocation = s.CurrentLocation,
            CalibrationCertificateNumber = s.CalibrationCertificateNumber,
            CalibratingLaboratory = s.CalibratingLaboratory,
            LastCalibrationDate = s.LastCalibrationDate,
            CalibrationDueDate = s.CalibrationDueDate,
            ExpandedUncertainty = s.ExpandedUncertainty,
            CoverageFactor = s.CoverageFactor,
            Error = s.Error,
            Correction = s.Correction
        };
    }

    private static StandardRole InferStandardRole(string? type, string? name)
    {
        var text = $"{type} {name}".ToLowerInvariant();
        if (text.Contains("balan")) return StandardRole.Balance;
        if (text.Contains("peso") || text.Contains("massa")) return StandardRole.ReferenceWeights;
        if (text.Contains("termo") && (text.Contains("liq") || text.Contains("imers") || text.Contains("agua") || text.Contains("água")))
            return StandardRole.LiquidThermometer;
        if (text.Contains("termohigro") || text.Contains("termo-higro"))
            return StandardRole.ThermoHygrometer;
        if (text.Contains("termo") && (text.Contains("amb") || text.Contains("sala")))
            return StandardRole.AmbientThermometer;
        if (text.Contains("higro")) return StandardRole.Hygrometer;
        if (text.Contains("baro") || text.Contains("press")) return StandardRole.Barometer;
        if (text.Contains("picno")) return StandardRole.Pycnometer;
        if (text.Contains("densim")) return StandardRole.Densimeter;
        return StandardRole.Balance;
    }

    // Internal DTO models for JSON deserialization
    private record ApiClientDto(Guid Id, string Name, string TaxId, string Email, string Phone, string? ContactPerson, string? Street, string? City, string? State, string? PostalCode, bool IsActive);
    private record ApiEquipmentSummaryDto(Guid Id, string LabTag, string Name, string Model, string Manufacturer, string SerialNumber, string InstrumentType, EquipmentStatus Status, string CurrentLocation, Guid ClientId, string? ClientName, decimal? NominalCapacity = null, string? Unit = null, decimal? RangeMin = null, decimal? RangeMax = null);
    private record ApiEquipmentDetailDto(Guid Id, string LabTag, string Name, string Model, string Manufacturer, string SerialNumber, string InstrumentType, decimal? NominalCapacity, string? Unit, Guid ClientId, string? ClientName, EquipmentStatus Status, decimal? RangeMin = null, decimal? RangeMax = null, string? CurrentLocation = null);
    private record ApiStandardEquipmentDto(Guid Id, string LabTag, string Name, string Model, string Manufacturer, string SerialNumber, string InstrumentType, string CurrentLocation, StandardEquipmentStatus Status, decimal Error, decimal Correction, decimal ExpandedUncertainty, decimal CoverageFactor, decimal Resolution, DateTime LastCalibrationDate, DateTime CalibrationDueDate, string CalibrationCertificateNumber, string CalibratingLaboratory, decimal RangeMin, decimal RangeMax, StandardRole? Role = null);
    private record ApiCalibrationResultDto(Guid Id, Guid EquipmentId, string? EquipmentLabTag, string? EquipmentName, decimal MeanError, decimal MaxAbsoluteError, decimal CombinedStandardUncertainty, decimal ExpandedUncertainty, decimal CoverageFactor, CalibrationResult Result, DateTime CalibrationDate, VolumeCalibrationResultDto? VolumeCalculationDetails = null, string? CertificateNumber = null);
    private record ApiCertificateSummaryDto(Guid Id, string CertificateNumber, Guid CalibrationRecordId, Guid EquipmentId, string EquipmentTag, string EquipmentName, string ClientName, CertificateStatus Status, DateTime CalibrationDate, DateTime? IssuedAt);
    private record ApiCertificateDetailDto(Guid Id, string CertificateNumber, Guid CalibrationRecordId, string EquipmentTag, string EquipmentName, string ClientName, CertificateStatus Status, DateTime CalibrationDate, DateTime NextCalibrationDue, DateTime? IssuedAt, decimal NominalVolumeMl, decimal SystematicErrorMl, decimal ExpandedUncertaintyMl, decimal CoverageFactor, CalibrationResult Result);
    private record ApiStandardCertDto(Guid Id, Guid StandardEquipmentId, string CertificateNumber, string CalibratingLaboratory, DateTime CalibrationDate, DateTime CalibrationDueDate, decimal CalibrationRangeMin, decimal CalibrationRangeMax, bool IsActive, int FitType, decimal? Slope, decimal? Intercept, decimal? R2);
}
