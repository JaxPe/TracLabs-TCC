namespace TracLabs.Frontend.Models;

public class DashboardMetricsDto
{
    public int ActiveCalibrationsCount { get; set; } = 5;
    public int IssuedCertificatesMonthCount { get; set; } = 42;
    public int ExpiringStandardsCount { get; set; } = 3;
    public int NonConformingCount { get; set; } = 2;
}
