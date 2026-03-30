using System.Collections.Generic;
using Enjaz.Models;

namespace Enjaz.Services
{
    /// <summary>
    /// واجهة خدمة ملفات PDF
    /// Interface for PDF Service
    /// </summary>
    public interface IPdfService
    {
        bool GenerateCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples, string outputPath);
        bool GenerateAndOpenCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples = null);
        string? SaveCertificateWithDialog(Certificate certificate, IEnumerable<Sample>? samples = null);
        bool SaveCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples, string filePath);
        bool PrintCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples = null);

        bool GenerateReferralLetterPdf(
            IEnumerable<Certificate> certificates, 
            string outputPath, 
            string recipientName, 
            bool includeCertNum, 
            bool includeSupplier, 
            bool includeSamples, 
            bool includeNotification);

        bool PrintReferralLetterPdf(
            IEnumerable<Certificate> certificates,
            string recipientName,
            bool includeCertNum,
            bool includeSupplier,
            bool includeSamples,
            bool includeNotification);
    }
}
