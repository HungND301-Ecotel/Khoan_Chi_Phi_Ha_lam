using System.Security.Cryptography;
using System.Text;

namespace Application.Utility;

public class Utils
{
    private const string HashKey = "UnB4JaKrhzP521sixoCL22gb3rHZxmMQ";
    private const string SilPassword = "IsoleteStoragePassword";
    private const string DbPassword = "DatabasePassword";
    private const string SilSalt = "IsoleteStorageSalt";
    private const string DbSalt = "DatabaseSalt";

    public static string ComputeHash(string password)
    {
        return ComputeHash(HashKey, password);
    }

    public static string ComputeHash(string hashKey, string stringToHash)
    {
        byte[] key = Encoding.UTF8.GetBytes(hashKey);

        using var hmac = new HMACSHA512(key);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
        string hashString = Convert.ToBase64String(hash);

        return hashString;
    }

    public static string NormalizeEmail(string email)
    {
        return string.IsNullOrEmpty(email) ? email : email.Trim().ToUpperInvariant();
    }

    public static string NormalizeUserName(string username)
    {
        return string.IsNullOrEmpty(username) ? username : username.Trim().ToUpperInvariant();
    }

    public static string NormalizeString(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return email;
        }

        return email.Trim();
    }

    public static bool CheckEmailIsValid(string email)
    {
        try
        {
            NormalizeString(email);

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string NormalizeVietnameseString(string str = "")
    {
        string[] vietnameseSigns =
        [
            "aAeEoOuUiIdDyY",
            "áàạảãâấầậẩẫăắằặẳẵ",
            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
            "éèẹẻẽêếềệểễ",
            "ÉÈẸẺẼÊẾỀỆỂỄ",
            "óòọỏõôốồộổỗơớờợởỡ",
            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
            "úùụủũưứừựửữ",
            "ÚÙỤỦŨƯỨỪỰỬỮ",
            "íìịỉĩ",
            "ÍÌỊỈĨ",
            "đ",
            "Đ",
            "ýỳỵỷỹ",
            "ÝỲỴỶỸ"
        ];

        for (int i = 1; i < vietnameseSigns.Length; i++)
        {
            for (int j = 0; j < vietnameseSigns[i].Length; j++)
            {
                str = str.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }
        }

        return str;
    }

    public static string Duration60(double time100)
    {
        double hours = Math.Floor(time100);
        double minutes = (time100 - hours) * 60 / 100;
        return $"{hours + minutes:0.00}".Replace(".", ":");
    }

    public static double Duration100(double time60)
    {
        double hours = Math.Floor(time60);
        double minutes = (time60 - hours) * 100 / 60;
        return hours + minutes;
    }

    public static bool IsImage(string fileName)
    {
        var imageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG", ".JPEG" };

        return imageExtensions.Contains(Path.GetExtension(fileName).ToUpperInvariant());
    }

    public static string DecryptString(string input, bool forDatabase = false)
    {
        string password = forDatabase ? DbPassword : SilPassword;
        string salt = forDatabase ? DbSalt : SilSalt;

        return DecryptString(input, password, salt);
    }

    public static string DecryptString(string input, string password, string salt)
    {
        try
        {
            // Our symmetric encryption algorithm
#pragma warning disable SYSLIB0021
            using Aes aes = new AesManaged();
#pragma warning restore SYSLIB0021
            // We're using the PBKDF2 standard for password-based key generation
            var deriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt));
            // Setting parameters
            aes.BlockSize = aes.LegalBlockSizes[0].MaxSize;
            aes.KeySize = aes.LegalKeySizes[0].MaxSize;
            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
            aes.IV = deriveBytes.GetBytes(aes.BlockSize / 8);

            using var decryptionStream = new MemoryStream();
            using (var decrypt = new CryptoStream(decryptionStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                byte[] encryptedData = Convert.FromBase64String(input);
                decrypt.Write(encryptedData, 0, encryptedData.Length);
                decrypt.Flush();
            }
            byte[] decryptedData = decryptionStream.ToArray();
            return Encoding.UTF8.GetString(decryptedData, 0, decryptedData.Length);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static int GetRandomRange(int min, int max)
    {
        var rnd = new Random();
        int val = rnd.Next(min, max + 1);
        return val + DateTime.Now.Millisecond;
    }

    public static string EncryptString(string input, bool forDatabase = false)
    {
        string password = forDatabase ? DbPassword : SilPassword;
        string salt = forDatabase ? DbSalt : SilSalt;

        return EncryptString(input, password, salt);
    }


    public static string EncryptString(string input, string password, string salt)
    {
        try
        {
            // Our symmetric encryption algorithm
#pragma warning disable SYSLIB0021
            using Aes aes = new AesManaged();
#pragma warning restore SYSLIB0021
            // We're using the PBKDF2 standard for password-based key generation
            var deriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt));
            // Setting parameters
            aes.BlockSize = aes.LegalBlockSizes[0].MaxSize;
            aes.KeySize = aes.LegalKeySizes[0].MaxSize;
            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
            aes.IV = deriveBytes.GetBytes(aes.BlockSize / 8);

            using var encryptionStream = new MemoryStream();
            using (var encrypt = new CryptoStream(encryptionStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] utfData = Encoding.UTF8.GetBytes(input);
                encrypt.Write(utfData, 0, utfData.Length);
                encrypt.FlushFinalBlock();
            }
            return Convert.ToBase64String(encryptionStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }
}