using Domain.Db;
using Domain.Rabbit;
using Infrastructure.Data;
using Infrastructure.RabbitMQ;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentsDbContext _context;
        private readonly RabbitMqProducer _producer;
        private readonly IWebHostEnvironment _environment;

        public DocumentsController(
            DocumentsDbContext context, 
            RabbitMqProducer producer,
            IWebHostEnvironment environment)
        {
            _context = context;
            _producer = producer;
            _environment = environment;
        }

        // Загрузка PDF
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // Проверка что файл PDF
            if (file == null || file.Length == 0)
                return BadRequest("Не загружен файл");

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Можно загружать только pdf-файлы");

            // Сохраняем документ в БД
            var document = new Document
            {
                FileName = file.FileName,
                Status = Status.Pending
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Сохраняем файл на диск
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, $"{Guid.NewGuid()}_{document.Id}.pdf");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Формируем данные в RabbitMQ
            var docTask = new DocumentTask
            {
                DocumentId = document.Id,
                FilePath = filePath
            };

            await _producer.PublishAsync(docTask);

            return Ok(new { document.Id, document.Status });
        }

        // Получение списка PDF
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var documents = await _context.Documents
                .Select(d => new { d.Id, d.FileName, d.Status })
                .ToListAsync();

            return Ok(documents);
        }

        // Получение текстового содержимого PDF
        [HttpGet("{id}/text")]
        public async Task<IActionResult> GetText(long id)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return NotFound($"Документ с id {id} не найден");

            if (document.Status != Status.Completed)
                return BadRequest($"Текст из документа еще не извлечен. Текущий статус: {document.Status}");

            return Ok(new { document.Id, document.FileName, document.ExtractedText });
        }
    }
}
