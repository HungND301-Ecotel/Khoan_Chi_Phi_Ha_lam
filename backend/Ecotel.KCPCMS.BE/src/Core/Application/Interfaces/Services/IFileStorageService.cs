using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Dto.Catalog;

namespace Application.Interfaces.Services;

public interface IFileStorageService
{
    Task<FileStreamResponse> GetFileStreamAsync(string presignedUrl);
}
