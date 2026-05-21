using System.Text;
using UglyToad.PdfPig;

namespace Infrastructure.Services
{
    public class PdfService
    {
        private readonly string _filePath;

        public PdfService(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// Извлечение текста из PDF-файла
        /// </summary>
        /// <returns>Текст из PDF</returns>
        public string GetText()
        {
            // ИЗВЛЕЧЕНИЕ ТЕКСТА ИЗ PDF
            var stringBuilder = new StringBuilder();
            using (var pdf = PdfDocument.Open(_filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    stringBuilder.Append(page.Text);
                }
            }

            return stringBuilder.ToString();
        }
    }
}
