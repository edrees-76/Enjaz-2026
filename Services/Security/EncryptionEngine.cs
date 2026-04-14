using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Enjaz.Services.Security
{
    /// <summary>
    /// المحرك النقي للتشفير وفك التشفير.
    /// مسؤول فقط عن تحويل التدفقات المغلفة بـ DPAPI و AES-GCM.
    /// التصميم: Stateless Pure Service (لا يعرف التخزين أو الملفات).
    /// </summary>
    public class EncryptionEngine
    {
        private readonly CryptoPolicyConfig _policy;
        // حجم كتلة الدفق لمنع استهلاك الذاكرة (4 ميجابايت)
        private const int ChunkSize = 4 * 1024 * 1024; 

        public EncryptionEngine(CryptoPolicyConfig policy)
        {
            _policy = policy;
        }

        public void EncryptStream(Stream inputStream, Stream outputStream)
        {
            // 1. توليد مفتاح التشفير الخاص بهذه النسخة (Per-Backup Key)
            byte[] rawKey = new byte[_policy.KeySizeBits / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(rawKey);
            }

            // 2. تغليف المفتاح عبر DPAPI (يربط المفتاح بالمستخدم الحالي)
            byte[] wrappedKey = ProtectedData.Protect(rawKey, null, DataProtectionScope.CurrentUser);

            // 3. كتابة الترويسة الموثوقة (Versioned Header with Magic Bytes)
            outputStream.Write(_policy.MagicBytes, 0, _policy.MagicBytes.Length);
            outputStream.WriteByte(_policy.Version);
            
            byte[] keyLengthBytes = BitConverter.GetBytes(wrappedKey.Length);
            outputStream.Write(keyLengthBytes, 0, 4);
            outputStream.Write(wrappedKey, 0, wrappedKey.Length);

            // 4. التشفير باستخدام AES-GCM مع تجزئة الدفق (Chunking) لمنع امتلاء الذاكرة
            using (var aesGcm = new AesGcm(rawKey, 16))
            {
                byte[] buffer = new byte[ChunkSize];
                int bytesRead;

                while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // كتابة حجم الكتلة الخام لفك التشفير لاحقًا
                    outputStream.Write(BitConverter.GetBytes(bytesRead), 0, 4);

                    byte[] nonce = new byte[12];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(nonce);
                    }
                    outputStream.Write(nonce, 0, nonce.Length);

                    byte[] plaintextChunk = new byte[bytesRead];
                    Array.Copy(buffer, plaintextChunk, bytesRead);

                    byte[] ciphertext = new byte[bytesRead];
                    byte[] tag = new byte[16];

                    // التشفير للكتلة المنفردة (Authentication Tag المتولد لكل كتلة)
                    aesGcm.Encrypt(nonce, plaintextChunk, ciphertext, tag);

                    // كتابة البيانات المشفرة
                    outputStream.Write(ciphertext, 0, ciphertext.Length);
                    
                    // كتابة تاغ المصادقة الصارم (Strict Finalization per chunk)
                    outputStream.Write(tag, 0, tag.Length);
                }
            }

            // تعزيز مسح المفتاح الأصلي الحساس من الذاكرة
            Array.Clear(rawKey, 0, rawKey.Length);
        }

        public void DecryptStream(Stream inputStream, Stream outputStream)
        {
            // 1. التحقق من الترويسة (Magic Bytes Validation)
            byte[] magic = new byte[4];
            if (inputStream.Read(magic, 0, 4) != 4 || !magic.SequenceEqual(_policy.MagicBytes))
                throw new InvalidOperationException("الملف تالف أو البصمة السحرية غير متطابقة. (Invalid Magic Bytes)");

            // 2. التحقق من الإصدار للرجعية
            int version = inputStream.ReadByte();
            if (version != _policy.Version)
                throw new InvalidOperationException($"إصدار التشفير غير مدعوم أو غير موثوق. المتوقع: {_policy.Version}");

            // 3. قراءة وفك تغليف مفتاح DPAPI
            byte[] keyLengthBytes = new byte[4];
            if (inputStream.Read(keyLengthBytes, 0, 4) != 4)
                throw new InvalidOperationException("بنية الترويسة غير مكتملة.");

            int keyLength = BitConverter.ToInt32(keyLengthBytes, 0);
            byte[] wrappedKey = new byte[keyLength];
            if (inputStream.Read(wrappedKey, 0, keyLength) != keyLength)
                throw new InvalidOperationException("مفتاح التشفير المغلف مفقود أو غير مكتمل.");

            byte[] rawKey;
            try
            {
                rawKey = ProtectedData.Unprotect(wrappedKey, null, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("تعذر فك الارتباط عبر DPAPI. ربما تم تغيير المستخدم أو النظام.", ex);
            }

            // 4. فك التشفير واستخراج الكتل Streaming Decryption
            using (var aesGcm = new AesGcm(rawKey, 16))
            {
                byte[] chunkSizeBytes = new byte[4];
                while (inputStream.Read(chunkSizeBytes, 0, 4) == 4)
                {
                    int chunkSize = BitConverter.ToInt32(chunkSizeBytes, 0);

                    byte[] nonce = new byte[12];
                    if (inputStream.Read(nonce, 0, 12) != 12)
                        throw new InvalidOperationException("مؤشر التشويش (Nonce) تالف.");

                    byte[] ciphertext = new byte[chunkSize];
                    if (inputStream.Read(ciphertext, 0, chunkSize) != chunkSize)
                        throw new InvalidOperationException("الكتلة المشفرة تالفة أو مبتورة.");

                    byte[] tag = new byte[16];
                    if (inputStream.Read(tag, 0, 16) != 16)
                        throw new InvalidOperationException("وسم المصادقة (Authentication Tag) الخاص بالكتلة مفقود.");

                    byte[] plaintext = new byte[chunkSize];

                    try
                    {
                        // Strict validation over the chunk during streaming
                        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
                    }
                    catch (CryptographicException)
                    {
                        throw new InvalidOperationException("فشل اختبار المصادقة (Authentication Tag) للكتلة، النسخة فاسدة أو تم التلاعب بها.");
                    }

                    outputStream.Write(plaintext, 0, plaintext.Length);
                }
            }

            // تنظيف سريع وآمن
            Array.Clear(rawKey, 0, rawKey.Length);
        }
    }
}
