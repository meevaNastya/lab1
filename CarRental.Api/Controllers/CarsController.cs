using CarRental.Api.Dtos.Cars;
using CarRental.Api.Entities;
using CarRental.Api.Enums;
using CarRental.Api.Exceptions;
using CarRental.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController : ControllerBase
{
    private const string ManagementRoles = "Manager,Administrator";

    private readonly CarService _carService;

    public CarsController(CarService carService)
    {
        _carService = carService ?? throw new ArgumentNullException(nameof(carService));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CarResponse>>> GetCars()
    {
        IReadOnlyCollection<Car> cars = await _carService.GetCarsAsync();
        List<CarResponse> response = cars
            .Select(CreateResponse)
            .ToList();

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("{carId:int}")]
    public async Task<ActionResult<CarResponse>> GetCar(int carId)
    {
        try
        {
            Car car = await _carService.GetCarAsync(carId);

            return Ok(CreateResponse(car));
        }
        catch (CarNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [AllowAnonymous]
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyCollection<CarResponse>>> GetAvailableCars(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] CarCategory? category = null,
        [FromQuery] string? brand = null,
        [FromQuery] decimal? maxPricePerDay = null)
    {
        try
        {
            IReadOnlyCollection<Car> cars = await _carService.GetAvailableCarsAsync(
                startDate,
                endDate,
                category,
                brand,
                maxPricePerDay);
            List<CarResponse> response = cars
                .Select(CreateResponse)
                .ToList();

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [AllowAnonymous]
    [HttpGet("{carId:int}/status")]
    public async Task<ActionResult<CarStatusResponse>> GetCarStatus(
        int carId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        try
        {
            CarStatus status = await _carService.GetCarStatusAsync(
                carId,
                startDate,
                endDate);
            var response = new CarStatusResponse(
                carId,
                startDate,
                endDate,
                status);

            return Ok(response);
        }
        catch (CarNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = ManagementRoles)]
    [HttpPost]
    public async Task<ActionResult<CarResponse>> CreateCar(CreateCarRequest request)
    {
        try
        {
            Car car = await _carService.CreateCarAsync(
                request.Brand,
                request.Model,
                request.Vin,
                request.Category,
                request.PricePerDay);
            CarResponse response = CreateResponse(car);

            return CreatedAtAction(
                nameof(GetCar),
                new { carId = car.Id },
                response);
        }
        catch (VinAlreadyExistsException exception)
        {
            return Conflict(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = ManagementRoles)]
    [HttpPut("{carId:int}/price")]
    public async Task<IActionResult> ChangePrice(
        int carId,
        ChangeCarPriceRequest request)
    {
        try
        {
            await _carService.ChangeCarPricePerDayAsync(
                carId,
                request.PricePerDay);

            return NoContent();
        }
        catch (CarNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = ManagementRoles)]
    [HttpPost("{carId:int}/maintenance")]
    public async Task<ActionResult<MaintenancePeriodResponse>> AddMaintenance(
        int carId,
        CreateMaintenanceRequest request)
    {
        try
        {
            MaintenancePeriod period = await _carService.AddMaintenancePeriodAsync(
                carId,
                request.StartDate,
                request.EndDate,
                request.Description);
            var response = new MaintenancePeriodResponse(
                period.Id,
                period.CarId,
                period.StartDate,
                period.EndDate,
                period.Description);

            return Ok(response);
        }
        catch (CarNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (CarIsNotAvailableException exception)
        {
            return Conflict(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private static CarResponse CreateResponse(Car car)
    {
        return new CarResponse(
            car.Id,
            car.Brand,
            car.Model,
            car.Vin,
            car.Category,
            car.PricePerDay);
    }
}
