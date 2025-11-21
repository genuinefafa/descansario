using Descansario.Api.Data;
using Descansario.Api.Models;
using Descansario.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Descansario.Tests.Services;

public class WorkingDaysCalculatorTests : IDisposable
{
    private readonly DescansarioDbContext _dbContext;
    private readonly WorkingDaysCalculator _calculator;

    public WorkingDaysCalculatorTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<DescansarioDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DescansarioDbContext(options);
        _calculator = new WorkingDaysCalculator(_dbContext);

        // Seed default configuration (weekends: Sunday and Saturday)
        _dbContext.Configurations.Add(new Configuration
        {
            Id = 1,
            WeekendDays = "0,6" // Sunday and Saturday
        });
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldReturnCorrectCount_WhenNoHolidays()
    {
        // Arrange: Monday to Friday (1 week)
        var startDate = new DateTime(2025, 11, 24); // Monday
        var endDate = new DateTime(2025, 11, 28);   // Friday

        // Act
        var result = await _calculator.CalculateWorkingDaysAsync(startDate, endDate);

        // Assert
        Assert.Equal(5, result); // 5 working days
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldExcludeWeekends()
    {
        // Arrange: Monday to Sunday (includes weekend)
        var startDate = new DateTime(2025, 11, 24); // Monday
        var endDate = new DateTime(2025, 11, 30);   // Sunday

        // Act
        var result = await _calculator.CalculateWorkingDaysAsync(startDate, endDate);

        // Assert
        Assert.Equal(5, result); // Only Mon-Fri, exclude Sat-Sun
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldExcludeHolidays()
    {
        // Arrange: Add a holiday on Wednesday
        var startDate = new DateTime(2025, 11, 24); // Monday
        var endDate = new DateTime(2025, 11, 28);   // Friday

        _dbContext.Holidays.Add(new Holiday
        {
            Date = new DateTime(2025, 11, 26), // Wednesday
            Name = "Test Holiday",
            Country = "AR"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateWorkingDaysAsync(startDate, endDate);

        // Assert
        Assert.Equal(4, result); // 5 days - 1 holiday = 4
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldExcludeBothWeekendsAndHolidays()
    {
        // Arrange: Monday to Sunday with holiday on Wednesday
        var startDate = new DateTime(2025, 11, 24); // Monday
        var endDate = new DateTime(2025, 11, 30);   // Sunday

        _dbContext.Holidays.Add(new Holiday
        {
            Date = new DateTime(2025, 11, 26), // Wednesday
            Name = "Test Holiday",
            Country = "AR"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateWorkingDaysAsync(startDate, endDate);

        // Assert
        Assert.Equal(4, result); // 5 weekdays - 1 holiday = 4
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldReturnOne_WhenSameDay()
    {
        // Arrange: Same day (Monday)
        var startDate = new DateTime(2025, 11, 24); // Monday
        var endDate = new DateTime(2025, 11, 24);   // Same Monday

        // Act
        var result = await _calculator.CalculateWorkingDaysAsync(startDate, endDate);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldReturnZero_WhenSameDayIsWeekend()
    {
        // Arrange: Same day (Saturday)
        var startDate = new DateTime(2025, 11, 29); // Saturday
        var endDate = new DateTime(2025, 11, 29);   // Same Saturday

        // Act
        var result = await _calculator.CalculateWorkingDaysAsync(startDate, endDate);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldThrowException_WhenStartDateAfterEndDate()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 28);
        var endDate = new DateTime(2025, 11, 24);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _calculator.CalculateWorkingDaysAsync(startDate, endDate)
        );
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldHandleMultipleHolidays()
    {
        // Arrange: Two weeks with multiple holidays
        var startDate = new DateTime(2025, 11, 24); // Monday
        var endDate = new DateTime(2025, 12, 5);    // Friday (2 weeks)

        _dbContext.Holidays.AddRange(new[]
        {
            new Holiday { Date = new DateTime(2025, 11, 26), Name = "Holiday 1", Country = "AR" }, // Wed
            new Holiday { Date = new DateTime(2025, 12, 1), Name = "Holiday 2", Country = "AR" }   // Mon
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateWorkingDaysAsync(startDate, endDate);

        // Assert
        // 10 weekdays - 2 holidays = 8
        Assert.Equal(8, result);
    }

    [Fact]
    public async Task CalculateWorkingDaysBatch_ShouldReturnCorrectCounts_ForMultipleRanges()
    {
        // Arrange
        var dateRanges = new[]
        {
            (new DateTime(2025, 11, 24), new DateTime(2025, 11, 28)), // Mon-Fri: 5 days
            (new DateTime(2025, 12, 1), new DateTime(2025, 12, 5)),   // Mon-Fri: 5 days
            (new DateTime(2025, 12, 20), new DateTime(2025, 12, 21))  // Sat-Sun: 0 days
        };

        // Act
        var results = await _calculator.CalculateWorkingDaysBatchAsync(dateRanges);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(5, results[dateRanges[0]]);
        Assert.Equal(5, results[dateRanges[1]]);
        Assert.Equal(0, results[dateRanges[2]]);
    }

    [Fact]
    public async Task CalculateWorkingDaysBatch_ShouldOptimizeHolidayQuery()
    {
        // Arrange: Add holidays across different months
        _dbContext.Holidays.AddRange(new[]
        {
            new Holiday { Date = new DateTime(2025, 11, 26), Name = "Nov Holiday", Country = "AR" },
            new Holiday { Date = new DateTime(2025, 12, 1), Name = "Dec Holiday", Country = "AR" }
        });
        await _dbContext.SaveChangesAsync();

        var dateRanges = new[]
        {
            (new DateTime(2025, 11, 24), new DateTime(2025, 11, 28)), // Contains 1 holiday
            (new DateTime(2025, 12, 1), new DateTime(2025, 12, 5))    // Contains 1 holiday
        };

        // Act
        var results = await _calculator.CalculateWorkingDaysBatchAsync(dateRanges);

        // Assert
        Assert.Equal(4, results[dateRanges[0]]); // 5 - 1 holiday
        Assert.Equal(4, results[dateRanges[1]]); // 5 - 1 holiday
    }

    [Fact]
    public async Task CalculateWorkingDaysBatch_ShouldReturnEmpty_WhenNoRanges()
    {
        // Arrange
        var dateRanges = Array.Empty<(DateTime, DateTime)>();

        // Act
        var results = await _calculator.CalculateWorkingDaysBatchAsync(dateRanges);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task CalculateWorkingDaysBatch_ShouldReturnZero_WhenStartDateAfterEndDate()
    {
        // Arrange
        var dateRanges = new[]
        {
            (new DateTime(2025, 11, 28), new DateTime(2025, 11, 24)) // Invalid range
        };

        // Act
        var results = await _calculator.CalculateWorkingDaysBatchAsync(dateRanges);

        // Assert
        Assert.Equal(0, results[dateRanges[0]]);
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldHandleLongRange()
    {
        // Arrange: 1 year
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 12, 31);

        // Act
        var result = await _calculator.CalculateWorkingDaysAsync(startDate, endDate);

        // Assert
        // Approximately 260-261 working days in a year (365 - 104 weekends)
        Assert.InRange(result, 250, 270);
    }

    [Fact]
    public async Task CalculateWorkingDays_ShouldHandleLeapYear()
    {
        // Arrange: February 29th in leap year
        var startDate = new DateTime(2024, 2, 26); // Monday
        var endDate = new DateTime(2024, 3, 1);    // Friday

        // Act
        var result = await _calculator.CalculateWorkingDaysAsync(startDate, endDate);

        // Assert
        // Mon(26), Tue(27), Wed(28), Thu(29), Fri(1) = 5 days
        Assert.Equal(5, result);
    }
}
