using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace kadroff.Components
{
    [Authorize(Roles = "Admin, Candidate")]
    [Route("api/cv/pdf")]
    public class CvPdfController : Controller
    {
        private readonly CvService _cvService;

        public CvPdfController(CvService cvService)
        {
            _cvService = cvService;
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetCvPdf(long id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var details = await _cvService.GetCvFullDetailsAsync(id, userId);
            if (details == null) return NotFound("Резюме не найдено");

            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append($@"
                <!DOCTYPE html>
                <html lang='ru'>
                <head>
                    <meta charset='utf-8' />
                    <title>Резюме - {details.Position?.Title}</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 30px; color: #333; }}
                        h1 {{ color: #0d6efd; border-bottom: 2px solid #0d6efd; padding-bottom: 5px; }}
                        h2 {{ margin-top: 20px; color: #495057; }}
                        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
                        th, td {{ border: 1px solid #dee2e6; padding: 8px; text-align: left; }}
                        th {{ background-color: #f8f9fa; }}
                        .project {{ border: 1px solid #ccc; padding: 10px; margin-bottom: 10px; border-radius: 4px; }}
                        .tag {{ display: inline-block; background: #e9ecef; padding: 2px 6px; border-radius: 3px; font-size: 12px; margin-right: 4px; }}
                        @media print {{
                            body {{ margin: 0; }}
                            .no-print {{ display: none; }}
                        }}
                    </style>
                </head>
                <body>
                    <h1>Резюме: {details.Position?.Title}</h1>
                    <p><strong>Дата формирования:</strong> {DateTime.UtcNow.ToLocalTime():dd.MM.yyyy HH:mm}</p>
                    <p><strong>Описание позиции:</strong> {details.Position?.Description}</p>

                    <h2>Анкетные данные и навыки</h2>
                    <table>
                        <thead>
                            <tr>
                                <th>Категория</th>
                                <th>Атрибут</th>
                                <th>Значение</th>
                            </tr>
                        </thead>
                        <tbody>");

            if (details.Attributes.Any())
            {
                foreach (var attr in details.Attributes)
                {
                    htmlBuilder.Append($@"
                        <tr>
                            <td>{attr.Attribute?.Category}</td>
                            <td>{attr.Attribute?.Name}</td>
                            <td>{attr.StringValue}</td>
                        </tr>");
                }
            }
            else
            {
                htmlBuilder.Append("<tr><td colspan='3' style='text-align:center;'>Атрибуты не заполнены.</td></tr>");
            }

            htmlBuilder.Append(@"
                        </tbody>
                    </table>
                    <h2>Опыт и проекты</h2>");

            if (details.Projects.Any())
            {
                foreach (var proj in details.Projects)
                {
                    string startDate = proj.StartDate?.ToString("yyyy-MM") ?? "N/A";
                    string endDate = proj.EndDate?.ToString("yyyy-MM") ?? "По настоящее время";

                    htmlBuilder.Append($@"
                    <div class='project'>
                        <strong>{proj.Name}</strong><br/>
                        <p>{proj.DescriptionMarkdown}</p>
                        <small>Период: {startDate} — {endDate}</small><br/>
                        <div>");

                    if (proj.Tags != null && proj.Tags.Any())
                    {
                        foreach (var tag in proj.Tags)
                        {
                            htmlBuilder.Append($"<span class='tag'>{tag}</span>");
                        }
                    }
                    htmlBuilder.Append("</div></div>");
                }
            }
            else
            {
                htmlBuilder.Append("<p>Проекты не добавлены.</p>");
            }

            htmlBuilder.Append(@"
                    <script>
                        window.onload = function() { window.print(); }
                    </script>
                </body>
                </html>");

            return Content(htmlBuilder.ToString(), "text/html", Encoding.UTF8);
        }
    }
}