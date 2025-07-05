namespace JwtAuthDemo.Services
{
    public interface IFileTemplateService
    {
        Task<string> GetParsedTemplateAsync(string fileName, Dictionary<string, string> placeholders);
    }
    public class FileTemplateService : IFileTemplateService
    {
        private readonly IWebHostEnvironment _env;

        public FileTemplateService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> GetParsedTemplateAsync(string fileName, Dictionary<string, string> placeholders)
        {
            var path = Path.Combine(_env.ContentRootPath, "Templates\\EmailTemplates", fileName);
            try
            {
                var template = await File.ReadAllTextAsync(path);
                foreach (var kv in placeholders)
                {
                    template = template.Replace($"{{{{{kv.Key}}}}}", kv.Value);
                }

                return template;
            }
            catch (Exception er)
            {

                throw new Exception( er.Message);
            }
            

            
        }
    }
}
