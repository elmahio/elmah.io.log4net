using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elmah.Io.Log4Net.AspNetCore60.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;

        public PrivacyModel(ILogger<PrivacyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            try
            {
                int i = 0;
                var y = 12 / i;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error happened");
            }
        }
    }
}