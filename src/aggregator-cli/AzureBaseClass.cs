using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;


namespace aggregator.cli {
    internal abstract class AzureBaseClass
    {
        protected IAzure _azure;

        protected ILogger _logger;

        protected AzureBaseClass(IAzure azure, ILogger logger)
        {
            _azure = azure;
            _logger = logger;
        }


        protected async Task<IWebApp> GetWebApp(InstanceName instance, CancellationToken cancellationToken)
        {
            var webFunctionApp = await _azure
                                       .AppServices
                                       .WebApps
                                       .GetByResourceGroupAsync(
                                                                instance.ResourceGroupName,
                                                                instance.FunctionAppName, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return webFunctionApp;
        }


        protected KuduApi GetKudu(InstanceName instance)
        {
            var kudu = new KuduApi(instance, _azure, _logger);
            return kudu;
        }
    }
}