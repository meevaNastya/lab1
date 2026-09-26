namespace CarRental.Api.Entities;

public class RentalAgreement
{
    private RentalAgreement()
    {
        RentalRequest = null!;
        Client = null!;
        Car = null!;
    }

    public RentalAgreement(
        RentalRequest rentalRequest,
        decimal pricePerDay)
    {
        if (rentalRequest is null)
            throw new ArgumentNullException(nameof(rentalRequest));

        if (pricePerDay <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pricePerDay),
                "Price per day must be positive.");
        }

        int rentalDays = rentalRequest.EndDate.DayNumber - rentalRequest.StartDate.DayNumber + 1;

        RentalRequest = rentalRequest;
        RentalRequestId = rentalRequest.Id;
        Client = rentalRequest.Client;
        ClientId = rentalRequest.ClientId;
        Car = rentalRequest.Car;
        CarId = rentalRequest.CarId;
        StartDate = rentalRequest.StartDate;
        EndDate = rentalRequest.EndDate;
        PricePerDay = pricePerDay;
        RentalCost = pricePerDay * rentalDays;
        Penalty = 0;
        TotalCost = RentalCost;
        IsCompleted = false;
    }

    public int Id { get; private set; }

    public int RentalRequestId { get; private set; }

    public RentalRequest RentalRequest { get; private set; }

    public int ClientId { get; private set; }

    public User Client { get; private set; }

    public int CarId { get; private set; }

    public Car Car { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public decimal PricePerDay { get; private set; }

    public decimal RentalCost { get; private set; }

    public decimal Penalty { get; private set; }

    public decimal TotalCost { get; private set; }

    public DateOnly? ActualReturnDate { get; private set; }

    public bool HasDamage { get; private set; }

    public bool IsCompleted { get; private set; }

    public bool Overlaps(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException(
                "Rental end date cannot be earlier than start date.",
                nameof(endDate));
        }

        return StartDate <= endDate && EndDate >= startDate;
    }

    public void Complete(
        DateOnly actualReturnDate,
        bool hasDamage,
        decimal damagePenaltyCoefficient)
    {
        if (IsCompleted)
            throw new InvalidOperationException("Rental agreement is already completed.");

        if (actualReturnDate < StartDate)
        {
            throw new ArgumentException(
                "Actual return date cannot be earlier than rental start date.",
                nameof(actualReturnDate));
        }

        if (damagePenaltyCoefficient < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damagePenaltyCoefficient),
                "Damage penalty coefficient cannot be negative.");
        }

        int lateDays = Math.Max(0, actualReturnDate.DayNumber - EndDate.DayNumber);
        decimal latePenalty = PricePerDay * lateDays;
        decimal damagePenalty = hasDamage
            ? PricePerDay * damagePenaltyCoefficient
            : 0;

        ActualReturnDate = actualReturnDate;
        HasDamage = hasDamage;
        Penalty = latePenalty + damagePenalty;
        TotalCost = RentalCost + Penalty;
        IsCompleted = true;
    }
}
