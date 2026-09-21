using kadroff.Components.Data;
using Microsoft.EntityFrameworkCore;

namespace kadroff.Components.Services
{
    public class JobApplicationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<JobApplicationService> _logger;

        public JobApplicationService(AppDbContext context, ILogger<JobApplicationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasAlreadyAppliedAsync(int positionId, string candidateId)
        {
            return await _context.JobApplications
                .AnyAsync(j => j.PositionId == positionId && j.CandidateId == candidateId);
        }

        public async Task<(bool Success, string Message)> ApplyAsync(int positionId, string candidateId, string? coverLetter)
        {
            try
            {
                if (await HasAlreadyAppliedAsync(positionId, candidateId))
                {
                    return (false, "Вы уже откликались на эту вакансию.");
                }

                var application = new JobApplication
                {
                    PositionId = positionId,
                    CandidateId = candidateId,
                    AppliedAt = DateTime.UtcNow,
                    Status = JobApplicationStatus.UnderReview,
                    CoverLetter = coverLetter?.Trim() ?? string.Empty
                };

                _context.JobApplications.Add(application);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Пользователь {CandidateId} успешно откликнулся на вакансию {PositionId}", candidateId, positionId);

                return (true, "Ваш отклик и сопроводительное письмо успешно отправлены!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании отклика на вакансию {PositionId} для кандидата {CandidateId}", positionId, candidateId);
                return (false, "Произошла ошибка при отправке отклика. Попробуйте позже.");
            }
        }

        public async Task<List<JobApplication>> GetCandidateApplicationsAsync(string candidateId)
        {
            return await _context.JobApplications
                .Include(j => j.Position)
                .Where(j => j.CandidateId == candidateId)
                .OrderByDescending(j => j.AppliedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<JobApplication>> GetAllApplicationsAsync()
        {
            return await _context.JobApplications
                .Include(j => j.Position)
                .Include(j => j.Candidate)
                .OrderByDescending(j => j.AppliedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> UpdateApplicationStatusAsync(int applicationId, JobApplicationStatus newStatus)
        {
            try
            {
                var application = await _context.JobApplications.FindAsync(applicationId);
                if (application == null)
                {
                    return (false, "Отклик не найден.");
                }

                application.Status = newStatus;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Статус отклика {ApplicationId} изменен на {Status}", applicationId, newStatus);
                return (true, "Статус отклика успешно обновлен.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении статуса отклика {ApplicationId}", applicationId);
                return (false, "Не удалось обновить статус.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateApplicationStatusAsync(int positionId, string candidateId, JobApplicationStatus newStatus)
        {
            try
            {
                var application = await _context.JobApplications
                    .FirstOrDefaultAsync(j => j.PositionId == positionId && j.CandidateId == candidateId);

                if (application == null)
                {
                    return (false, "Отклик не найден.");
                }

                application.Status = newStatus;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Статус отклика для кандидата {CandidateId} на вакансию {PositionId} изменен на {Status}", candidateId, positionId, newStatus);
                return (true, "Статус отклика успешно обновлен.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении статуса отклика для {CandidateId} на вакансию {PositionId}", candidateId, positionId);
                return (false, "Не удалось обновить статус.");
            }
        }
        public async Task<(bool Success, string Message)> SaveDecisionAsync(int applicationId, JobApplicationStatus status, string? comment, DateTime? autoAcceptDate)
        {
            try
            {
                var app = await _context.JobApplications.FindAsync(applicationId);

                if (app == null) return (false, "Отклик не найден.");

                app.Status = status;
                app.ResponseComment = comment;
                app.AutoAcceptDate = autoAcceptDate;

                app.AutoAcceptDate = autoAcceptDate.HasValue
                    ? DateTime.SpecifyKind(autoAcceptDate.Value, DateTimeKind.Utc)
                    : null;

                await _context.SaveChangesAsync();
                return (true, "Решение успешно сохранено.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении решения по отклику");
                return (false, "Ошибка при сохранении.");
            }
        }
    }
}