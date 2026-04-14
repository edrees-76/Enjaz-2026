using System;

namespace Enjaz.Services.Security
{
    /// <summary>
    /// اعدادات نظام التشفير لفصل الثوابت (Crypto Policy Configuration)
    /// </summary>
    public struct CryptoPolicyConfig
    {
        /// <summary>
        /// إصدار خوارزمية التشفير لدعم النسخ المستقبلية (Versioning)
        /// </summary>
        public byte Version { get; }
        
        /// <summary>
        /// حجم المفتاح الأساسي للتشفير (مثال: 256 لـ AES-256)
        /// </summary>
        public int KeySizeBits { get; }
        
        /// <summary>
        /// البصمة السحرية الثابتة للتحقق من الملف (Magic Bytes)
        /// </summary>
        public byte[] MagicBytes { get; }
        
        /// <summary>
        /// الإعدادات القياسية للمنظومة
        /// </summary>
        public static CryptoPolicyConfig Default => new CryptoPolicyConfig(0x01, 256, new byte[] { (byte)'E', (byte)'N', (byte)'J', (byte)'Z' });

        public CryptoPolicyConfig(byte version, int keySizeBits, byte[] magicBytes)
        {
            Version = version;
            KeySizeBits = keySizeBits;
            MagicBytes = magicBytes;
            
            if (MagicBytes.Length != 4)
                throw new ArgumentException("Magic Bytes must be exactly 4 bytes.");
        }
    }
}
