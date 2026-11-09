using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;

namespace TracLabs.Frontend.Services;

public sealed class MockMetrologyDataService : IMetrologyDataService
{
    private readonly List<ClientDto> _clients = new();
    private readonly List<ClientEquipmentDto> _equipment = new();
    private readonly List<StandardEquipmentDto> _standards = new();
    private readonly List<CalibrationRecordDto> _calibrations = new();
    private readonly List<CertificateDto> _certificates = new();
    private readonly List<CalibrationTimelineEventDto> _recentEvents = new();
    private readonly List<StandardCertificateDto> _standardCertificates = new();
    private readonly List<StandardCalibrationUsageDto> _calibrationUsages = new();
    private readonly object _lock = new();

    public static readonly Guid StandardBalanceId = Guid.Parse("00000001-0000-0000-0000-000000000001");
    public static readonly Guid StandardThermometerId = Guid.Parse("00000001-0000-0000-0000-000000000002");
    public static readonly Guid StandardBarometerId = Guid.Parse("00000001-0000-0000-0000-000000000003");
    public static readonly Guid StandardHygrometerId = Guid.Parse("00000001-0000-0000-0000-000000000004");
    public static readonly Guid StandardWeightsId = Guid.Parse("00000001-0000-0000-0000-000000000005");
    public static readonly Guid StandardAmbientThermometerId = Guid.Parse("00000001-0000-0000-0000-000000000006");
    public static readonly Guid StandardStandHygrometerId = Guid.Parse("00000001-0000-0000-0000-000000000007");
    public static readonly Guid StandardPycnometerId = Guid.Parse("00000001-0000-0000-0000-000000000008");
    public static readonly Guid StandardPycnometerExpiredId = Guid.Parse("00000001-0000-0000-0000-000000000009");

    public MockMetrologyDataService()
    {
        SeedData();
    }

