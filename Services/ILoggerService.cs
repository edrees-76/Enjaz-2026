namespace Enjaz.Services
{
    /// <summary>
    /// واجهة خدمة التسجيل — قابلة للحقن عبر DI وللاختبار عبر Mock
    /// Logger service interface — DI-injectable and testable via Mock
    /// </summary>
    public interface ILoggerService
    {
        void LogInfo(string message);
        void LogError(string message, Exception? ex = null);
        void LogWarning(string message);
    }
}
