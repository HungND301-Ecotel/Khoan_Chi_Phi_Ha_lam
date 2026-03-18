namespace Shared.Constants;

public class MessageCommon
{
    public const string GetDataSuccess = "Get data successfully";
    public const string CreateSuccess = "Create successful";
    public const string CreateFailed = "Create failed";
    public const string UpdateSuccess = "Update successful";
    public const string UpdateFailed = "Update failed";
    public const string DeleteSuccess = "Delete successful";
    public const string DeleteFailed = "Delete failed";
    public const string DataNotFound = "Can't find entity to get";
    public const string CannotUpdateStatus = "Can't update status";
    public const string UserUpdateStatusSuccessful = "Status ser has been updated successfully";
    public const string UploadSuccess = "Upload successfully";
    public const string ExportSuccess = "Export successfully";
    public const string ImportSuccess = "Import successfully";

    public static string SetEntityNotFound(string entityName, object id) => $"{entityName} id {id} not found.";

    public static string SetEntityExists(string entityName, object id) => $"{entityName} id {id} has been exist.";
}