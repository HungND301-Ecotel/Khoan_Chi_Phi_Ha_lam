namespace Application.Dto.Catalog;

public class FileStreamResponse
{
    public Stream Data { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public string? FileName { get; set; }
}