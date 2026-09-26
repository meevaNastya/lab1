namespace CarRental.Api.Entities;

public class MaintenancePeriod
{
    private const int MaximumDescriptionLength = 500;

    private MaintenancePeriod()
    {
        Car = null!;
        Description = string.Empty;
    }

    public MaintenancePeriod(
        Car car,
        DateOnly startDate,
        DateOnly endDate,
        string description)
    {
        if (car is null)
            throw new ArgumentNullException(nameof(car));

        if (startDate == default)
            throw new ArgumentException("Maintenance start date is required.", nameof(startDate));

        if (endDate == default)
            throw new ArgumentException("Maintenance end date is required.", nameof(endDate));

        if (endDate < startDate)
        {
            throw new ArgumentException(
                "Maintenance end date cannot be earlier than start date.",
                nameof(endDate));
        }

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Maintenance description is empty.", nameof(description));

        if (description.Length > MaximumDescriptionLength)
        {
            throw new ArgumentException(
                $"Maintenance description cannot contain more than {MaximumDescriptionLength} characters.",
                nameof(description));
        }

        Car = car;
        CarId = car.Id;
        StartDate = startDate;
        EndDate = endDate;
        Description = description.Trim();
    }

    public int Id { get; private set; }

    public int CarId { get; private set; }

    public Car Car { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public string Description { get; private set; }

    public bool Overlaps(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException(
                "Period end date cannot be earlier than start date.",
                nameof(endDate));
        }

        return StartDate <= endDate && EndDate >= startDate;
    }
}
