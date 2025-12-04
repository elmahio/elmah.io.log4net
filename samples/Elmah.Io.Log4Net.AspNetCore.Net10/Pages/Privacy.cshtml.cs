using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elmah.Io.Log4Net.AspNetCore.Net10.Pages
{
    public class PrivacyModel(ILogger<PrivacyModel> logger) : PageModel
    {
        public void OnGet()
        {
            try
            {
                var i = 0;
                var result = 42 / i;
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Result is {Result}", result);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during Privacy");
            }
        }
    }
}