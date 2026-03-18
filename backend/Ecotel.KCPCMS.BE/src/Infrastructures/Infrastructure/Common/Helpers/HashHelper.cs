using System.Security.Cryptography;

namespace Infrastructure.Common.Helpers;

public static class HashHelper
{
    public static string CreateHash(byte[] data, string hashAlgorithm = "SHA1", int trimByteCount = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm);

        var algorithm = CryptoConfig.CreateFromName(hashAlgorithm) as HashAlgorithm ?? throw new ArgumentException("Unrecognized hash name");

        if (trimByteCount <= 0 || data.Length <= trimByteCount)
        {
            return Convert.ToHexString(algorithm.ComputeHash(data));
        }

        byte[] newData = new byte[trimByteCount];
        Array.Copy(data, newData, trimByteCount);

        return Convert.ToHexString(algorithm.ComputeHash(newData));
    }
}