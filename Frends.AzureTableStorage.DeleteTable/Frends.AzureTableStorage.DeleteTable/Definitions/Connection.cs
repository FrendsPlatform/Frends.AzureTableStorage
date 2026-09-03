using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.AzureTableStorage.DeleteTable.Attributes;

namespace Frends.AzureTableStorage.DeleteTable.Definitions;

/// <summary>
/// Connection parameters for Azure Table Storage.
/// </summary>
public class Connection
{
    /// <summary>
    /// Connection method to use for connecting to Azure Table Storage.
    /// </summary>
    /// <example>ConnectionMethod.ConnectionString</example>
    [DefaultValue(ConnectionMethod.ConnectionString)]
    public ConnectionMethod ConnectionMethod { get; set; } = ConnectionMethod.ConnectionString;

    /// <summary>
    /// Connection string for the Azure Storage Account.
    /// Required when ConnectionMethod is ConnectionString.
    /// </summary>
    /// <example>DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net</example>
    [DisplayFormat(DataFormatString = "Text")]
    [PasswordPropertyText]
    [DefaultValue("")]
    [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.ConnectionString)]
    [RequiredIf(nameof(ConnectionMethod), ConnectionMethod.ConnectionString)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Application (Client) ID of Azure AD Application.
    /// Required when ConnectionMethod is OAuth2.
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.OAuth2)]
    [RequiredIf(nameof(ConnectionMethod), ConnectionMethod.OAuth2)]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Tenant ID.
    /// Required when ConnectionMethod is OAuth2.
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.OAuth2)]
    [RequiredIf(nameof(ConnectionMethod), ConnectionMethod.OAuth2)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Application Client Secret.
    /// Required when ConnectionMethod is OAuth2.
    /// </summary>
    /// <example>your-client-secret</example>
    [DisplayFormat(DataFormatString = "Text")]
    [PasswordPropertyText]
    [DefaultValue("")]
    [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.OAuth2)]
    [RequiredIf(nameof(ConnectionMethod), ConnectionMethod.OAuth2)]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Shared Access Signature (SAS) token for authentication.
    /// Required when ConnectionMethod is SasToken.
    /// </summary>
    /// <example>?sv=2021-06-08&amp;ss=t&amp;srt=sco&amp;sp=rwdlacu&amp;se=2024-12-31T23:59:59Z&amp;st=2024-01-01T00:00:00Z&amp;spr=https&amp;sig=...</example>
    [DisplayFormat(DataFormatString = "Text")]
    [PasswordPropertyText]
    [DefaultValue("")]
    [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.SasToken)]
    [RequiredIf(nameof(ConnectionMethod), ConnectionMethod.SasToken)]
    public string SasToken { get; set; } = string.Empty;

    /// <summary>
    /// Name of the Azure Storage Account.
    /// Required when ConnectionMethod is OAuth2, SasToken, ArcManagedIdentity, or ArcManagedIdentityCrossTenant.
    /// </summary>
    /// <example>mystorageaccount</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    [UIHint(
        nameof(ConnectionMethod),
        "",
        ConnectionMethod.OAuth2,
        ConnectionMethod.SasToken,
        ConnectionMethod.ArcManagedIdentity,
        ConnectionMethod.ArcManagedIdentityCrossTenant)]
    [RequiredIf(
        nameof(ConnectionMethod),
        ConnectionMethod.OAuth2,
        ConnectionMethod.SasToken,
        ConnectionMethod.ArcManagedIdentity,
        ConnectionMethod.ArcManagedIdentityCrossTenant)]
    public string StorageAccountName { get; set; } = string.Empty;

    /// <summary>
    /// Scopes used when authenticating with Arc Managed Identity Cross Tenant.
    /// Required when ConnectionMethod is ArcManagedIdentityCrossTenant.
    /// </summary>
    /// <example>["api://AzureADTokenExchange/.default"]</example>
    [DefaultValue(null)]
    [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.ArcManagedIdentityCrossTenant)]
    [RequiredIf(nameof(ConnectionMethod), ConnectionMethod.ArcManagedIdentityCrossTenant)]
    public string[] Scopes { get; set; } = [];

    /// <summary>
    /// Target Tenant ID of Azure Tenant.
    /// Required when ConnectionMethod is ArcManagedIdentityCrossTenant.
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.ArcManagedIdentityCrossTenant)]
    [RequiredIf(nameof(ConnectionMethod), ConnectionMethod.ArcManagedIdentityCrossTenant)]
    public string TargetTenantId { get; set; } = string.Empty;

    /// <summary>
    /// Target Client ID of Azure Tenant.
    /// Required when ConnectionMethod is ArcManagedIdentityCrossTenant.
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    [UIHint(nameof(ConnectionMethod), "", ConnectionMethod.ArcManagedIdentityCrossTenant)]
    [RequiredIf(nameof(ConnectionMethod), ConnectionMethod.ArcManagedIdentityCrossTenant)]
    public string TargetClientId { get; set; } = string.Empty;
}
