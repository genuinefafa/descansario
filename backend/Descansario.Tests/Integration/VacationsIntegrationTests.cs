using System.Net;
using System.Net.Http.Json;
using Descansario.Api.DTOs;
using Descansario.Api.Data;
using Descansario.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Descansario.Tests.Integration;

public class VacationsIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public VacationsIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region GET /api/vacations

    [Fact]
    public async Task GetVacations_ReturnsSuccessAndEmptyList_WhenNoVacationsExist()
    {
        // Act
        var response = await _client.GetAsync("/api/vacations");

        // Assert
        response.EnsureSuccessStatusCode();
        var vacations = await response.Content.ReadFromJsonAsync<List<VacationDto>>();
        Assert.NotNull(vacations);
    }

    #endregion

    #region POST /api/vacations

    [Fact]
    public async Task CreateVacation_ReturnsCreated_WhenValidData()
    {
        // Arrange
        var createDto = new CreateVacationDto
        {
            PersonId = 1,
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2025, 12, 5),
            Status = "Pending",
            Notes = "Test vacation"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vacations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var vacation = await response.Content.ReadFromJsonAsync<VacationDto>();
        Assert.NotNull(vacation);
        Assert.Equal(1, vacation.PersonId);
        Assert.Equal("Pending", vacation.Status);
        Assert.True(vacation.WorkingDaysCount > 0);
    }

    [Fact]
    public async Task CreateVacation_ReturnsCorrectWorkingDays_ExcludingWeekends()
    {
        // Arrange: Monday to Friday = 5 working days
        var createDto = new CreateVacationDto
        {
            PersonId = 1,
            StartDate = new DateTime(2025, 11, 24), // Monday
            EndDate = new DateTime(2025, 11, 28),   // Friday
            Status = "Approved"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vacations", createDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var vacation = await response.Content.ReadFromJsonAsync<VacationDto>();
        Assert.NotNull(vacation);
        Assert.Equal(5, vacation.WorkingDaysCount);
    }

    [Fact]
    public async Task CreateVacation_ReturnsBadRequest_WhenEndDateBeforeStartDate()
    {
        // Arrange
        var createDto = new CreateVacationDto
        {
            PersonId = 1,
            StartDate = new DateTime(2025, 12, 10),
            EndDate = new DateTime(2025, 12, 5), // Before start
            Status = "Pending"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vacations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateVacation_ReturnsBadRequest_WhenPersonNotFound()
    {
        // Arrange
        var createDto = new CreateVacationDto
        {
            PersonId = 999, // Non-existent
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2025, 12, 5),
            Status = "Pending"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vacations", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /api/vacations/{id}

    [Fact]
    public async Task GetVacation_ReturnsVacation_WhenExists()
    {
        // Arrange: Create a vacation first
        var createDto = new CreateVacationDto
        {
            PersonId = 1,
            StartDate = new DateTime(2025, 12, 15),
            EndDate = new DateTime(2025, 12, 19),
            Status = "Approved",
            Notes = "Get test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/vacations", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<VacationDto>();

        // Act
        var response = await _client.GetAsync($"/api/vacations/{created!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var vacation = await response.Content.ReadFromJsonAsync<VacationDto>();
        Assert.NotNull(vacation);
        Assert.Equal(created.Id, vacation.Id);
        Assert.Equal("Get test", vacation.Notes);
    }

    [Fact]
    public async Task GetVacation_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/vacations/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region PUT /api/vacations/{id}

    [Fact]
    public async Task UpdateVacation_ReturnsOk_WhenValidData()
    {
        // Arrange: Create a vacation first
        var createDto = new CreateVacationDto
        {
            PersonId = 1,
            StartDate = new DateTime(2025, 12, 22),
            EndDate = new DateTime(2025, 12, 26),
            Status = "Pending"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/vacations", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<VacationDto>();

        // Update to Approved
        var updateDto = new UpdateVacationDto
        {
            PersonId = 1,
            StartDate = new DateTime(2025, 12, 22),
            EndDate = new DateTime(2025, 12, 26),
            Status = "Approved",
            Notes = "Updated notes"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/vacations/{created!.Id}", updateDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<VacationDto>();
        Assert.NotNull(updated);
        Assert.Equal("Approved", updated.Status);
        Assert.Equal("Updated notes", updated.Notes);
    }

    [Fact]
    public async Task UpdateVacation_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var updateDto = new UpdateVacationDto
        {
            PersonId = 1,
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2025, 12, 5),
            Status = "Approved"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/vacations/99999", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region DELETE /api/vacations/{id}

    [Fact]
    public async Task DeleteVacation_ReturnsNoContent_WhenExists()
    {
        // Arrange: Create a vacation first
        var createDto = new CreateVacationDto
        {
            PersonId = 1,
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 9),
            Status = "Pending"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/vacations", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<VacationDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/vacations/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/vacations/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteVacation_ReturnsNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync("/api/vacations/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /api/vacations/working-days

    [Fact]
    public async Task GetWorkingDays_ReturnsCorrectCount()
    {
        // Arrange: Monday to Friday
        var startDate = new DateTime(2025, 11, 24).ToString("yyyy-MM-dd");
        var endDate = new DateTime(2025, 11, 28).ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/api/vacations/working-days?startDate={startDate}&endDate={endDate}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WorkingDaysResponse>();
        Assert.NotNull(result);
        Assert.Equal(5, result.WorkingDays);
    }

    private record WorkingDaysResponse(int WorkingDays);

    #endregion

    #region GET /api/vacations/person/{personId}

    [Fact]
    public async Task GetVacationsByPerson_ReturnsVacationsForPerson()
    {
        // Arrange: Create a vacation for person 1
        var createDto = new CreateVacationDto
        {
            PersonId = 1,
            StartDate = new DateTime(2026, 2, 2),
            EndDate = new DateTime(2026, 2, 6),
            Status = "Approved"
        };
        await _client.PostAsJsonAsync("/api/vacations", createDto);

        // Act
        var response = await _client.GetAsync("/api/vacations/person/1");

        // Assert
        response.EnsureSuccessStatusCode();
        var vacations = await response.Content.ReadFromJsonAsync<List<VacationDto>>();
        Assert.NotNull(vacations);
        Assert.All(vacations, v => Assert.Equal(1, v.PersonId));
    }

    #endregion
}
