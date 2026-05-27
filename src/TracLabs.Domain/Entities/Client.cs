namespace TracLabs.Domain.Entities;

/// <summary>
/// Represents a client (customer) of the laboratory.
/// A client owns one or more pieces of equipment that are submitted for calibration.
/// </summary>
public class Client : BaseEntity
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Full legal name of the company or individual.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Brazilian tax identifier — either CNPJ (companies) or CPF (individuals).
    /// Stored as digits only, no formatting characters (no dots, slashes or dashes).
    /// </summary>
    public string TaxId { get; private set; } = string.Empty;

    /// <summary>Indicates whether TaxId is a CNPJ or CPF.</summary>
    public TaxIdType TaxIdType { get; private set; }

    // ── Contact ───────────────────────────────────────────────────────────────

    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;

    /// <summary>Name of the contact person at this client's organization.</summary>
    public string ContactPerson { get; private set; } = string.Empty;

    // ── Address ───────────────────────────────────────────────────────────────

    public string Street { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;

    /// <summary>Brazilian state abbreviation, e.g. "RJ", "SP".</summary>
    public string State { get; private set; } = string.Empty;

    /// <summary>Brazilian postal code (CEP), digits only.</summary>
    public string PostalCode { get; private set; } = string.Empty;

    // ── Relationships ─────────────────────────────────────────────────────────

    /// <summary>
    /// All equipment this client has ever submitted to the lab.
    /// Read-only from outside — use Equipment.CompleteCalibration() to mutate.
    /// </summary>
    public IReadOnlyCollection<Equipment> Equipment => _equipment.AsReadOnly();
    private readonly List<Equipment> _equipment = new();

    // ── Constructors ──────────────────────────────────────────────────────────

    private Client() { }

    public Client(
        string name,
        string taxId,
        TaxIdType taxIdType,
        string email,
        string phone,
        string contactPerson,
        string street,
        string city,
        string state,
        string postalCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Client name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(taxId))
            throw new ArgumentException("Tax ID cannot be empty.", nameof(taxId));

        Name = name;
        TaxId = taxId;
        TaxIdType = taxIdType;
        Email = email;
        Phone = phone;
        ContactPerson = contactPerson;
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
    }

    // ── Domain Methods ────────────────────────────────────────────────────────

    /// <summary>Updates the client's contact information.</summary>
    public void UpdateContactInfo(string email, string phone, string contactPerson)
    {
        Email = email;
        Phone = phone;
        ContactPerson = contactPerson;
        MarkAsUpdated();
    }

    /// <summary>Updates the client's address.</summary>
    public void UpdateAddress(string street, string city, string state, string postalCode)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        MarkAsUpdated();
    }
}