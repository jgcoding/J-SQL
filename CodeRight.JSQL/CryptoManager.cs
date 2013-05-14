
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.ComponentModel;
// not storing crypto config in app.config at this time
//using System.Configuration; 
using System.Globalization;
using System.Collections;
using System.Reflection;

namespace Crypto
{
	/// <summary>
	/// There are 3 different types of encryption that are supported when communicating to a Barix device.
	/// 1. No Encryption.  Communications are sent as clear text.
	/// 2. 128-bit AES Encryption
	/// 3. 256-bit AES Encryption
	/// </summary>
	public enum CryptoLevel
	{
		[Description("No Encryption")]
		None = 0,
		[Description("128-bit AES")]
		AES128 = 1,
		[Description("256-bit AES")]
		AES256 = 2
	}

	/// <summary>
	/// Encrypts un-ciphered data and decrypts ciphered data
	/// </summary>
	/// <remarks>
	/// based on the example demonstrating how to encrypt and decrypt sample data using the Aes class on MSDN
	/// http://msdn.microsoft.com/en-us/library/system.security.cryptography.aes.aes.aspx
	/// </remarks>
	public sealed class CryptoManager
	{
		#region constructor

        public CryptoManager() 
        {
            SetEncryptionLevel();
        }		
		
		#endregion

		#region public properties
		/// <summary>
		/// Gets or sets the encryption level as a CryptoLevel enum
		/// </summary>
		public CryptoLevel KeySize
		{
			get
			{
				if (m_keySize == CryptoLevel.None)
				{
					m_keySize = CryptoLevel.AES256;
				}

				return m_keySize;
			}
			set
			{
				m_keySize = value;
			}
		}
		#endregion

		#region public and private methods

		/// <summary>
		/// Loads the crypto properties from hard-coded values. At this time, utilizing an encrypted app.config is not in the specifications.
		/// </summary>
		void ConfigureCryptoInCode()
		{
			m_cryptoSalt = KeySize == CryptoLevel.AES256 ? "deadbeefdeadbeefdeadbeefdeadbeef" : KeySize == CryptoLevel.AES128 ? "deadbeefdeadbeef" : String.Empty;
			m_cryptoHash = "SHA1";
			m_cryptoIterations = 5;
			m_validNonceChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToArray();
			m_nonceLength = 8;
			m_nonce = Convert.FromBase64String("AQIDBAUFBAM=");
		}

		/// <summary>
		/// Sets or resets the encryption level for the encryption instance
		/// </summary>
		/// <param name="keysize">The encryption level as an CryptoLevel enum</param>
		public void SetEncryptionLevel(CryptoLevel keysize = CryptoLevel.AES256)
		{
			// update the keySize property with the requested encryption level
			KeySize = keysize;

			// at this time we are utilizing hard-coded values
			ConfigureCryptoInCode();

			// convert the CryptoLevel to an integer to assist calculationg the crypto properties
			m_encryptionBits = KeySize == CryptoLevel.AES256 ? 256 : KeySize == CryptoLevel.AES128 ? 128 : 0;

			// initialize the IV array
			iv_array = new byte[16];

			// initialize the Key array
			key_array = new byte[m_encryptionBits / 8];

			// generate the Crypto Key and IV
			// create the key and Initial Vector from the 
			OpenSslCompatDeriveBytes crap = new OpenSslCompatDeriveBytes(m_cryptoSalt, m_nonce, m_cryptoHash, m_cryptoIterations);

			stuff_array = crap.GetBytes((m_encryptionBits / 8) + 16);
			Buffer.BlockCopy(stuff_array, 0, key_array, 0, m_encryptionBits / 8);
			Buffer.BlockCopy(stuff_array, m_encryptionBits / 8, iv_array, 0, 16);
		}

		/// <summary>
		/// Generates a random value derived from a set of pre-defined alphanumeric characters.
		/// </summary>
		/// <param name="length">The length of the random value string</param>
		/// <returns>Returns a random value derived from a set of pre-defined alphanumeric characters</returns>
		public byte[] GenerateNonce(Int32 length = 8)
		{
			// set the random seed
			var seed = m_validNonceChars.Count();
			var result = String.Empty;
			// construct the nonce from characters randomly selected from the permissible characters
			for (int i = 0; i < length; i++)
			{
				result += m_validNonceChars[m_random.Next(0, seed)].ToString(CultureInfo.InvariantCulture);
			}

			// convert the results to an ASCII encoded byte[] and return it.
			return Encoding.ASCII.GetBytes(result);
		}

