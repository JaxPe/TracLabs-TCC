namespace TracLabs.Domain.Enums;

/// <summary>
/// Discriminates between Brazilian tax identification document types.
/// </summary>
public enum TaxIdType
{
    /// <summary>Cadastro Nacional da Pessoa Jurídica — for companies.</summary>
    Cnpj = 0,

    /// <summary>Cadastro de Pessoa Física — for individuals.</summary>
    Cpf = 1
}