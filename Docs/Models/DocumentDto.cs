namespace Docs.Models
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public Author[] Authors { get; set; }
        public string CreatedDate { get; set; }
        public string ModifiedDate { get; set; }
        public Category Category { get; set; }
        public Category SubCategory { get; set; }
        public Tag[] Tags { get; set; }
        public string Version { get; set; }
        public string Url { get; set; }
        public double? Score { get; set; }
    }
}