		/// <summary>
		/// Encrypts the un-cyphered data as an array of bytes
		/// </summary>
		/// <param name="clearPayload">The un-ciphered data to be encrypted</param>
		/// <param name="keySize">The encryption level to be used to construct the encryption hash.</param>
		/// <returns>An ciphered array of bytes</returns>
		public byte[] EncryptAES(byte[] clearPayload, CryptoLevel keysize = CryptoLevel.AES256)
		{
			// if the key strength requested is different from the instance
			if (KeySize != KeySize)
			{
				KeySize = keysize;
				// re-calculate the encryption hash using the encryption level requested
				SetEncryptionLevel(KeySize);
			}

			// Check arguments. 
			if (clearPayload == null || clearPayload.Length <= 0)
			{
				throw new ArgumentNullException("clearPayload");
			}
			if (key_array == null || key_array.Length <= 0)
			{
				throw new ArgumentNullException("Key");
			}
			if (iv_array == null || iv_array.Length <= 0)
			{
				throw new ArgumentNullException("Key");
			}

			// declare the output object.
			byte[] encrypted;
			// Create an Aes object 
			// with the specified key and IV. 
			using (Aes aes = Aes.Create())
			{
				aes.Key = key_array;
				aes.IV = iv_array;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;

				// Create a decrytor to perform the stream transform.
				ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

				// Create the streams used for encryption. 

				using (MemoryStream msEncrypt = new MemoryStream())
				{
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						csEncrypt.Write(clearPayload, 0, clearPayload.Length);
						csEncrypt.FlushFinalBlock();
						encrypted = msEncrypt.ToArray();
					}
					msEncrypt.Close();
				}
			}
			// Return the encrypted bytes from the memory stream. 
			return encrypted;
		}

		/// <summary>
		/// Decrypts a cyphered key and returns it as clear text
		/// </summary>
		/// <param name="plainText">The ciphered string values in the form of a byte array</param>
		/// <param name="keySize">the encryption strength - haven't seen this make a difference yet.</param>
		/// <returns>Clear text as a string</returns>
		public byte[] DecryptAES(byte[] cipheredPayload, CryptoLevel keysize = CryptoLevel.AES256)
		{
			// if the key strenght requested is different from the instance
			if (KeySize != KeySize)
			{
				KeySize = keysize;
				// re-calculate the decryption hash using the decryption level requested
				SetEncryptionLevel(KeySize);
			}

			// Check arguments. 
			if (cipheredPayload == null || cipheredPayload.Length <= 0)
			{
				throw new ArgumentNullException("cipheredPayload");
			}
			if (key_array == null || key_array.Length <= 0)
			{
				throw new ArgumentNullException("Key");
			}
			if (iv_array == null || iv_array.Length <= 0)
			{
				throw new ArgumentNullException("Key");
			}

            // declare the output object.
            byte[] decrypted;

			// Create an Aes object 
			// with the specified key and IV. 
			using (Aes aes = Aes.Create())
			{
				aes.Key = key_array;
				aes.IV = iv_array;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;

				// Create a decrytor to perform the stream transform.
				ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

				// Create the streams used for decryption. 
				using (MemoryStream msDecrypt = new MemoryStream(cipheredPayload))
				{
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        csDecrypt.Read(cipheredPayload, 0, cipheredPayload.Length);
                        csDecrypt.Flush();
                        decrypted = msDecrypt.ToArray();
                    }
                    msDecrypt.Close();
				}
			}
            return decrypted;
		}

		///// <summary>
		///// Un-documented: Retrieves crypto properties from an encrypted app.config. This isn't in the specifications at this time.
		///// </summary>
		//void ConfigureCryptoFromAppConfig()
		//{
		//    m_cryptoSalt = KeySize == CryptoLevel.AES256 ? ConfigurationManager.AppSettings["Crypto256Salt"] : KeySize == CryptoLevel.AES128 ? ConfigurationManager.AppSettings["Crypto128Salt"] : String.Empty;
		//    m_cryptoHash = ConfigurationManager.AppSettings["CryptoHash"];
		//    m_cryptoIterations = short.Parse(ConfigurationManager.AppSettings["CryptoIterations"]);
		//    m_validNonceChars = ConfigurationManager.AppSettings["ValidNonceChars"].ToArray();
		//    m_nonceLength = short.Parse(ConfigurationManager.AppSettings["NonceLength"]);
		//    m_nonce = Convert.FromBase64String(ConfigurationManager.AppSettings["Nonce"]);
		//}

		#endregion

		#region instance fields
		CryptoLevel m_keySize;
		String m_cryptoSalt;
		String m_cryptoHash;
		Int16 m_cryptoIterations;
		Char[] m_validNonceChars;
		Int16 m_nonceLength;
		byte[] m_nonce;
		byte[] stuff_array;
		byte[] iv_array;
		byte[] key_array;
		int m_encryptionBits;
		Random m_random = new Random();
		#endregion

	}
}
