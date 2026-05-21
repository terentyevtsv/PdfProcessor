using Domain.Db;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services
{
    public class DocumentsDbService
    {
        private readonly DbContextOptions<DocumentsDbContext> _options;

        public DocumentsDbService(string conStr)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DocumentsDbContext>();
            optionsBuilder.UseNpgsql(conStr);

            _options = optionsBuilder.Options;
        }

        /// <summary>
        /// Сохранение текста PDF в БД
        /// </summary>
        /// <param name="id">id документа в БД</param>
        /// <param name="text">Текст, который нужно сохранить</param>
        /// <returns></returns>
        public async Task SaveTextAsync(long id, string text)
        {
            // Обновление БД
            using var dbContext = new DocumentsDbContext(_options);
            var document = await dbContext.Documents
                .FirstOrDefaultAsync(d => d.Id == id);
            if (document != null)
            {
                document.Status = Status.Completed;
                document.ExtractedText = text;
                await dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Обновление статуса документа
        /// </summary>
        /// <param name="id">id документа</param>
        /// <param name="status">Новый статус</param>
        /// <returns></returns>
        public async Task UpdateStatusAsync(long id, Status status)
        {
            using var dbContext = new DocumentsDbContext(_options);
            var document = await dbContext.Documents
                .FirstOrDefaultAsync(d => d.Id == id);
            if (document != null)
            {
                document.Status = Status.Failed;
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<Status?> GetStatusAsync(long id)
        {
            using var dbContext = new DocumentsDbContext(_options);
            var document = await dbContext.Documents
                .FirstOrDefaultAsync(d => d.Id == id);

            return document?.Status;
        }
    }
}
