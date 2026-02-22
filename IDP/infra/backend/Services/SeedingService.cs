using Backend.Models;
using Backend.Data;
using Backend.Services;

namespace Backend.Services;

public interface ISeedingService
{
    Task SeedDataAsync();
}

public class SeedingService : ISeedingService
{
    private readonly IMemoryService _memoryService;
    private readonly ILogger<SeedingService> _logger;

    public SeedingService(IMemoryService memoryService, ILogger<SeedingService> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    public async Task SeedDataAsync()
    {
        try
        {
            // Check if data already exists
            var existingCourses = await _memoryService.GetActiveCoursesAsync();
            if (existingCourses.Any())
            {
                _logger.LogInformation("Sample data already exists, skipping seeding");
                return;
            }

            // Seed courses
            var courses = SampleDataSeeder.GetSampleCourses();
            foreach (var course in courses)
            {
                // Store course in memory (simplified for demo)
                _logger.LogInformation("Created course: {CourseTitle}", course.Title);
            }

            // Seed questions
            var questions = SampleDataSeeder.GetSampleQuestions();
            foreach (var question in questions)
            {
                // Store question in memory (simplified for demo)
                _logger.LogInformation("Created question: {QuestionId}", question.Id);
            }

            // Seed users
            var users = SampleDataSeeder.GetSampleUsers();
            foreach (var user in users)
            {
                await _memoryService.CreateUserAsync(user);
                _logger.LogInformation("Created user: {PhoneNumber}", user.PhoneNumber);
            }

            _logger.LogInformation("Sample data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding sample data");
            throw;
        }
    }
}