    private void SeedData()
    {
        lock (_lock)
        {
            // 1. Clientes
            var cli1 = new ClientDto
            {
                Id = "cli-001",
                Name = "Indústria Química Brasileira S.A.",
                TaxId = "12.345.678/0001-90",
                Street = "Av. das Nações Unidas, 14200 - Torre A",
                City = "São Paulo",
                State = "SP",
                PostalCode = "04794-000",
                ContactPerson = "Dra. Helena Vasconcelos",
                Email = "helena.vasconcelos@iqb.com.br",
                Phone = "(11) 3456-7890",
                ActiveEquipmentCount = 12,
                IsActive = true
            };
            var cli2 = new ClientDto
            {
                Id = "cli-002",
                Name = "Petroquímica União S.A.",
                TaxId = "98.765.432/0001-10",
                Street = "Rodovia Dom Pedro I, km 122",
                City = "Campinas",
                State = "SP",
                PostalCode = "13080-000",
                ContactPerson = "Eng. Marcos Pontes Jr.",
                Email = "mpontes@petrouniao.com.br",
                Phone = "(19) 3890-1122",
                ActiveEquipmentCount = 8,
                IsActive = true
            };
            var cli3 = new ClientDto
            {
                Id = "cli-003",
                Name = "BioTech Diagnósticos Ltda.",
                TaxId = "33.444.555/0001-22",
                Street = "Rua Rui Barbosa, 850",
                City = "Ribeirão Preto",
                State = "SP",
                PostalCode = "14015-120",
                ContactPerson = "Dr. Fábio Medeiros",
                Email = "fmedeiros@biotechdiag.com.br",
                Phone = "(16) 3211-9988",
                ActiveEquipmentCount = 5,
                IsActive = true
            };
            _clients.AddRange(new[] { cli1, cli2, cli3 });

            // 2. Equipamentos dos Clientes
            var eq1 = new ClientEquipmentDto
            {
                Id = "eq-001",
                ClientId = cli1.Id,
                ClientName = cli1.Name,
                LabTag = "BAL-CLI-001",
                Description = "Balão Volumétrico 100 mL Rolha Poli",
                Manufacturer = "Brand GmbH",
                Model = "Blaubrand Classe A",
                SerialNumber = "SN-987654",
                InstrumentType = "Balão Volumétrico",
                NominalCapacity = 100.0m,
                Unit = "mL",
                AccuracyClass = "Classe A",
                MaxPermissibleError = 0.100m,
                CalibrationIntervalMonths = 12,
                Status = EquipmentStatus.InCalibration,
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-12),
                NextCalibrationDue = DateTime.UtcNow.AddDays(7)
            };
            var eq2 = new ClientEquipmentDto
            {
                Id = "eq-002",
                ClientId = cli1.Id,
                ClientName = cli1.Name,
                LabTag = "PIC-CLI-002",
                Description = "Picnômetro Gay-Lussac 50 mL com Termômetro",
                Manufacturer = "Vidrolabor",
                Model = "GL-50-PT",
                SerialNumber = "VL-2024-441",
                InstrumentType = "Picnômetro",
                NominalCapacity = 50.0m,
                Unit = "mL",
                AccuracyClass = "Classe A",
                MaxPermissibleError = 0.050m,
                CalibrationIntervalMonths = 12,
                Status = EquipmentStatus.Received,
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-11),
                NextCalibrationDue = DateTime.UtcNow.AddMonths(1)
            };
            var eq3 = new ClientEquipmentDto
            {
                Id = "eq-003",
                ClientId = cli2.Id,
                ClientName = cli2.Name,
                LabTag = "PIP-CLI-003",
                Description = "Micropipeta Monocanal Variável 100-1000 µL",
                Manufacturer = "Eppendorf",
                Model = "Research Plus",
                SerialNumber = "EP-887412",
                InstrumentType = "Micropipeta Monocanal",
                NominalCapacity = 1000.0m,
                Unit = "µL",
                AccuracyClass = "ISO 8655",
                MaxPermissibleError = 6.0m,
                CalibrationIntervalMonths = 6,
                Status = EquipmentStatus.Calibrated,
                LastCalibrationDate = DateTime.UtcNow.AddDays(-2),
                NextCalibrationDue = DateTime.UtcNow.AddMonths(6)
            };
            var eq4 = new ClientEquipmentDto
            {
                Id = "eq-004",
                ClientId = cli3.Id,
                ClientName = cli3.Name,
                LabTag = "BUR-CLI-004",
                Description = "Bureta Digital 50 mL com Torneira PTFE",
                Manufacturer = "Hirschmann",
                Model = "Solarus 50",
                SerialNumber = "HS-552199",
                InstrumentType = "Bureta Digital",
                NominalCapacity = 50.0m,
                Unit = "mL",
                AccuracyClass = "Classe A",
                MaxPermissibleError = 0.050m,
                CalibrationIntervalMonths = 12,
                Status = EquipmentStatus.Approved,
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-10),
                NextCalibrationDue = DateTime.UtcNow.AddMonths(2)
            };
            _equipment.AddRange(new[] { eq1, eq2, eq3, eq4 });

            // 3. Padrões do Laboratório (ISO 17025 6.4.13)
            var padBal = new StandardEquipmentDto
            {
                Id = "pad-bal-01",
                LabTag = "PAD-BAL-01",
                Name = "Balança Analítica Semimicro 220g",
                InstrumentType = "Balança Analítica",
                Role = StandardRole.Balance,
                Manufacturer = "Mettler Toledo",
                Model = "XPE205",
                SerialNumber = "MT-B11094",
                Resolution = 0.00001m,
                RangeMin = 0.001m,
                RangeMax = 220.0m,
                Status = StandardEquipmentStatus.Calibrated,
                CurrentLocation = "Sala de Calibração Volumétrica - Bancada 01",
                CalibrationCertificateNumber = "CAL-RBC-2025-8841",
                CalibratingLaboratory = "IPT - Instituto de Pesquisas Tecnológicas (RBC CAL-001)",
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-6),
                CalibrationDueDate = DateTime.UtcNow.AddMonths(18),
                ExpandedUncertainty = 0.00005m,
                CoverageFactor = 2.00m,
                Error = 0.00002m,
                Correction = -0.00002m
            };
            var padTerm = new StandardEquipmentDto
            {
                Id = "pad-term-01",
                LabTag = "PAD-TERM-01",
                Name = "Termômetro Digital de Referência PT-100",
                InstrumentType = "Termômetro de Imersão",
                Role = StandardRole.LiquidThermometer,
                Manufacturer = "Incoterm",
                Model = "PT-100 Precision Plus",
                SerialNumber = "INC-99412",
                Resolution = 0.01m,
                RangeMin = -10.0m,
                RangeMax = 110.0m,
                Status = StandardEquipmentStatus.Calibrated,
                CurrentLocation = "Sala de Calibração Volumétrica - Padrões",
                CalibrationCertificateNumber = "CAL-RBC-2025-1029",
                CalibratingLaboratory = "Ipem-SP (RBC CAL-007)",
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-4),
                CalibrationDueDate = DateTime.UtcNow.AddMonths(20),
                ExpandedUncertainty = 0.03m,
                CoverageFactor = 2.00m,
                Error = -0.02m,
                Correction = 0.02m
            };
            var padBar = new StandardEquipmentDto
            {
                Id = "pad-bar-01",
                LabTag = "PAD-BAR-01",
                Name = "Barômetro Digital de Precisão",
                InstrumentType = "Barômetro",
                Role = StandardRole.Barometer,
                Manufacturer = "Vaisala",
                Model = "PTB110",
                SerialNumber = "VS-778102",
                Resolution = 0.1m,
                RangeMin = 500.0m,
                RangeMax = 1100.0m,
                Status = StandardEquipmentStatus.Calibrated,
                CurrentLocation = "Sala Climática 01",
                CalibrationCertificateNumber = "CAL-RBC-2026-0312",
                CalibratingLaboratory = "INMETRO / DIMCI (RBC CAL-000)",
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-8),
                CalibrationDueDate = DateTime.UtcNow.AddMonths(16),
                ExpandedUncertainty = 0.20m,
                CoverageFactor = 2.00m,
                Error = 0.10m,
                Correction = -0.10m
            };
            var padHig = new StandardEquipmentDto
            {
                Id = "pad-hig-01",
                LabTag = "PAD-HIG-01",
                Name = "Termo-Higrômetro Digital de Sala",
                InstrumentType = "Termo-Higrômetro",
                Role = StandardRole.ThermoHygrometer,
                Manufacturer = "Testo",
                Model = "Testo 622",
                SerialNumber = "TS-441029",
                Resolution = 0.1m,
                RangeMin = 10.0m,
                RangeMax = 95.0m,
                Status = StandardEquipmentStatus.Calibrated,
                CurrentLocation = "Sala Climática 01",
                CalibrationCertificateNumber = "CAL-RBC-2025-4421",
                CalibratingLaboratory = "LabMetro Calibrações (RBC CAL-034)",
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-11).AddDays(-20),
                CalibrationDueDate = DateTime.UtcNow.AddDays(8), // CRÍTICO: vence em 8 dias!
                ExpandedUncertainty = 1.2m,
                CoverageFactor = 2.00m,
                Error = 0.5m,
                Correction = -0.5m
            };
            var padPes = new StandardEquipmentDto
            {
                Id = "pad-pes-01",
                LabTag = "PAD-PES-01",
                Name = "Jogo de Pesos Padrão Inox Classe E2 (1mg - 200g)",
                InstrumentType = "Pesos Padrão",
                Role = StandardRole.ReferenceWeights,
                Manufacturer = "Häfner Gewichte",
                Model = "E2 Knob Weights",
                SerialNumber = "HAF-2022-81",
                Resolution = 0.000001m,
                RangeMin = 0.001m,
                RangeMax = 200.0m,
                Status = StandardEquipmentStatus.Calibrated,
                CurrentLocation = "Cofre de Padrões RBC",
                CalibrationCertificateNumber = "CAL-RBC-2025-0019",
                CalibratingLaboratory = "IPT - Divisão de Mecânica (RBC CAL-001)",
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-23),
                CalibrationDueDate = DateTime.UtcNow.AddDays(22), // ALERTA: vence em 22 dias!
                ExpandedUncertainty = 0.00003m,
                CoverageFactor = 2.00m,
                Error = 0.00001m,
                Correction = -0.00001m
            };

            var padTermAmb = new StandardEquipmentDto
            {
                Id = "pad-teramb-01",
                LabTag = "PAD-TERAMB-01",
                Name = "Termômetro Ambiente Digital de Alta Precisão",
                InstrumentType = "Termômetro de Ambiente",
                Role = StandardRole.AmbientThermometer,
                Manufacturer = "Fluke Calibration",
                Model = "Fluke 1523",
                SerialNumber = "FLK-771290",
                Resolution = 0.005m,
                RangeMin = 0.0m,
                RangeMax = 50.0m,
                Status = StandardEquipmentStatus.Calibrated,
                CurrentLocation = "Sala Climática 01 - Monitoramento",
                CalibrationCertificateNumber = "CAL-RBC-2025-3312",
                CalibratingLaboratory = "IPT - Divisão de Térmica (RBC CAL-001)",
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-7),
                CalibrationDueDate = DateTime.UtcNow.AddMonths(17),
                ExpandedUncertainty = 0.02m,
                CoverageFactor = 2.00m,
                Error = 0.01m,
                Correction = -0.01m
            };

            var padHigStand = new StandardEquipmentDto
            {
                Id = "pad-hig-02",
                LabTag = "PAD-HIG-02",
                Name = "Higrômetro Digital de Bancada",
                InstrumentType = "Higrômetro",
                Role = StandardRole.Hygrometer,
                Manufacturer = "Rotronic",
                Model = "HygroPalm HP32",
                SerialNumber = "ROT-559102",
                Resolution = 0.1m,
                RangeMin = 0.0m,
                RangeMax = 100.0m,
                Status = StandardEquipmentStatus.Calibrated,
                CurrentLocation = "Sala Climática 01 - Estação B",
                CalibrationCertificateNumber = "CAL-RBC-2025-7890",
                CalibratingLaboratory = "Ipem-SP (RBC CAL-007)",
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-6),
                CalibrationDueDate = DateTime.UtcNow.AddMonths(18),
                ExpandedUncertainty = 1.0m,
                CoverageFactor = 2.00m,
                Error = 0.3m,
                Correction = -0.3m
            };

            var padPic = new StandardEquipmentDto
            {
                Id = "pad-pic-01",
                LabTag = "PAD-PIC-01",
                Name = "Picnômetro Gay-Lussac 50 mL com Termômetro",
                InstrumentType = "Picnômetro",
                Role = StandardRole.Pycnometer,
                Manufacturer = "Vidrolabor",
                Model = "Gay-Lussac GL-50",
                SerialNumber = "PIC-RBC-2024-09",
                Resolution = 0.001m,
                RangeMin = 49.0m,
                RangeMax = 51.0m,
                Status = StandardEquipmentStatus.Calibrated,
                CurrentLocation = "Sala de Calibração Volumétrica - Armário de Padrões A",
                CalibrationCertificateNumber = "CAL-RBC-2025-5519",
                CalibratingLaboratory = "Inmetro / LabMetro (RBC CAL-018)",
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-5),
                CalibrationDueDate = DateTime.UtcNow.AddMonths(19),
                ExpandedUncertainty = 0.005m,
                CoverageFactor = 2.00m,
                Error = 0.002m,
                Correction = -0.002m
            };

            var padPicExpired = new StandardEquipmentDto
            {
                Id = "pad-pic-02",
                LabTag = "PAD-PIC-02",
                Name = "Picnômetro Gay-Lussac 25 mL",
                InstrumentType = "Picnômetro",
                Role = StandardRole.Pycnometer,
                Manufacturer = "Vidrolabor",
                Model = "GL-25",
                SerialNumber = "VL-2019-112",
                Resolution = 0.001m,
                RangeMin = 24.0m,
                RangeMax = 26.0m,
                Status = StandardEquipmentStatus.PendingCalibration,
                CurrentLocation = "Quarentena de Padrões",
                CalibrationCertificateNumber = "CAL-RBC-2023-1102",
                CalibratingLaboratory = "Inmetro / LabMetro (RBC CAL-018)",
                LastCalibrationDate = DateTime.UtcNow.AddMonths(-25),
                CalibrationDueDate = DateTime.UtcNow.AddDays(-10), // Vencido para teste negativo de validação
                ExpandedUncertainty = 0.005m,
                CoverageFactor = 2.00m,
                Error = 0.004m,
                Correction = -0.004m
            };

            _standards.AddRange(new[] { padBal, padTerm, padBar, padHig, padPes, padTermAmb, padHigStand, padPic, padPicExpired });

            // 4. Calibração Volumétrica Pré-populada (Planilha Real 11 Repetições)
            var calib = new CalibrationRecordDto
            {
                Id = "cal-2026-001",
                EquipmentId = eq1.Id,
                EquipmentTag = eq1.LabTag,
                EquipmentDescription = eq1.Description,
                ClientId = cli1.Id,
                ClientName = cli1.Name,
                NominalCapacity = 100.0m,
                Unit = "mL",
                AccuracyClass = "Classe A",
                MaxPermissibleError = 0.100m,
                Material = VolumetricMaterial.SodaLimeGlass,
                StandardMethod = "ABNT NBR ISO 4787:2013",
                WaterTemperatureCelsius = 20.03m,
                AmbientTemperatureCelsius = 20.20m,
                AtmosphericPressureHpa = 1018.0m,
                RelativeHumidityPercent = 68.0m,
                BalanceStandardId = padBal.Id,
                ThermometerStandardId = padTerm.Id,
                BarometerStandardId = padBar.Id,
                HygrometerStandardId = padHig.Id,
                Measurements = MetrologyCalculations.GetBenchmark100MlPoints(),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                TechnicianId = "usr-02",
                TechnicianName = "Carlos Eduardo Silva"
            };
            MetrologyCalculations.ComputeCalibrationResults(calib, calib.NominalCapacity, calib.MaxPermissibleError);
            _calibrations.Add(calib);

            // 5. Certificados de Demonstração
            var cert1 = new CertificateDto
            {
                Id = "cert-001",
                CertificateNumber = "CERT-2026-00042",
                CalibrationRecordId = calib.Id,
                EquipmentTag = calib.EquipmentTag,
                EquipmentDescription = calib.EquipmentDescription,
                ClientName = cli1.Name,
                ClientTaxId = cli1.TaxId,
                ClientAddress = $"{cli1.Street}, {cli1.City} - {cli1.State}",
                IssueDate = DateTime.UtcNow,
                CalibrationDate = calib.CreatedAt,
                NextCalibrationDueDate = DateTime.UtcNow.AddYears(1),
                Status = CertificateStatus.Draft, // Inicialmente Draft para permitir testar workflow
                Result = CalibrationResult.Conforming,
                NominalVolume = 100.0m,
                MeanVolumeV20 = calib.MeanV20,
                SystematicError = calib.SystematicError,
                ExpandedUncertainty = calib.ExpandedUncertainty,
                CoverageFactor = calib.CoverageFactor,
                ConfidenceLevel = "95.45%",
                MaxPermissibleError = calib.MaxPermissibleError,
                StandardsUsed = new List<StandardEquipmentDto> { padBal, padTerm, padBar, padHig },
                TechnicianName = "Carlos Eduardo Silva",
                TechnicianRegistration = "CFT/CRT 198420-SP",
                SupervisorName = "Dr. Alexandre Morales",
                SupervisorRegistration = "CRQ IV 04218901",
                QrCodeValidationUrl = "https://traclabs.com.br/validar?cert=CERT-2026-00042&hash=a8f9b2c3d4e5f60718293a4b5c6d7e8f",
                Sha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
            };

            var cert2 = new CertificateDto
            {
                Id = "cert-002",
                CertificateNumber = "CERT-2026-00041",
                CalibrationRecordId = Guid.NewGuid().ToString(),
                EquipmentTag = eq3.LabTag,
                EquipmentDescription = eq3.Description,
                ClientName = cli2.Name,
                ClientTaxId = cli2.TaxId,
                ClientAddress = $"{cli2.Street}, {cli2.City} - {cli2.State}",
                IssueDate = DateTime.UtcNow.AddDays(-2),
                CalibrationDate = DateTime.UtcNow.AddDays(-3),
                NextCalibrationDueDate = DateTime.UtcNow.AddMonths(6),
                Status = CertificateStatus.Issued,
                Result = CalibrationResult.Conforming,
                NominalVolume = 1000.0m,
                MeanVolumeV20 = 998.42m,
                SystematicError = -1.58m,
                ExpandedUncertainty = 1.12m,
                CoverageFactor = 2.00m,
                ConfidenceLevel = "95.45%",
                MaxPermissibleError = 6.00m,
                StandardsUsed = new List<StandardEquipmentDto> { padBal, padTerm, padBar },
                TechnicianName = "Carlos Eduardo Silva",
                TechnicianRegistration = "CFT/CRT 198420-SP",
                SupervisorName = "Dr. Alexandre Morales",
                SupervisorRegistration = "CRQ IV 04218901",
                QrCodeValidationUrl = "https://traclabs.com.br/validar?cert=CERT-2026-00041&hash=b7e2d1f9a8c43029184756abcdef1234",
                Sha256Hash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918",
                ApprovalDate = DateTime.UtcNow.AddDays(-2)
            };

            var cert3 = new CertificateDto
            {
                Id = "cert-003",
                CertificateNumber = "CERT-2026-00039",
                CalibrationRecordId = Guid.NewGuid().ToString(),
                EquipmentTag = eq2.LabTag,
                EquipmentDescription = eq2.Description,
                ClientName = cli1.Name,
                ClientTaxId = cli1.TaxId,
                ClientAddress = $"{cli1.Street}, {cli1.City} - {cli1.State}",
                IssueDate = DateTime.UtcNow.AddDays(-15),
                CalibrationDate = DateTime.UtcNow.AddDays(-16),
                NextCalibrationDueDate = DateTime.UtcNow.AddMonths(12),
                Status = CertificateStatus.Superseded,
                Result = CalibrationResult.NonConforming,
                NominalVolume = 50.0m,
                MeanVolumeV20 = 49.882m,
                SystematicError = -0.118m,
                ExpandedUncertainty = 0.015m,
                CoverageFactor = 2.00m,
                ConfidenceLevel = "95.45%",
                MaxPermissibleError = 0.050m,
                StandardsUsed = new List<StandardEquipmentDto> { padBal, padTerm },
                TechnicianName = "Carlos Eduardo Silva",
                TechnicianRegistration = "CFT/CRT 198420-SP",
                SupervisorName = "Dr. Alexandre Morales",
                SupervisorRegistration = "CRQ IV 04218901",
                QrCodeValidationUrl = "https://traclabs.com.br/validar?cert=CERT-2026-00039&hash=0192837465abcdef9876543210fedcba",
                Sha256Hash = "ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb",
                CancellationReason = "Erro na identificação da tara do termômetro acoplado ao picnômetro. Cancelado conforme ISO 17025 item 7.8.8.2."
            };

            _certificates.AddRange(new[] { cert1, cert2, cert3 });

            // 6. Linha do Tempo de Eventos Recentes
            _recentEvents.AddRange(new[]
            {
                new CalibrationTimelineEventDto
                {
                    TimeString = "Hoje, 14:32",
                    Title = "Ensaio Gravimétrico Finalizado (11 repetições)",
                    EquipmentTag = "BAL-CLI-001",
                    ClientName = "Indústria Química Brasileira",
                    OperatorName = "Carlos Eduardo Silva",
                    Result = CalibrationResult.Conforming,
                    Details = "V20 = 99.9431 mL | Es = -0.0569 mL | U = 0.0077 mL (k=2.02). Dentro da tolerância EMA ±0.100 mL."
                },
                new CalibrationTimelineEventDto
                {
                    TimeString = "Hoje, 11:15",
                    Title = "Certificado Emitido e Assinado Digitalmente",
                    EquipmentTag = "PIP-CLI-003",
                    ClientName = "Petroquímica União S.A.",
                    OperatorName = "Dr. Alexandre Morales",
                    Result = CalibrationResult.Conforming,
                    Details = "Certificado CERT-2026-00041 emitido com chave SHA-256 e liberado para o cliente."
                },
                new CalibrationTimelineEventDto
                {
                    TimeString = "Ontem, 16:40",
                    Title = "Novo Padrão RBC Cadastrado",
                    EquipmentTag = "PAD-BAL-01",
                    ClientName = "Laboratório Central",
                    OperatorName = "Dra. Mariana Costa",
                    Result = CalibrationResult.InformationOnly,
                    Details = "Balança Mettler Toledo XPE205 cadastrada e integrada ao inventário ativo."
                },
                new CalibrationTimelineEventDto
                {
                    TimeString = "04/11, 09:20",
                    Title = "Não Conformidade Detectada no Ensaio",
                    EquipmentTag = "PIC-CLI-002",
                    ClientName = "BioTech Diagnósticos",
                    OperatorName = "Carlos Eduardo Silva",
                    Result = CalibrationResult.NonConforming,
                    Details = "Desvio de -0.118 mL excede a tolerância máxima admissível (±0.050 mL). Certificado cancelado."
                }
            });

            // 7. Certificados dos Padrões (ISO/IEC 17025 6.4.13)
            _standardCertificates.AddRange(new[]
            {
                // PAD-BAL-01
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0001-0000-0000-000000000001"),
                    StandardEquipmentId = StandardBalanceId,
                    CertificateNumber = "CAL-RBC-2025-8841",
                    CalibratingLaboratory = "IPT - Instituto de Pesquisas Tecnológicas (RBC CAL-001)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-6),
                    CalibrationDueDate = DateTime.UtcNow.AddMonths(18),
                    CalibrationRangeMin = 0.001m,
                    CalibrationRangeMax = 220.0m,
                    IsActive = true,
                    Error = 0.00002m,
                    Correction = -0.00002m,
                    ExpandedUncertainty = 0.00005m,
                    CoverageFactor = 2.00m,
                    CertificateHash = "a1b2c3d4e5f67890123456789abcdef0123456789abcdef0123456789abcdef0"
                },
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0001-0000-0000-000000000002"),
                    StandardEquipmentId = StandardBalanceId,
                    CertificateNumber = "CAL-RBC-2023-7120",
                    CalibratingLaboratory = "IPT - Instituto de Pesquisas Tecnológicas (RBC CAL-001)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-30),
                    CalibrationDueDate = DateTime.UtcNow.AddMonths(-6),
                    CalibrationRangeMin = 0.001m,
                    CalibrationRangeMax = 220.0m,
                    IsActive = false,
                    Error = 0.00003m,
                    Correction = -0.00003m,
                    ExpandedUncertainty = 0.00006m,
                    CoverageFactor = 2.00m
                },
                // PAD-TERM-01
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0002-0000-0000-000000000001"),
                    StandardEquipmentId = StandardThermometerId,
                    CertificateNumber = "CAL-RBC-2025-1029",
                    CalibratingLaboratory = "Ipem-SP (RBC CAL-007)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-4),
                    CalibrationDueDate = DateTime.UtcNow.AddMonths(20),
                    CalibrationRangeMin = -10.0m,
                    CalibrationRangeMax = 110.0m,
                    IsActive = true,
                    Error = -0.02m,
                    Correction = 0.02m,
                    ExpandedUncertainty = 0.03m,
                    CoverageFactor = 2.00m
                },
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0002-0000-0000-000000000002"),
                    StandardEquipmentId = StandardThermometerId,
                    CertificateNumber = "CAL-RBC-2023-0881",
                    CalibratingLaboratory = "Ipem-SP (RBC CAL-007)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-28),
                    CalibrationDueDate = DateTime.UtcNow.AddMonths(-4),
                    CalibrationRangeMin = -10.0m,
                    CalibrationRangeMax = 110.0m,
                    IsActive = false,
                    Error = -0.015m,
                    Correction = 0.015m,
                    ExpandedUncertainty = 0.035m,
                    CoverageFactor = 2.00m
                },
                // PAD-BAR-01
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0003-0000-0000-000000000001"),
                    StandardEquipmentId = StandardBarometerId,
                    CertificateNumber = "CAL-RBC-2026-0312",
                    CalibratingLaboratory = "INMETRO / DIMCI (RBC CAL-000)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-8),
                    CalibrationDueDate = DateTime.UtcNow.AddMonths(16),
                    CalibrationRangeMin = 500.0m,
                    CalibrationRangeMax = 1100.0m,
                    IsActive = true,
                    Error = 0.10m,
                    Correction = -0.10m,
                    ExpandedUncertainty = 0.20m,
                    CoverageFactor = 2.00m
                },
                // PAD-HIG-01 (Vence em 8 dias)
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0004-0000-0000-000000000001"),
                    StandardEquipmentId = StandardHygrometerId,
                    CertificateNumber = "CAL-RBC-2025-4421",
                    CalibratingLaboratory = "LabMetro Calibrações (RBC CAL-034)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-11).AddDays(-20),
                    CalibrationDueDate = DateTime.UtcNow.AddDays(8),
                    CalibrationRangeMin = 10.0m,
                    CalibrationRangeMax = 95.0m,
                    IsActive = true,
                    Error = 0.5m,
                    Correction = -0.5m,
                    ExpandedUncertainty = 1.2m,
                    CoverageFactor = 2.00m
                },
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0004-0000-0000-000000000002"),
                    StandardEquipmentId = StandardHygrometerId,
                    CertificateNumber = "CAL-RBC-2024-2190",
                    CalibratingLaboratory = "LabMetro Calibrações (RBC CAL-034)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-24),
                    CalibrationDueDate = DateTime.UtcNow.AddMonths(-12),
                    CalibrationRangeMin = 10.0m,
                    CalibrationRangeMax = 95.0m,
                    IsActive = false,
                    Error = 0.4m,
                    Correction = -0.4m,
                    ExpandedUncertainty = 1.1m,
                    CoverageFactor = 2.00m
                },
                // PAD-PES-01 (Vence em 22 dias)
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0005-0000-0000-000000000001"),
                    StandardEquipmentId = StandardWeightsId,
                    CertificateNumber = "CAL-RBC-2025-0019",
                    CalibratingLaboratory = "IPT - Divisão de Mecânica (RBC CAL-001)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-23),
                    CalibrationDueDate = DateTime.UtcNow.AddDays(22),
                    CalibrationRangeMin = 0.001m,
                    CalibrationRangeMax = 200.0m,
                    IsActive = true,
                    Error = 0.00001m,
                    Correction = -0.00001m,
                    ExpandedUncertainty = 0.00003m,
                    CoverageFactor = 2.00m
                },
                // PAD-TERAMB-01
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0006-0000-0000-000000000001"),
                    StandardEquipmentId = StandardAmbientThermometerId,
                    CertificateNumber = "CAL-RBC-2025-3312",
                    CalibratingLaboratory = "IPT - Divisão de Térmica (RBC CAL-001)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-7),
                    CalibrationDueDate = DateTime.UtcNow.AddMonths(17),
                    CalibrationRangeMin = 0.0m,
                    CalibrationRangeMax = 50.0m,
                    IsActive = true,
                    Error = 0.01m,
                    Correction = -0.01m,
                    ExpandedUncertainty = 0.02m,
                    CoverageFactor = 2.00m
                },
                // PAD-HIG-02
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0007-0000-0000-000000000001"),
                    StandardEquipmentId = StandardStandHygrometerId,
                    CertificateNumber = "CAL-RBC-2025-7890",
                    CalibratingLaboratory = "Ipem-SP (RBC CAL-007)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-6),
                    CalibrationDueDate = DateTime.UtcNow.AddMonths(18),
                    CalibrationRangeMin = 0.0m,
                    CalibrationRangeMax = 100.0m,
                    IsActive = true,
                    Error = 0.3m,
                    Correction = -0.3m,
                    ExpandedUncertainty = 1.0m,
                    CoverageFactor = 2.00m
                },
                // PAD-PIC-01
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0008-0000-0000-000000000001"),
                    StandardEquipmentId = StandardPycnometerId,
                    CertificateNumber = "CAL-RBC-2025-5519",
                    CalibratingLaboratory = "Inmetro / LabMetro (RBC CAL-018)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-5),
                    CalibrationDueDate = DateTime.UtcNow.AddMonths(19),
                    CalibrationRangeMin = 49.0m,
                    CalibrationRangeMax = 51.0m,
                    IsActive = true,
                    Error = 0.002m,
                    Correction = -0.002m,
                    ExpandedUncertainty = 0.005m,
                    CoverageFactor = 2.00m
                },
                // PAD-PIC-02 (Expirado)
                new StandardCertificateDto
                {
                    Id = Guid.Parse("10000001-0009-0000-0000-000000000001"),
                    StandardEquipmentId = StandardPycnometerExpiredId,
                    CertificateNumber = "CAL-RBC-2023-1102",
                    CalibratingLaboratory = "Inmetro / LabMetro (RBC CAL-018)",
                    CalibrationDate = DateTime.UtcNow.AddMonths(-25),
                    CalibrationDueDate = DateTime.UtcNow.AddDays(-10),
                    CalibrationRangeMin = 24.0m,
                    CalibrationRangeMax = 26.0m,
                    IsActive = true,
                    Error = 0.004m,
                    Correction = -0.004m,
                    ExpandedUncertainty = 0.005m,
                    CoverageFactor = 2.00m
                }
            });

            // 8. Histórico de Utilização dos Padrões (ISO/IEC 17025 6.4.13)
            _calibrationUsages.AddRange(new[]
            {
                // PAD-BAL-01
                new StandardCalibrationUsageDto
                {
                    StandardId = StandardBalanceId,
                    CalibrationId = Guid.Parse("20000001-0000-0000-0000-000000000001"),
                    CalibrationDate = DateTime.UtcNow.AddDays(-1),
                    CustomerName = "Indústria Química Brasileira S.A.",
                    EquipmentTag = "BAL-CLI-001",
                    EquipmentDescription = "Balão Volumétrico 100 mL Rolha Poli",
                    NominalVolume = 100.0m,
                    Unit = "mL",
                    TechnicianName = "Carlos Eduardo Silva",
                    CertificateNumberIssued = "CERT-2026-00042",
                    RoleUsed = StandardRole.Balance,
                    OperatingValue = 99.9431m,
                    CertificateStatus = "Emitido"
                },
                new StandardCalibrationUsageDto
                {
                    StandardId = StandardBalanceId,
                    CalibrationId = Guid.Parse("20000001-0000-0000-0000-000000000002"),
                    CalibrationDate = DateTime.UtcNow.AddDays(-2),
                    CustomerName = "Petroquímica União S.A.",
                    EquipmentTag = "PIP-CLI-003",
                    EquipmentDescription = "Micropipeta Monocanal Variável 100-1000 µL",
                    NominalVolume = 1000.0m,
                    Unit = "µL",
                    TechnicianName = "Carlos Eduardo Silva",
                    CertificateNumberIssued = "CERT-2026-00041",
                    RoleUsed = StandardRole.Balance,
                    OperatingValue = 998.42m,
                    CertificateStatus = "Emitido"
                },
                new StandardCalibrationUsageDto
                {
                    StandardId = StandardBalanceId,
                    CalibrationId = Guid.Parse("20000001-0000-0000-0000-000000000003"),
                    CalibrationDate = DateTime.UtcNow.AddDays(-5),
                    CustomerName = "BioTech Diagnósticos Ltda.",
                    EquipmentTag = "BUR-CLI-004",
                    EquipmentDescription = "Bureta Digital 50 mL com Torneira PTFE",
                    NominalVolume = 50.0m,
                    Unit = "mL",
                    TechnicianName = "Carlos Eduardo Silva",
                    CertificateNumberIssued = "CERT-2026-00040",
                    RoleUsed = StandardRole.Balance,
                    OperatingValue = 49.982m,
                    CertificateStatus = "Emitido"
                },
                new StandardCalibrationUsageDto
                {
                    StandardId = StandardBalanceId,
                    CalibrationId = Guid.Parse("20000001-0000-0000-0000-000000000004"),
                    CalibrationDate = DateTime.UtcNow.AddMonths(-1).AddDays(-10),
                    CustomerName = "Indústria Química Brasileira S.A.",
                    EquipmentTag = "BAL-CLI-005",
                    EquipmentDescription = "Balão Volumétrico 250 mL Classe A",
                    NominalVolume = 250.0m,
                    Unit = "mL",
                    TechnicianName = "Mariana Costa",
                    CertificateNumberIssued = "CERT-2026-00015",
                    RoleUsed = StandardRole.Balance,
                    OperatingValue = 249.910m,
                    CertificateStatus = "Emitido"
                },
                // PAD-TERM-01
                new StandardCalibrationUsageDto
                {
                    StandardId = StandardThermometerId,
                    CalibrationId = Guid.Parse("20000001-0000-0000-0000-000000000001"),
                    CalibrationDate = DateTime.UtcNow.AddDays(-1),
                    CustomerName = "Indústria Química Brasileira S.A.",
                    EquipmentTag = "BAL-CLI-001",
                    EquipmentDescription = "Balão Volumétrico 100 mL Rolha Poli",
                    NominalVolume = 100.0m,
                    Unit = "mL",
                    TechnicianName = "Carlos Eduardo Silva",
                    CertificateNumberIssued = "CERT-2026-00042",
                    RoleUsed = StandardRole.LiquidThermometer,
                    OperatingValue = 20.03m,
                    CertificateStatus = "Emitido"
                },
                new StandardCalibrationUsageDto
                {
                    StandardId = StandardThermometerId,
                    CalibrationId = Guid.Parse("20000001-0000-0000-0000-000000000002"),
                    CalibrationDate = DateTime.UtcNow.AddDays(-2),
                    CustomerName = "Petroquímica União S.A.",
                    EquipmentTag = "PIP-CLI-003",
                    EquipmentDescription = "Micropipeta Monocanal Variável 100-1000 µL",
                    NominalVolume = 1000.0m,
                    Unit = "µL",
                    TechnicianName = "Carlos Eduardo Silva",
                    CertificateNumberIssued = "CERT-2026-00041",
                    RoleUsed = StandardRole.LiquidThermometer,
                    OperatingValue = 20.50m,
                    CertificateStatus = "Emitido"
                },
                // PAD-BAR-01
                new StandardCalibrationUsageDto
                {
                    StandardId = StandardBarometerId,
                    CalibrationId = Guid.Parse("20000001-0000-0000-0000-000000000001"),
                    CalibrationDate = DateTime.UtcNow.AddDays(-1),
                    CustomerName = "Indústria Química Brasileira S.A.",
                    EquipmentTag = "BAL-CLI-001",
                    EquipmentDescription = "Balão Volumétrico 100 mL Rolha Poli",
                    NominalVolume = 100.0m,
                    Unit = "mL",
                    TechnicianName = "Carlos Eduardo Silva",
                    CertificateNumberIssued = "CERT-2026-00042",
                    RoleUsed = StandardRole.Barometer,
                    OperatingValue = 1018.0m,
                    CertificateStatus = "Emitido"
                },
                // PAD-HIG-01
                new StandardCalibrationUsageDto
                {
                    StandardId = StandardHygrometerId,
                    CalibrationId = Guid.Parse("20000001-0000-0000-0000-000000000001"),
                    CalibrationDate = DateTime.UtcNow.AddDays(-1),
                    CustomerName = "Indústria Química Brasileira S.A.",
                    EquipmentTag = "BAL-CLI-001",
                    EquipmentDescription = "Balão Volumétrico 100 mL Rolha Poli",
                    NominalVolume = 100.0m,
                    Unit = "mL",
                    TechnicianName = "Carlos Eduardo Silva",
                    CertificateNumberIssued = "CERT-2026-00042",
                    RoleUsed = StandardRole.ThermoHygrometer,
                    OperatingValue = 68.0m,
                    CertificateStatus = "Emitido"
                },
                // PAD-PES-01
                new StandardCalibrationUsageDto
                {
                    StandardId = StandardWeightsId,
                    CalibrationId = Guid.Parse("20000001-0000-0000-0000-000000000005"),
                    CalibrationDate = DateTime.UtcNow.AddDays(-7),
                    CustomerName = "Laboratório Central TracLabs (Controle Interno)",
                    EquipmentTag = "PAD-BAL-01",
                    EquipmentDescription = "Balança Analítica Semimicro 220g",
                    NominalVolume = 100.0m,
                    Unit = "g",
                    TechnicianName = "Mariana Costa",
                    CertificateNumberIssued = "VERIF-2026-0012",
                    RoleUsed = StandardRole.ReferenceWeights,
                    OperatingValue = 100.0000m,
                    CertificateStatus = "Emitido"
                }
            });
        }
    }

    public Task<List<ClientDto>> GetClientsAsync(string? search = null)
    {
        lock (_lock)
        {
            var query = _clients.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         c.TaxId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         c.City.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult(query.OrderBy(c => c.Name).ToList());
        }
    }

    public Task<ClientDto> SaveClientAsync(ClientDto client)
    {
        lock (_lock)
        {
            var existing = _clients.FirstOrDefault(c => c.Id == client.Id);
            if (existing != null)
            {
                _clients.Remove(existing);
            }
            _clients.Add(client);
            return Task.FromResult(client);
        }
    }

    public Task<List<ClientEquipmentDto>> GetClientEquipmentAsync(string? search = null, string? clientId = null)
    {
        lock (_lock)
        {
            var query = _equipment.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                query = query.Where(e => e.ClientId == clientId);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.LabTag.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         e.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         e.ClientName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult(query.OrderBy(e => e.LabTag).ToList());
        }
    }

    public Task<ClientEquipmentDto?> GetClientEquipmentByIdAsync(string id)
    {
        lock (_lock)
        {
            var item = _equipment.FirstOrDefault(e => e.Id == id || e.LabTag == id);
            return Task.FromResult(item);
        }
    }

    public Task<ClientEquipmentDto> UpdateEquipmentStatusAsync(string equipmentId, EquipmentStatus newStatus)
    {
        lock (_lock)
        {
            var eq = _equipment.FirstOrDefault(e => e.Id == equipmentId || e.LabTag == equipmentId);
            if (eq != null)
            {
                eq.Status = newStatus;
                return Task.FromResult(eq);
            }
            throw new KeyNotFoundException($"Equipamento {equipmentId} não encontrado no mock.");
        }
    }

    public Task<ClientEquipmentDto> SaveClientEquipmentAsync(ClientEquipmentDto equipment)
    {
        lock (_lock)
        {
            var existing = _equipment.FirstOrDefault(e => e.Id == equipment.Id);
            if (existing != null)
            {
                _equipment.Remove(existing);
            }
            _equipment.Add(equipment);
            return Task.FromResult(equipment);
        }
    }

    public Task<List<StandardEquipmentDto>> GetStandardEquipmentAsync(string? search = null, StandardRole? role = null)
    {
        lock (_lock)
        {
            var query = _standards.AsEnumerable();
            if (role.HasValue)
            {
                query = query.Where(s => s.Role == role.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.LabTag.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         s.CalibrationCertificateNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult(query.OrderBy(s => s.LabTag).ToList());
        }
    }

    public Task<List<StandardEquipmentDto>> GetExpiringStandardsAsync(int daysThreshold = 30)
    {
        lock (_lock)
        {
            var list = _standards
                .Where(s => s.DaysUntilExpiry <= daysThreshold)
                .OrderBy(s => s.DaysUntilExpiry)
                .ToList();
            return Task.FromResult(list);
        }
    }

    public Task<StandardEquipmentDto> SaveStandardEquipmentAsync(StandardEquipmentDto standard)
    {
        lock (_lock)
        {
            var existing = _standards.FirstOrDefault(s => s.Id == standard.Id);
            if (existing != null)
            {
                _standards.Remove(existing);
            }
            _standards.Add(standard);
            return Task.FromResult(standard);
        }
    }

    public Task<DashboardMetricsDto> GetDashboardMetricsAsync()
    {
        lock (_lock)
        {
            var metrics = new DashboardMetricsDto
            {
                ActiveCalibrationsCount = _equipment.Count(e => e.Status == EquipmentStatus.InCalibration || e.Status == EquipmentStatus.Approved),
                IssuedCertificatesMonthCount = _certificates.Count(c => c.Status == CertificateStatus.Issued),
                ExpiringStandardsCount = _standards.Count(s => s.DaysUntilExpiry <= 30),
                NonConformingCount = _certificates.Count(c => c.Result == CalibrationResult.NonConforming) + _equipment.Count(e => e.Status == EquipmentStatus.NonConforming)
            };
            return Task.FromResult(metrics);
        }
    }

    public Task<List<CalibrationTimelineEventDto>> GetRecentEventsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_recentEvents.ToList());
        }
    }

    public Task<VolumeCalibrationResultDto?> CalculateVolumeCalibrationAsync(VolumeCalibrationCalculationRequest request)
    {
        var points = request.Measurements ?? new List<VolumeMeasurementInputDto>();
        var nominal = request.NominalVolumeMl > 0 ? request.NominalVolumeMl : 100.0m;

        var tempCal = new CalibrationRecordDto
        {
            NominalCapacity = nominal,
            Material = request.Material,
            WaterDensityGcm3 = request.WaterDensityGcm3,
            Measurements = points.Select(p => new VolumeMeasurementPointDto
            {
                RunNumber = p.RunNumber,
                FullMassGrams = p.FullMassGrams,
                EmptyMassGrams = p.EmptyMassGrams,
                WaterTemperatureCelsius = p.WaterTemperatureCelsius,
                AmbientTemperatureCelsius = p.AmbientTemperatureCelsius,
                AtmosphericPressureHpa = p.AtmosphericPressureHpa,
                RelativeHumidityPercent = p.RelativeHumidityPercent
            }).ToList()
        };

        MetrologyCalculations.ComputeCalibrationResults(tempCal, nominal, 0.100m);

        var result = new VolumeCalibrationResultDto
        {
            NominalVolumeMl = nominal,
            PointsCount = points.Count,
            MeanVolumeV20Ml = tempCal.MeanV20,
            SystematicErrorMl = tempCal.SystematicError,
            CorrectionMl = -tempCal.SystematicError,
            WaterDensityGramsPerCm3 = tempCal.CalculatedWaterDensity,
            AirDensityKgPerM3 = tempCal.CalculatedAirDensity,
            AirDensityGramsPerCm3 = tempCal.CalculatedAirDensity / 1000m,
            RepeatabilityStandardDeviationMl = tempCal.SampleStdDev,
            RepeatabilityUncertaintyMl = tempCal.RepeatabilityUncertainty,
            CombinedStandardUncertaintyMl = tempCal.CombinedUncertainty,
            EffectiveDegreesOfFreedom = 10,
            CoverageFactor = tempCal.CoverageFactor,
            ExpandedUncertaintyMl = tempCal.ExpandedUncertainty,
            RelativeExpandedUncertaintyPercent = tempCal.RelativeSystematicErrorPercent,
            CalculatedPoints = tempCal.Measurements.Select(m => new CalculatedPointDto
            {
                RunNumber = m.RunNumber,
                FullMassGrams = m.FullMassGrams,
                EmptyMassGrams = m.EmptyMassGrams,
                WaterTemperatureCelsius = m.WaterTemperatureCelsius,
                AmbientTemperatureCelsius = m.AmbientTemperatureCelsius,
                AtmosphericPressureHpa = m.AtmosphericPressureHpa,
                RelativeHumidityPercent = m.RelativeHumidityPercent,
                CalculatedAirDensityKgPerM3 = m.CalculatedAirDensity,
                CalculatedVolumeMl = m.CalculatedVolumeMl
            }).ToList()
        };

        return Task.FromResult<VolumeCalibrationResultDto?>(result);
    }

    public Task<CalibrationRecordDto> SaveCalibrationAsync(CalibrationRecordDto calibration)
    {
        lock (_lock)
        {
            var existing = _calibrations.FirstOrDefault(c => c.Id == calibration.Id);
            if (existing != null)
            {
                _calibrations.Remove(existing);
            }
            _calibrations.Add(calibration);

            // Atualiza status do equipamento para Calibrated
            var eq = _equipment.FirstOrDefault(e => e.Id == calibration.EquipmentId || e.LabTag == calibration.EquipmentTag);
            if (eq != null)
            {
                eq.Status = EquipmentStatus.Calibrated;
                eq.LastCalibrationDate = DateTime.UtcNow;
                eq.NextCalibrationDue = DateTime.UtcNow.AddMonths(eq.CalibrationIntervalMonths);
            }

            return Task.FromResult(calibration);
        }
    }

    public Task<CalibrationRecordDto?> GetCalibrationByIdAsync(string id)
    {
        lock (_lock)
        {
            var cal = _calibrations.FirstOrDefault(c => c.Id == id);
            return Task.FromResult(cal);
        }
    }

    public Task<List<CertificateDto>> GetCertificatesAsync(CertificateStatus? status = null)
    {
        lock (_lock)
        {
            var query = _certificates.AsEnumerable();
            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }
            return Task.FromResult(query.OrderByDescending(c => c.CertificateNumber).ToList());
        }
    }

    public Task<CertificateDto?> GetCertificateByIdAsync(string id)
    {
        lock (_lock)
        {
            var cert = _certificates.FirstOrDefault(c => c.Id == id || c.CertificateNumber == id);
            return Task.FromResult(cert);
        }
    }

    public Task<CertificateDto> CreateCertificateFromCalibrationAsync(CalibrationRecordDto calibration, bool showDetailedPoints = false)
    {
        lock (_lock)
        {
            var nextNum = _certificates.Count + 43;
            var certNumber = $"CERT-2026-{nextNum:D5}";
            var cert = new CertificateDto
            {
                Id = Guid.NewGuid().ToString(),
                CertificateNumber = certNumber,
                CalibrationRecordId = calibration.Id,
                EquipmentTag = calibration.EquipmentTag,
                EquipmentDescription = calibration.EquipmentDescription,
                ClientName = calibration.ClientName,
                IssueDate = DateTime.UtcNow,
                CalibrationDate = calibration.CreatedAt,
                NextCalibrationDueDate = DateTime.UtcNow.AddYears(1),
                Status = CertificateStatus.Draft,
                Result = calibration.Result,
                NominalVolume = calibration.NominalCapacity,
                MeanVolumeV20 = calibration.MeanV20,
                SystematicError = calibration.SystematicError,
                ExpandedUncertainty = calibration.ExpandedUncertainty,
                CoverageFactor = calibration.CoverageFactor,
                ConfidenceLevel = "95.45%",
                MaxPermissibleError = calibration.MaxPermissibleError,
                TechnicianName = calibration.TechnicianName,
                TechnicianRegistration = "CFT/CRT 198420-SP",
                SupervisorName = "Dr. Alexandre Morales",
                SupervisorRegistration = "CRQ IV 04218901",
                QrCodeValidationUrl = $"https://traclabs.com.br/validar?cert={certNumber}&hash={Guid.NewGuid():N}",
                Sha256Hash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")
            };

            // Adiciona padrões usados
            var usedStds = _standards
                .Where(s => s.Id == calibration.BalanceStandardId ||
                            s.Id == calibration.LiquidThermometerStandardId ||
                            s.Id == calibration.BarometerStandardId ||
                            s.Id == calibration.HygrometerStandardId ||
                            s.Id == calibration.AmbientThermometerStandardId ||
                            s.Id == calibration.ThermoHygrometerStandardId ||
                            s.Id == calibration.PycnometerStandardId)
                .ToList();
            cert.StandardsUsed = usedStds;

            _certificates.Insert(0, cert);
            return Task.FromResult(cert);
        }
    }

    public Task<CertificateDto> UpdateCertificateStatusAsync(string id, CertificateStatus newStatus, string? reason = null)
    {
        lock (_lock)
        {
            var cert = _certificates.FirstOrDefault(c => c.Id == id || c.CertificateNumber == id)
                ?? throw new KeyNotFoundException($"Certificado com ID '{id}' não foi encontrado.");

            cert.Status = newStatus;
            if (newStatus == CertificateStatus.Issued)
            {
                cert.ApprovalDate = DateTime.UtcNow;
            }
            else if (newStatus == CertificateStatus.Superseded && !string.IsNullOrWhiteSpace(reason))
            {
                cert.CancellationReason = reason;
            }

            return Task.FromResult(cert);
        }
    }

    public Task<CertificateDto?> GenerateCertificatePdfAsync(string certificateId, bool showDetailedPoints = false)
    {
        lock (_lock)
        {
            var cert = _certificates.FirstOrDefault(c => c.Id == certificateId || c.CertificateNumber == certificateId);
            return Task.FromResult(cert);
        }
    }

    // =========================================================================
    // Emissão e Download de PDF
    // =========================================================================

    public Task<byte[]?> GetCertificatePdfAsync(Guid certificateId)
    {
        return GetCertificatePdfAsync(certificateId.ToString());
    }

    public Task<byte[]?> GetCertificatePdfAsync(string certificateId)
    {
        lock (_lock)
        {
            var cert = _certificates.FirstOrDefault(c =>
                c.Id.Equals(certificateId, StringComparison.OrdinalIgnoreCase) ||
                c.CertificateNumber.Equals(certificateId, StringComparison.OrdinalIgnoreCase) ||
                (Guid.TryParse(certificateId, out var parsedGuid) &&
                 Guid.TryParse(c.Id, out var certGuid) && certGuid == parsedGuid));

            cert ??= _certificates.FirstOrDefault();
            if (cert == null) return Task.FromResult<byte[]?>(null);

            var pdfBytes = GenerateMockPdfBytes(cert);
            return Task.FromResult<byte[]?>(pdfBytes);
        }
    }

    private static byte[] GenerateMockPdfBytes(CertificateDto cert)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, System.Text.Encoding.Latin1);

        // Header: PDF 1.4 with standard binary comment
        writer.Write("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        writer.Flush();

        var offsets = new long[7];

        // 1. Catalog
        offsets[1] = ms.Position;
        writer.Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        writer.Flush();

        // 2. Pages
        offsets[2] = ms.Position;
        writer.Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        writer.Flush();

        // 3. Page (A4 standard: 595 x 842 pt)
        offsets[3] = ms.Position;
        writer.Write("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> /Contents 4 0 R >>\nendobj\n");
        writer.Flush();

        // 4. Content stream
        static string EscapePdf(string s) => (s ?? string.Empty).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("BT");
        sb.AppendLine("/F1 15 Tf");
        sb.AppendLine("50 790 Td");
        sb.AppendLine($"(TRACLABS - CERTIFICADO DE CALIBRACAO METROLOGICA) Tj");

        sb.AppendLine("0 -18 Td");
        sb.AppendLine("/F2 9 Tf");
        sb.AppendLine($"(Laboratorio de Calibracao Volumetrica RBC CAL-0942 | ABNT NBR ISO/IEC 17025) Tj");

        sb.AppendLine("0 -12 Td");
        sb.AppendLine("/F1 10 Tf");
        sb.AppendLine($"(------------------------------------------------------------------------------------------------------) Tj");

        sb.AppendLine("0 -20 Td");
        sb.AppendLine("/F1 12 Tf");
        sb.AppendLine($"({EscapePdf(cert.CertificateNumber)}  -  Status: {cert.Status}) Tj");

        sb.AppendLine("0 -18 Td");
        sb.AppendLine("/F2 10 Tf");
        sb.AppendLine($"(Cliente: {EscapePdf(cert.ClientName)}) Tj");

        sb.AppendLine("0 -15 Td");
        sb.AppendLine($"(CNPJ: {EscapePdf(cert.ClientTaxId)} | Endereco: {EscapePdf(cert.ClientAddress)}) Tj");

        sb.AppendLine("0 -15 Td");
        sb.AppendLine($"(Instrumento: {EscapePdf(cert.EquipmentDescription)} [{EscapePdf(cert.EquipmentTag)}]) Tj");

        sb.AppendLine("0 -15 Td");
        sb.AppendLine($"(Data de Calibracao: {cert.CalibrationDate:dd/MM/yyyy} | Validade Sugerida: {cert.NextCalibrationDueDate:dd/MM/yyyy}) Tj");

        sb.AppendLine("0 -22 Td");
        sb.AppendLine("/F1 11 Tf");
        sb.AppendLine($"(RESULTADOS METROLOGICOS (ABNT NBR ISO 4787 / ISO 8655)) Tj");

        sb.AppendLine("0 -16 Td");
        sb.AppendLine("/F2 10 Tf");
        sb.AppendLine($"(Volume Nominal: {cert.NominalVolume:F2} mL | Volume Medio V20: {cert.MeanVolumeV20:F4} mL) Tj");

        sb.AppendLine("0 -15 Td");
        sb.AppendLine($"(Erro Sistematico: {cert.SystematicError:F4} mL | Erro Maximo Permissivel: {cert.MaxPermissibleError:F4} mL) Tj");

        sb.AppendLine("0 -15 Td");
        sb.AppendLine($"(Incerteza Expandida U: {cert.ExpandedUncertainty:F4} mL [k = {cert.CoverageFactor:F2}, Confianca: {EscapePdf(cert.ConfidenceLevel)}]) Tj");

        sb.AppendLine("0 -15 Td");
        sb.AppendLine($"(Declaracao de Conformidade: {cert.Result}) Tj");

        sb.AppendLine("0 -22 Td");
        sb.AppendLine("/F1 11 Tf");
        sb.AppendLine($"(PADROES DE REFERENCIA UTILIZADOS) Tj");

        if (cert.StandardsUsed != null && cert.StandardsUsed.Any())
        {
            foreach (var std in cert.StandardsUsed.Take(4))
            {
                sb.AppendLine("0 -14 Td");
                sb.AppendLine("/F2 9 Tf");
                sb.AppendLine($"(- {EscapePdf(std.LabTag)}: {EscapePdf(std.Name)} | Cert: {EscapePdf(std.CalibrationCertificateNumber)}) Tj");
            }
        }
        else
        {
            sb.AppendLine("0 -14 Td");
            sb.AppendLine("/F2 9 Tf");
            sb.AppendLine($"(- Padroes RBC rastreados conforme relatorio gravimetrico) Tj");
        }

        sb.AppendLine("0 -22 Td");
        sb.AppendLine("/F1 11 Tf");
        sb.AppendLine($"(RASTREABILIDADE E AUTENTICIDADE) Tj");

        sb.AppendLine("0 -15 Td");
        sb.AppendLine("/F2 9 Tf");
        sb.AppendLine($"(URL de Validacao: {EscapePdf(cert.QrCodeValidationUrl)}) Tj");

        sb.AppendLine("0 -13 Td");
        sb.AppendLine($"(Hash SHA-256: {EscapePdf(cert.Sha256Hash)}) Tj");

        sb.AppendLine("0 -28 Td");
        sb.AppendLine("/F1 10 Tf");
        sb.AppendLine($"(Tecnico Responsavel: {EscapePdf(cert.TechnicianName)} - {EscapePdf(cert.TechnicianRegistration)}) Tj");

        sb.AppendLine("0 -15 Td");
        sb.AppendLine($"(Supervisor Metrologico: {EscapePdf(cert.SupervisorName)} - {EscapePdf(cert.SupervisorRegistration)}) Tj");

        sb.AppendLine("0 -28 Td");
        sb.AppendLine("/F2 8 Tf");
        sb.AppendLine($"(Documento gerado pelo sistema TracLabs Metrologia Industrial - ISO/IEC 17025) Tj");

        sb.AppendLine("ET");

        var streamBytes = System.Text.Encoding.Latin1.GetBytes(sb.ToString());

        offsets[4] = ms.Position;
        writer.Write($"4 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
        writer.Flush();
        ms.Write(streamBytes, 0, streamBytes.Length);
        writer.Write("\nendstream\nendobj\n");
        writer.Flush();

        // 5. Helvetica-Bold
        offsets[5] = ms.Position;
        writer.Write("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");
        writer.Flush();

        // 6. Helvetica Regular
        offsets[6] = ms.Position;
        writer.Write("6 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        writer.Flush();

        // xref Table
        var xrefOffset = ms.Position;
        writer.Write("xref\n0 7\n0000000000 65535 f \n");
        for (int i = 1; i <= 6; i++)
        {
            writer.Write($"{offsets[i]:D10} 00000 n \n");
        }

        // Trailer
        writer.Write($"trailer\n<< /Size 7 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        writer.Flush();

        return ms.ToArray();
    }

    // =========================================================================
    // Padrões do Laboratório - Ciclo de Vida e Rastreabilidade (ISO 17025 6.4.13)
    // =========================================================================

    private StandardEquipmentDto? FindStandard(Guid standardId)
    {
        var idStr = standardId.ToString();
        return _standards.FirstOrDefault(s =>
            s.Id.Equals(idStr, StringComparison.OrdinalIgnoreCase) ||
            (standardId == StandardBalanceId && (s.Id == "pad-bal-01" || s.LabTag == "PAD-BAL-01")) ||
            (standardId == StandardThermometerId && (s.Id == "pad-term-01" || s.LabTag == "PAD-TERM-01")) ||
            (standardId == StandardBarometerId && (s.Id == "pad-bar-01" || s.LabTag == "PAD-BAR-01")) ||
            (standardId == StandardHygrometerId && (s.Id == "pad-hig-01" || s.LabTag == "PAD-HIG-01")) ||
            (standardId == StandardWeightsId && (s.Id == "pad-pes-01" || s.LabTag == "PAD-PES-01")) ||
            (standardId == StandardAmbientThermometerId && (s.Id == "pad-teramb-01" || s.LabTag == "PAD-TERAMB-01")) ||
            (standardId == StandardStandHygrometerId && (s.Id == "pad-hig-02" || s.LabTag == "PAD-HIG-02")) ||
            (standardId == StandardPycnometerId && (s.Id == "pad-pic-01" || s.LabTag == "PAD-PIC-01")) ||
            (standardId == StandardPycnometerExpiredId && (s.Id == "pad-pic-02" || s.LabTag == "PAD-PIC-02")) ||
            (Guid.TryParse(s.Id, out var g) && g == standardId));
    }

    private StandardEquipmentDto? FindStandard(string standardId)
    {
        if (Guid.TryParse(standardId, out var guid))
        {
            var byGuid = FindStandard(guid);
            if (byGuid != null) return byGuid;
        }

        return _standards.FirstOrDefault(s =>
            s.Id.Equals(standardId, StringComparison.OrdinalIgnoreCase) ||
            s.LabTag.Equals(standardId, StringComparison.OrdinalIgnoreCase));
    }

    public Task<List<StandardCertificateDto>> GetStandardCertificatesAsync(Guid standardId)
    {
        lock (_lock)
        {
            var standard = FindStandard(standardId);
            var targetGuid = standardId;
            if (standard != null)
            {
                if (standard.Id == "pad-bal-01" || standard.LabTag == "PAD-BAL-01") targetGuid = StandardBalanceId;
                else if (standard.Id == "pad-term-01" || standard.LabTag == "PAD-TERM-01") targetGuid = StandardThermometerId;
                else if (standard.Id == "pad-bar-01" || standard.LabTag == "PAD-BAR-01") targetGuid = StandardBarometerId;
                else if (standard.Id == "pad-hig-01" || standard.LabTag == "PAD-HIG-01") targetGuid = StandardHygrometerId;
                else if (standard.Id == "pad-pes-01" || standard.LabTag == "PAD-PES-01") targetGuid = StandardWeightsId;
                else if (standard.Id == "pad-teramb-01" || standard.LabTag == "PAD-TERAMB-01") targetGuid = StandardAmbientThermometerId;
                else if (standard.Id == "pad-hig-02" || standard.LabTag == "PAD-HIG-02") targetGuid = StandardStandHygrometerId;
                else if (standard.Id == "pad-pic-01" || standard.LabTag == "PAD-PIC-01") targetGuid = StandardPycnometerId;
                else if (standard.Id == "pad-pic-02" || standard.LabTag == "PAD-PIC-02") targetGuid = StandardPycnometerExpiredId;
                else if (Guid.TryParse(standard.Id, out var parsedGuid)) targetGuid = parsedGuid;
            }

            var certs = _standardCertificates
                .Where(c => c.StandardEquipmentId == targetGuid || c.StandardEquipmentId == standardId)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.CalibrationDate)
                .ToList();

            return Task.FromResult(certs);
        }
    }

    public Task<List<StandardCalibrationUsageDto>> GetStandardCalibrationsUsedAsync(Guid standardId)
    {
        lock (_lock)
        {
            var standard = FindStandard(standardId);
            var targetGuid = standardId;
            if (standard != null)
            {
                if (standard.Id == "pad-bal-01" || standard.LabTag == "PAD-BAL-01") targetGuid = StandardBalanceId;
                else if (standard.Id == "pad-term-01" || standard.LabTag == "PAD-TERM-01") targetGuid = StandardThermometerId;
                else if (standard.Id == "pad-bar-01" || standard.LabTag == "PAD-BAR-01") targetGuid = StandardBarometerId;
                else if (standard.Id == "pad-hig-01" || standard.LabTag == "PAD-HIG-01") targetGuid = StandardHygrometerId;
                else if (standard.Id == "pad-pes-01" || standard.LabTag == "PAD-PES-01") targetGuid = StandardWeightsId;
                else if (standard.Id == "pad-teramb-01" || standard.LabTag == "PAD-TERAMB-01") targetGuid = StandardAmbientThermometerId;
                else if (standard.Id == "pad-hig-02" || standard.LabTag == "PAD-HIG-02") targetGuid = StandardStandHygrometerId;
                else if (standard.Id == "pad-pic-01" || standard.LabTag == "PAD-PIC-01") targetGuid = StandardPycnometerId;
                else if (standard.Id == "pad-pic-02" || standard.LabTag == "PAD-PIC-02") targetGuid = StandardPycnometerExpiredId;
                else if (Guid.TryParse(standard.Id, out var parsedGuid)) targetGuid = parsedGuid;
            }

            var usages = _calibrationUsages
                .Where(u => u.StandardId == targetGuid || u.StandardId == standardId)
                .OrderByDescending(u => u.CalibrationDate)
                .ToList();

            return Task.FromResult(usages);
        }
    }

    public Task UpdateStandardAsync(StandardEquipmentDto standard)
    {
        ArgumentNullException.ThrowIfNull(standard);

        lock (_lock)
        {
            var existing = FindStandard(standard.Id)
                ?? (Guid.TryParse(standard.Id, out var g) ? FindStandard(g) : null)
                ?? throw new KeyNotFoundException($"Equipamento padrão com ID '{standard.Id}' não encontrado.");

            if (string.IsNullOrWhiteSpace(standard.LabTag))
                throw new ArgumentException("O tag de identificação do padrão é obrigatório.", nameof(standard.LabTag));
            if (standard.RangeMax < standard.RangeMin)
                throw new ArgumentException("A faixa máxima deve ser maior ou igual à faixa mínima.", nameof(standard.RangeMax));
            if (standard.Resolution < 0)
                throw new ArgumentException("A resolução não pode ser negativa.", nameof(standard.Resolution));

            existing.LabTag = standard.LabTag;
            existing.Name = standard.Name;
            existing.Model = standard.Model;
            existing.Manufacturer = standard.Manufacturer;
            existing.SerialNumber = standard.SerialNumber;
            existing.InstrumentType = standard.InstrumentType;
            existing.Role = standard.Role;
            existing.Resolution = standard.Resolution;
            existing.RangeMin = standard.RangeMin;
            existing.RangeMax = standard.RangeMax;
            existing.CurrentLocation = standard.CurrentLocation;
            existing.Error = standard.Error;
            existing.Correction = standard.Correction;
            existing.ExpandedUncertainty = standard.ExpandedUncertainty;
            existing.CoverageFactor = standard.CoverageFactor;

            _recentEvents.Insert(0, new CalibrationTimelineEventDto
            {
                Id = Guid.NewGuid().ToString(),
                TimeString = "Hoje, " + DateTime.UtcNow.ToString("HH:mm"),
                Title = "Informações de Padrão Atualizadas",
                EquipmentTag = existing.LabTag,
                ClientName = "Laboratório Central TracLabs",
                OperatorName = "Supervisor do Laboratório",
                Result = CalibrationResult.Conforming,
                Details = $"Informações cadastrais do padrão '{existing.LabTag} — {existing.Name}' foram atualizadas."
            });

            return Task.CompletedTask;
        }
    }

    public Task SendStandardToCalibrationAsync(Guid standardId, string laboratory, DateTime expectedReturn)
    {
        if (string.IsNullOrWhiteSpace(laboratory))
            throw new ArgumentException("O laboratório executor da calibração deve ser informado.", nameof(laboratory));

        lock (_lock)
        {
            var standard = FindStandard(standardId)
                ?? throw new KeyNotFoundException($"Equipamento padrão com ID '{standardId}' não encontrado.");

            if (standard.Status == StandardEquipmentStatus.InCalibration)
                throw new InvalidOperationException("O equipamento padrão já está em processo de calibração.");

            standard.Status = StandardEquipmentStatus.InCalibration;
            standard.CalibratingLaboratory = laboratory;

            _recentEvents.Insert(0, new CalibrationTimelineEventDto
            {
                Id = Guid.NewGuid().ToString(),
                TimeString = "Hoje, " + DateTime.UtcNow.ToString("HH:mm"),
                Title = "Padrão Enviado para Calibração Externa",
                EquipmentTag = standard.LabTag,
                ClientName = "Laboratório Central TracLabs",
                OperatorName = "Supervisor do Laboratório",
                Result = CalibrationResult.Pending,
                Details = $"Padrão '{standard.LabTag}' enviado para calibração externa no laboratório '{laboratory}'. Retorno previsto: {expectedReturn:dd/MM/yyyy}."
            });

            return Task.CompletedTask;
        }
    }

    public Task RegisterStandardCertificateAsync(Guid standardId, StandardCertificateDto certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (string.IsNullOrWhiteSpace(certificate.CertificateNumber))
            throw new ArgumentException("O número do certificado deve ser informado.", nameof(certificate.CertificateNumber));
        if (string.IsNullOrWhiteSpace(certificate.CalibratingLaboratory))
            throw new ArgumentException("O laboratório calibrador deve ser informado.", nameof(certificate.CalibratingLaboratory));
        if (certificate.CalibrationDueDate <= certificate.CalibrationDate)
            throw new ArgumentException("A data de validade deve ser posterior à data de calibração.", nameof(certificate.CalibrationDueDate));

        lock (_lock)
        {
            var standard = FindStandard(standardId)
                ?? throw new KeyNotFoundException($"Equipamento padrão com ID '{standardId}' não encontrado.");

            var targetGuid = standardId;
            if (standard.Id == "pad-bal-01" || standard.LabTag == "PAD-BAL-01") targetGuid = StandardBalanceId;
            else if (standard.Id == "pad-term-01" || standard.LabTag == "PAD-TERM-01") targetGuid = StandardThermometerId;
            else if (standard.Id == "pad-bar-01" || standard.LabTag == "PAD-BAR-01") targetGuid = StandardBarometerId;
            else if (standard.Id == "pad-hig-01" || standard.LabTag == "PAD-HIG-01") targetGuid = StandardHygrometerId;
            else if (standard.Id == "pad-pes-01" || standard.LabTag == "PAD-PES-01") targetGuid = StandardWeightsId;
            else if (standard.Id == "pad-teramb-01" || standard.LabTag == "PAD-TERAMB-01") targetGuid = StandardAmbientThermometerId;
            else if (standard.Id == "pad-hig-02" || standard.LabTag == "PAD-HIG-02") targetGuid = StandardStandHygrometerId;
            else if (standard.Id == "pad-pic-01" || standard.LabTag == "PAD-PIC-01") targetGuid = StandardPycnometerId;
            else if (standard.Id == "pad-pic-02" || standard.LabTag == "PAD-PIC-02") targetGuid = StandardPycnometerExpiredId;
            else if (Guid.TryParse(standard.Id, out var parsedGuid)) targetGuid = parsedGuid;

            certificate.StandardEquipmentId = targetGuid;

            if (certificate.IsActive)
            {
                var existingCerts = _standardCertificates.Where(c => c.StandardEquipmentId == targetGuid || c.StandardEquipmentId == standardId).ToList();
                foreach (var cert in existingCerts)
                {
                    cert.IsActive = false;
                }

                standard.CalibrationCertificateNumber = certificate.CertificateNumber;
                standard.CalibratingLaboratory = certificate.CalibratingLaboratory;
                standard.LastCalibrationDate = certificate.CalibrationDate;
                standard.CalibrationDueDate = certificate.CalibrationDueDate;

                if (certificate.ExpandedUncertainty > 0)
                {
                    standard.Error = certificate.Error;
                    standard.Correction = certificate.Correction != 0 ? certificate.Correction : -certificate.Error;
                    standard.ExpandedUncertainty = certificate.ExpandedUncertainty;
                    standard.CoverageFactor = certificate.CoverageFactor > 0 ? certificate.CoverageFactor : 2.0m;
                }

                if (certificate.IsValid)
                {
                    standard.Status = StandardEquipmentStatus.Calibrated;
                }
                else
                {
                    standard.Status = StandardEquipmentStatus.PendingCalibration;
                }
            }

            _standardCertificates.Insert(0, certificate);

            _recentEvents.Insert(0, new CalibrationTimelineEventDto
            {
                Id = Guid.NewGuid().ToString(),
                TimeString = "Hoje, " + DateTime.UtcNow.ToString("HH:mm"),
                Title = "Novo Certificado de Padrão Registrado",
                EquipmentTag = standard.LabTag,
                ClientName = "Laboratório Central TracLabs",
                OperatorName = "Supervisor do Laboratório",
                Result = CalibrationResult.Conforming,
                Details = $"Novo certificado '{certificate.CertificateNumber}' registrado para o padrão '{standard.LabTag}'. Validade até {certificate.CalibrationDueDate:dd/MM/yyyy}."
            });

            return Task.CompletedTask;
        }
    }
}
