using Docs.Models;
using Nest;


namespace Docs.Extension
{
    public static class ElasticSearchExtension
    {
        public static void AddElasticSearch(this IServiceCollection services, IConfiguration configuration)
        {
            var baseUrl = configuration["ElasticSettings:baseUrl"];
            var index = configuration["ElasticSettings:defaultIndex"];

            var settings = new ConnectionSettings(new Uri(baseUrl ?? ""))
                .PrettyJson()
                .CertificateFingerprint("77c5f7c313cbad7da26174dd3818b0285b6e8ce906c56d43c534d3af48c52465")
                .BasicAuthentication("elastic", "3A13DIp*uGWz498q1+pL")
                .DefaultIndex(index);

            var client = new ElasticClient(settings);

            // Register Elasticsearch client as a singleton
            services.AddSingleton<IElasticClient>(client);

            CreateIndex(client, index);

        }

        private static void CreateIndex(IElasticClient client, string indexName)
        {

            var createIndexResponse = client.Indices.Create(indexName,
                index => index.Map<Document>(x => x.AutoMap()));
        }
    }
}
