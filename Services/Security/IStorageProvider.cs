using System.IO;
using System.Threading.Tasks;

namespace Enjaz.Services.Security
{
    /// <summary>
    /// طبقة البنية التحتية المنعزلة للحفظ والتخزين (Infrastructure - Storage Layer)
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// يُرجع مسار الحفظ المباشر
        /// </summary>
        Stream CreateBackupStream(string destinationPath);
        
        /// <summary>
        /// يُرجع مسار القراءة للنسخة
        /// </summary>
        Stream OpenBackupStream(string sourcePath);
        
        /// <summary>
        /// إعادة تسمية ذرية للملف (Atomic Rename)
        /// </summary>
        void CommitBackup(string tempPath, string finalPath);

        /// <summary>
        /// حذف آمن للملف المؤقت في حال الإلغاء أو الفشل
        /// </summary>
        void AbortBackup(string tempPath);
    }

    public class LocalSystemStorageProvider : IStorageProvider
    {
        public Stream CreateBackupStream(string destinationPath)
        {
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        }

        public Stream OpenBackupStream(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("لم يتم العثور على ملف النسخة الاحتياطية", sourcePath);

            return new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void CommitBackup(string tempPath, string finalPath)
        {
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }
            File.Move(tempPath, finalPath);
        }

        public void AbortBackup(string tempPath)
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch { /* التجاهل الآمن لمحاولة الحذف عند الإجهاض */ }
            }
        }
    }
}
