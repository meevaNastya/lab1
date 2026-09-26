using CarRental.Api.Enums;

namespace CarRental.Api.Exceptions;

public class DriverRequirementsNotMetException : Exception
{
    public DriverRequirementsNotMetException(
        CarCategory category,
        int minimumAge,
        int minimumDrivingExperienceYears)
        : base(
            $"Driver does not meet requirements for {category}: "
            + $"minimum age is {minimumAge}, "
            + $"minimum driving experience is {minimumDrivingExperienceYears} years.")
    {
    }
}
