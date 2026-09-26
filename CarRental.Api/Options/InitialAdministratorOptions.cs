namespace CarRental.Api.Options;

public class InitialAdministratorOptions
{
    public const string SectionName = "InitialAdministrator";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }
}
