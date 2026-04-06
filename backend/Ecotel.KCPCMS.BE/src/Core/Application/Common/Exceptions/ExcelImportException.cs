using System.Net;

namespace Application.Common.Exceptions;

public class ExcelImportException : CustomException
{
    private const string DefaultMessage = "Dữ liệu import Excel không hợp lệ.";

    public ExcelImportException(IEnumerable<string>? errors = null)
        : base(DefaultMessage, null, HttpStatusCode.BadRequest)
    {
        if (errors == null)
        {
            return;
        }

        foreach (var error in errors)
        {
            AddError(error);
        }
    }
}
