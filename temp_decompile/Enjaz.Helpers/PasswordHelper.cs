using System;
using System.Security.Cryptography;
using System.Text;
using Enjaz.Services;

namespace Enjaz.Helpers;

public static class PasswordHelper
{
	private const int SaltSize = 16;

	private const int KeySize = 32;

	private const int Iterations = 100000;

	public static string HashPassword(string password)
	{
		using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, 16, 100000, HashAlgorithmName.SHA256);
		string value = Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(32));
		string value2 = Convert.ToBase64String(rfc2898DeriveBytes.Salt);
		return $"v2:{100000}:{value2}:{value}";
	}

	public static bool VerifyPassword(string password, string hash)
	{
		if (string.IsNullOrEmpty(hash))
		{
			return false;
		}
		if (!hash.StartsWith("v2:"))
		{
			LoggerService.LogInfo("Verifying password using legacy SHA256 hashing.");
			string text = HashPasswordLegacy(password);
			return text.Equals(hash, StringComparison.OrdinalIgnoreCase);
		}
		try
		{
			string[] array = hash.Split(':');
			if (array.Length != 4)
			{
				return false;
			}
			int num = int.Parse(array[1]);
			byte[] salt = Convert.FromBase64String(array[2]);
			byte[] array2 = Convert.FromBase64String(array[3]);
			LoggerService.LogInfo($"Verifying password using PBKDF2 (v2) with {num} iterations.");
			using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, salt, num, HashAlgorithmName.SHA256);
			byte[] bytes = rfc2898DeriveBytes.GetBytes(32);
			return CryptographicOperations.FixedTimeEquals(array2, bytes);
		}
		catch
		{
			return false;
		}
	}

	private static string HashPasswordLegacy(string password)
	{
		using SHA256 sHA = SHA256.Create();
		byte[] array = sHA.ComputeHash(Encoding.UTF8.GetBytes(password));
		StringBuilder stringBuilder = new StringBuilder();
		byte[] array2 = array;
		foreach (byte b in array2)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
