using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Crypto;
using System.Text;

public partial class UserDefinedFunctions
{
    /// <summary>
    /// Converts a byte[] into a decrypted string using the encryption strength provided.
    /// </summary>
    /// <param name="payload">The byte[] to be decrypted</param>
    /// <param name="strength">The encryption strength to apply: 0 = none, 1 = AES 128, 2 = AES 256</param>
    /// <returns></returns>
    [SqlFunction]
    public static string SqlDecrypt(string json, int strength)
    {
        byte[] bson = Convert.FromBase64String(json);
        CryptoManager crypto = new CryptoManager();

        byte[] jbytes = crypto.DecryptAES(bson, (CryptoLevel)Convert.ToByte(strength));
        return Encoding.UTF8.GetString(jbytes);
    }

    /// <summary>
    /// Converts a string into an encrypted byte[] using the encryption strength provided.
    /// </summary>
    /// <param name="json">The string to be encrypted</param>
    /// <param name="strength">The encryption strength to apply: 0 = none, 1 = AES 128, 2 = AES 256</param>
    /// <returns>byte[]</returns>
    [SqlFunction]
    public static byte[] SqlEncrypt(string json, int strength)
    {        
        CryptoManager crypto = new CryptoManager();
        return crypto.EncryptAES(Encoding.UTF8.GetBytes(json), (CryptoLevel)Convert.ToByte(strength));
    }
};

