using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using kadroff.Components.Data;

namespace kadroff.Components
{
    public class CvService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CvService> _logger;

        public CvService(AppDbContext context, ILogger<CvService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> TogglePublishStatusAsync(long cvId, string userId)
        {
            var cv = await _context.Cvs
                .FirstOrDefaultAsync(c => c.Id == cvId && c.UserId == userId)
                ?? throw new KeyNotFoundException("Резюме не найдено или доступ запрещен.");

            cv.IsPublished = !cv.IsPublished;

            try
            {
                await _context.SaveChangesAsync();
                return cv.IsPublished;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Конфликт параллельного доступа при изменении резюме {CvId}", cvId);
                throw new InvalidOperationException("Данные резюме были изменены другим запросом. Обновите страницу.");
            }
        }

        public async Task ToggleLikeAsync(long cvId, string recruiterId)
        {
            var existingLike = await _context.CvLikes
                .FirstOrDefaultAsync(l => l.CvId == cvId && l.RecruiterId == recruiterId);

            if (existingLike != null)
            {
                _context.CvLikes.Remove(existingLike);
            }
            else
            {
                _context.CvLikes.Add(new CvLike
                {
                    CvId = cvId,
                    RecruiterId = recruiterId
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetLikesCountAsync(long cvId)
        {
            return await _context.CvLikes
                .AsNoTracking()
                .CountAsync(l => l.CvId == cvId);
        }

        public async Task<List<CvItemDto>> GetCandidateCvsAsync(string userId)
        {
            return await _context.Cvs
                .AsNoTracking()
                .Include(c => c.Position)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CvItemDto
                {
                    Id = c.Id,
                    PositionTitle = c.Position != null ? c.Position.Title : "Без названия",
                    CreatedAt = c.CreatedAt,
                    IsPublished = c.IsPublished
                })
                .ToListAsync();
        }

        public async Task<CvFullDetailsDto?> GetCvFullDetailsAsync(long cvId, string userId)
        {
            var cv = await _context.Cvs
                .AsNoTracking()
                .Include(c => c.Position)
                .FirstOrDefaultAsync(c => c.Id == cvId && c.UserId == userId);

            if (cv == null) return null;

            var attributes = await _context.UserAttributeValues
                .AsNoTracking()
                .Include(a => a.Attribute)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var projects = await _context.CandidateProjects
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            return new CvFullDetailsDto
            {
                Cv = cv,
                Position = cv.Position,
                Attributes = attributes,
                Projects = projects
            };
        }
    }

    public class CvWizardService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CvWizardService> _logger;

        public CvWizardService(AppDbContext context, ILogger<CvWizardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Position>> GetPositionsAsync() =>
            await _context.Positions.AsNoTracking().ToListAsync();

        public async Task<List<AppAttribute>> GetAppAttributesAsync() =>
            await _context.Attributes.AsNoTracking().ToListAsync();

        public async Task<bool> SaveWizardDataAsync(string userId, CreateCvViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cv = new Cv
                {
                    UserId = userId,
                    PositionId = model.PositionId,
                    IsPublished = model.IsPublished,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Cvs.Add(cv);

                var existingAttributes = await _context.UserAttributeValues
                    .Where(u => u.UserId == userId)
                    .ToListAsync();

                foreach (var attr in model.Attributes.Where(a => !string.IsNullOrWhiteSpace(a.StringValue)))
                {
                    var existing = existingAttributes.FirstOrDefault(e => e.AttributeId == attr.AttributeId);
                    if (existing != null)
                    {
                        existing.StringValue = attr.StringValue.Trim();
                    }
                    else
                    {
                        _context.UserAttributeValues.Add(new UserAttributeValue
                        {
                            UserId = userId,
                            AttributeId = attr.AttributeId,
                            StringValue = attr.StringValue.Trim()
                        });
                    }
                }

                var oldProjects = await _context.CandidateProjects
                    .Where(p => p.UserId == userId)
                    .ToListAsync();

                if (oldProjects.Any())
                {
                    _context.CandidateProjects.RemoveRange(oldProjects);
                }

                foreach (var proj in model.Projects.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
                {
                    var tagsArray = string.IsNullOrWhiteSpace(proj.TagsInput)
                        ? Array.Empty<string>()
                        : proj.TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    _context.CandidateProjects.Add(new CandidateProject
                    {
                        UserId = userId,
                        Name = proj.Name.Trim(),
                        DescriptionMarkdown = proj.DescriptionMarkdown?.Trim() ?? string.Empty,
                        StartDate = proj.StartDate.HasValue ? DateTime.SpecifyKind(proj.StartDate.Value, DateTimeKind.Utc) : null,
                        EndDate = proj.EndDate.HasValue ? DateTime.SpecifyKind(proj.EndDate.Value, DateTimeKind.Utc) : null,
                        Tags = tagsArray
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при сохранении мастера резюме для пользователя {UserId}. Причина: {Message}",
                    userId, ex.InnerException?.Message ?? ex.Message);

                return false;
            }
        }
    }

    #region ViewModels & DTOs

    public class CreateCvViewModel
    {
        [Required(ErrorMessage = "Выберите позицию")]
        [Range(1, int.MaxValue, ErrorMessage = "Выберите позицию из списка")]
        public int PositionId { get; set; }

        public bool IsPublished { get; set; }

        public List<UserAttributeViewModel> Attributes { get; set; } = new();
        public List<CandidateProjectViewModel> Projects { get; set; } = new();
    }

    public class UserAttributeViewModel
    {
        public int AttributeId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string StringValue { get; set; } = string.Empty;
    }

    public class CandidateProjectViewModel
    {
        [Required(ErrorMessage = "Введите название проекта")]
        public string Name { get; set; } = string.Empty;
        public string DescriptionMarkdown { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string TagsInput { get; set; } = string.Empty;
    }

    public class CvItemDto
    {
        public long Id { get; set; }
        public string PositionTitle { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsPublished { get; set; }
    }

    public class CvFullDetailsDto
    {
        public Cv Cv { get; set; } = null!;
        public Position? Position { get; set; }
        public List<UserAttributeValue> Attributes { get; set; } = new();
        public List<CandidateProject> Projects { get; set; } = new();
    }

    #endregion
}