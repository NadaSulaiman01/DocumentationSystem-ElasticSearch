using Docs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace Docs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IElasticClient _elasticClient;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public DocumentController(IElasticClient elasticClient,
            IWebHostEnvironment hostingEnvironment)
        {
            _elasticClient = elasticClient;
            _hostingEnvironment = hostingEnvironment;
        }


        //[HttpGet]
        //public IEnumerable<Document> Index(string keyword) {
        //    var documents = new List<Document>();
        //    if (!string.IsNullOrEmpty(keyword))
        //    {
        //        var result = _elasticClient.SearchAsync<Document>(
        //            s => s.Query(q => q.QueryString(
        //                d => d.Query('*' + keyword + '*')))
        //            .Size(50));
        //        var finalResult = result;
        //        documents = finalResult.Result.Documents.ToList();
        //    }

        //    return documents.AsEnumerable();
        //}

        [HttpGet("getAll")]
        public ActionResult<IEnumerable<DocumentDto>> getAll()
        {
            var documents = new List<Document>();
            var result = _elasticClient.SearchAsync<Document>(
                s => s.Query(q => q.MatchAll()).Size(10000));

            documents = result.Result.Documents.ToList();

            var documentsDto = documents.Select(d => new DocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                Authors = d.Authors,
                CreatedDate = d.CreatedDate,
                ModifiedDate = d.ModifiedDate,
                Category = d.Category,
                SubCategory = d.SubCategory,
                Tags = d.Tags,
                Version = d.Version,
                Url = d.Url
            }).ToList();

            return documentsDto;
        }

        [HttpPost("getTop")]
        public ActionResult<IEnumerable<DocumentDto>> getTopResults( TicketDto ticket)
        {
            // Extract keywords and synonyms from the searchString, converting to lowercase
            var keywords = ExtractKeywords(ticket.Description.ToLower());
            var synonyms = GetSynonyms(keywords);
            //foreach ( var synonym in synonyms)
            //{
            //    Console.WriteLine(synonym);
            //}

            string searchableText = string.Join(" ", synonyms);

            // Perform Elasticsearch search with case-insensitive terms and bidirectional synonyms
            //var searchResponse = _elasticClient.Search<Document>(s => s
            //    .Query(q => q
            //        .Terms(t => t
            //            .Field(f => f.Content) // Assuming "Content" is the field to search
            //            .Terms(synonyms.SelectMany(syn => synonyms.Contains(syn) ? new List<string> { syn, syn } : new List<string> { syn }))
            //        )
            //    )
            //    .Sort(sort => sort.Descending("_score"))
            //);

            var searchResponse = _elasticClient.Search<DocumentDto>(s => s
            .Size(10000)
    .Query(q => q
        .Bool(b => b
            .Should(sh => sh
                .MultiMatch(mm => mm
                    .Query(searchableText)
                    .Fields(f => f.Fields("content", "tags"))
                    .MinimumShouldMatch(MinimumShouldMatch.Percentage(1))
                    .Boost(2)
                )
            )
        )
    )
    .TrackScores(true) // Track scores if needed
);

            var documents = searchResponse.Documents.ToList();
            var maxScore = searchResponse.MaxScore;
            Console.WriteLine(maxScore);

            var resultsWithScore = searchResponse.Hits.Select(hit => new {
                Document = hit.Source,
                Score = hit.Score
            });


            // Convert to DocumentDto
            var documentsDto = resultsWithScore.Select(d => new DocumentDto
            {
                Id = d.Document.Id,
                Title = d.Document.Title,
                Authors = d.Document.Authors,
                CreatedDate = d.Document.CreatedDate,
                ModifiedDate = d.Document.ModifiedDate,
                Category = d.Document.Category,
                SubCategory = d.Document.SubCategory,
                Tags = d.Document.Tags,
                Version = d.Document.Version,
                Url = d.Document.Url,
                Score = d.Score
            }).ToList();

            return documentsDto;
        }

        // Method to extract keywords and convert to lowercase
        private List<string> ExtractKeywords(string input)
        {
            // Split input into words
            var words = input.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            // Example: Remove common stop words (you can expand this list as needed)
            var stopWords = new HashSet<string> {
    "a", "an", "the", "is", "are", "am", "in", "on", "at", "to", "for", "of",
    "with", "and", "or", "but", "not", "can", "this", "that", "these", "those",
    "it", "he", "she", "we", "they", "you", "i", "me", "him", "her", "us", "them",
    "my", "mine", "your", "yours", "his", "her", "hers", "its", "our", "ours", "their", "theirs",
    "where", "when", "how", "why", "what", "which", "who", "whom", "whose",
    "be", "being", "been", "was", "were", "will", "shall", "should", "would", "could",
    "do", "does", "did", "done", "doing", "have", "has", "had", "having", "get", "gets", "got", "getting",
    "been", "being", "am", "are", "is", "was", "were", "be", "having", "has", "had", "do", "did", "does", "doing",
    "some", "any", "no", "yes", "more", "less", "many", "few", "much", "lot", "lots", "a lot",
    "another", "each", "every", "either", "neither", "one", "two", "three", "four", "five",
    "first", "second", "third", "last", "such", "other", "same", "own", "same", "different",
    "about", "against", "between", "through", "during", "before", "after", "above", "below",
    "up", "down", "out", "into", "over", "under", "then", "there", "here", "now", "just", "only",
    "until", "while", "since", "ago", "before", "after", "already", "yet", "still", "almost",
    "so", "very", "too", "quite", "really", "just", "enough", "perhaps", "probably", "actually",
    "almost", "always", "sometimes", "often", "never", "ever", "once", "twice", "thrice",
    "day", "week", "month", "year", "today", "yesterday", "tomorrow", "morning", "evening", "night",
    "include", "including"
};

            // Remove stop words and return distinct lowercase keywords
            var keywords = words.Where(word => !stopWords.Contains(word.ToLower())).Select(word => word.ToLower()).Distinct().ToList();

            return keywords;
        }

        // Method to get bidirectional synonyms for keywords
        private List<string> GetSynonyms(List<string> keywords)
        {
            var synonyms = new HashSet<string>();

            foreach (var keyword in keywords)
            {
                // Add bidirectional synonyms based on specific mappings
                switch (keyword.ToLower())
                {
                    case "failure":
                        synonyms.UnionWith(new[] { "failure", "down" });
                        break;
                    case "monitor":
                        synonyms.UnionWith(new[] { "monitor", "screen" });
                        break;
                    case "phone":
                        synonyms.UnionWith(new[] { "phone", "mobile" });
                        break;
                    case "program":
                        synonyms.UnionWith(new[] { "program", "software", "application" });
                        break;
                    // Add more cases for other keywords and their bidirectional synonyms as needed
                    default:
                        synonyms.Add(keyword.ToLower());
                        break;
                }
            }

            return synonyms.ToList();
        }
    }
}
