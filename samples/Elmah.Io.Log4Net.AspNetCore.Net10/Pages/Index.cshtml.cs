using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elmah.Io.Log4Net.AspNetCore.Net10.Pages
{
    public class IndexModel(ILogger<IndexModel> logger) : PageModel
    {
        public void OnGet()
        {
            logger.LogWarning("Request to frontpage");
        }
    }
}
