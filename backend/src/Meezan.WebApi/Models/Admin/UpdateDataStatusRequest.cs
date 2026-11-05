namespace Meezan.WebApi.Models.Admin;

/// <summary>Request body for the manual DataStatus override endpoint.</summary>
public sealed record UpdateDataStatusRequest(string DataStatus);
