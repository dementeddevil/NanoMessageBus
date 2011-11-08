namespace NanoMessageBus.Serialization
{
	using System;
	using System.IO;
	using System.Security.Cryptography;

	public class RijndaelSerializer : SerializerBase
	{
		protected override void SerializePayload(Stream output, object message)
		{
			using (var rijndael = new RijndaelManaged())
			{
				rijndael.Key = this.encryptionKey;
				rijndael.Mode = CipherMode.CBC;
				rijndael.GenerateIV();

				using (var encryptor = rijndael.CreateEncryptor())
				using (var outputWrapper = new IndisposableStream(output))
				using (var encryptionStream = new CryptoStream(outputWrapper, encryptor, CryptoStreamMode.Write))
				{
					outputWrapper.Write(rijndael.IV, 0, rijndael.IV.Length);
					this.inner.Serialize(encryptionStream, message);
					encryptionStream.Flush();
					encryptionStream.FlushFinalBlock();
				}
			}
		}

		protected override object DeserializePayload(Stream input)
		{
			using (var rijndael = new RijndaelManaged())
			{
				rijndael.Key = this.encryptionKey;
				rijndael.IV = GetInitVectorFromStream(input, rijndael.IV.Length);
				rijndael.Mode = CipherMode.CBC;

				using (var decryptor = rijndael.CreateDecryptor())
				using (var decryptedStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
					return this.inner.Deserialize(decryptedStream);
			}
		}
		private static byte[] GetInitVectorFromStream(Stream encrypted, int initVectorSizeInBytes)
		{
			var buffer = new byte[initVectorSizeInBytes];
			encrypted.Read(buffer, 0, buffer.Length);
			return buffer;
		}

		public RijndaelSerializer(ISerializeMessages inner, byte[] encryptionKey)
		{
			if (encryptionKey == null || encryptionKey.Length != KeyLength)
				throw new ArgumentException(Diagnostics.InvalidEncryptionKey, "encryptionKey");

			this.encryptionKey = encryptionKey;
			this.inner = inner;
		}

		private const int KeyLength = 16; // bytes
		private readonly ISerializeMessages inner;
		private readonly byte[] encryptionKey;
	}
}