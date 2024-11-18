using Microsoft.Extensions.Logging;

namespace Elmah.Io.Log4Net.Console80
{
    public class Service
    {
        private readonly ILogger<Service> logger;

        public Service(ILogger<Service> logger)
        {
            this.logger = logger;
        }

        public void Execute()
        {
            try
            {
                var i = 0;
                var result = 42 / i;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during Execute");
            }
        }
    }
}
