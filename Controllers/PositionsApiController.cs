using kadroff.Components.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace kadroff.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PositionsApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PositionsApiController(AppDbContext db) => _db = db;

        [HttpGet("aggregated")]
        public async Task<IActionResult> GetAggregatedData([FromHeader(Name = "X-Api-Token")] string? apiToken = null)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                var allData = await _db.Positions
                    .Where(p => p.PositionTokens.Any())
                    .Include(p => p.PositionAttributes)
                        .ThenInclude(pa => pa.Attribute)
                    .Select(p => new
                    {
                        positionId = p.Id,
                        title = p.Title,
                        tokens = _db.PositionTokens
                            .Where(t => t.PositionId == p.Id)
                            .Select(t => t.Token)
                            .ToList(),
                    })
                    .ToListAsync();

                return Ok(allData);
            }

            var singlePosition = await _db.PositionTokens
                .Where(pt => pt.Token == apiToken)
                .Join(_db.Positions, token => token.PositionId, position => position.Id, (token, position) => position)
                .Include(p => p.PositionAttributes)
                    .ThenInclude(pa => pa.Attribute)
                .FirstOrDefaultAsync();

            if (singlePosition == null)
                return NotFound(new { error = "Вакансия с таким API-токеном не найдена" });

            var singleResult = new
            {
                positionId = singlePosition.Id,
                title = singlePosition.Title,
                usedToken = apiToken,
            };

            return Ok(singleResult);
        }
    }
}
