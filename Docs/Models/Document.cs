namespace Docs.Models
{
    public class Document
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
        public string  Content { get; set; }
        public double? Score { get; set; }
    }

    public class Category
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class Author
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class Tag
    {
        public string Value { get; set; }
        public string Label { get; set; }
    }
}
