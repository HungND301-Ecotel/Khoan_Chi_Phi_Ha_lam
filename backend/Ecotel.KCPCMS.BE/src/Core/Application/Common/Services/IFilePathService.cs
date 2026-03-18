namespace Application.Common.Services;

public interface IFilePathService
{
    string GetFileStorageFullPath(string path);

    TDto? BindFullPaths<TDto>(TDto? dto) where TDto : class;

    List<TDto?> BindFullPaths<TDto>(IEnumerable<TDto> dtos) where TDto : class;
}