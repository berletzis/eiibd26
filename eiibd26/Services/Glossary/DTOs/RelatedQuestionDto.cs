namespace eiibd26.Services.Glossary.DTOs
{
    public class RelatedQuestionDto
    {
        public System.Guid Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Slug { get; set; } = "";
        public int Score { get; set; }
    }
}
