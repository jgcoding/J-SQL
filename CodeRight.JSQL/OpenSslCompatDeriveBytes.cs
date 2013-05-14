using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Crypto
{
	/// <summary>
	/// Derives a key from a password using an OpenSSL-compatible version of the PBKDF1 algorithm.
	/// </summary>
	/// <remarks>
	/// based on the OpenSSL EVP_BytesToKey method for generating key and iv
	/// http://www.openssl.org/docs/crypto/EVP_BytesToKey.html
	/// </remarks>
	public class OpenSslCompatDeriveBytes : DeriveBytes
	{
		private readonly byte[] _data;
		private readonly HashAlgorithm _hash;
		private readonly int _iterations;
		private readonly byte[] _salt;
		private byte[] _currentHash;
		private int _hashListReadIndex;
		private List<byte> _hashList;

		/// <summary>
		/// Initializes a new instance of the <see cref="Crypto.OpenSslCompatDeriveBytes"/> class specifying the password, key salt, hash name, and iterations to use to derive the key.
		/// </summary>
		/// <param name="password">The password for which to derive the key.</param>
		/// <param name="salt">The key salt to use to derive the key.</param>
		/// <param name="hashName">The name of the hash algorithm for the operation. (e.g. MD5 or SHA1)</param>
		/// <param name="iterations">The number of iterations for the operation.</param>
		public OpenSslCompatDeriveBytes(string password, byte[] salt, string hashName, int iterations)
			: this(new UTF8Encoding(false).GetBytes(password), salt, hashName, iterations)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Crypto.OpenSslCompatDeriveBytes"/> class specifying the password, key salt, hash name, and iterations to use to derive the key.
		/// </summary>
		/// <param name="password">The password for which to derive the key.</param>
		/// <param name="salt">The key salt to use to derive the key.</param>
		/// <param name="hashName">The name of the hash algorithm for the operation. (e.g. MD5 or SHA1)</param>
		/// <param name="iterations">The number of iterations for the operation.</param>
		public OpenSslCompatDeriveBytes(byte[] password, byte[] salt, string hashName, int iterations)
		{
			if (iterations <= 0)
				throw new ArgumentOutOfRangeException("iterations", iterations, "iterations is out of range. Positive number required");

			_data = password;
			_salt = salt;
			_hash = HashAlgorithm.Create(hashName);
			_iterations = iterations;
		}

		/// <summary>
		/// Returns a pseudo-random key from a password, salt and iteration count.
		/// </summary>
		/// <param name="cb">The number of pseudo-random key bytes to generate.</param>
		/// <returns>A byte array filled with pseudo-random key bytes.</returns>
		public override byte[] GetBytes(int cb)
		{
			if (cb <= 0)
				throw new ArgumentOutOfRangeException("cb", cb, "cb is out of range. Positive number required.");

			if (_currentHash == null)
			{
				_hashList = new List<byte>();
				_currentHash = new byte[0];
				_hashListReadIndex = 0;

				int preHashLength = _data.Length + ((_salt != null) ? _salt.Length : 0);

				var preHash = new byte[preHashLength];

				Buffer.BlockCopy(_data, 0, preHash, 0, _data.Length);
				if (_salt != null)
					Buffer.BlockCopy(_salt, 0, preHash, _data.Length, _salt.Length);

				_currentHash = _hash.ComputeHash(preHash);

				for (int i = 1; i < _iterations; i++)
				{
					_currentHash = _hash.ComputeHash(_currentHash);
				}

				_hashList.AddRange(_currentHash);
			}

			while (_hashList.Count < (cb + _hashListReadIndex))
			{
				int preHashLength = _currentHash.Length + _data.Length + ((_salt != null) ? _salt.Length : 0);
				var preHash = new byte[preHashLength];

				Buffer.BlockCopy(_currentHash, 0, preHash, 0, _currentHash.Length);
				Buffer.BlockCopy(_data, 0, preHash, _currentHash.Length, _data.Length);
				if (_salt != null)
					Buffer.BlockCopy(_salt, 0, preHash, _currentHash.Length + _data.Length, _salt.Length);

				_currentHash = _hash.ComputeHash(preHash);

				for (int i = 1; i < _iterations; i++)
				{
					_currentHash = _hash.ComputeHash(_currentHash);
				}

				_hashList.AddRange(_currentHash);
			}

			byte[] dst = new byte[cb];
			_hashList.CopyTo(_hashListReadIndex, dst, 0, cb);
			_hashListReadIndex += cb;

			return dst;
		}

		/// <summary>
		/// Resets the state of the operation.
		/// </summary>
		public override void Reset()
		{
			_hashListReadIndex = 0;
			_currentHash = null;
			_hashList = null;
		}
	}
}
