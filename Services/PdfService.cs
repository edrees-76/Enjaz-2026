using System;
using System.IO;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Elements;
using Enjaz.Models;
using QRCoder;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace Enjaz.Services
{
    /// <summary>
    /// خدمة إنشاء ملفات PDF للشهادات
    /// PDF Generation Service for Certificates
    /// </summary>
    public class PdfService : IPdfService
    {
        private readonly IOSService _osService;

        public PdfService(IOSService osService)
        {
            _osService = osService;
            // تفعيل رخصة المجتمع لـ QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public bool GenerateCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples, string outputPath)
        {
            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Portrait());
                        page.Margin(0);
                        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));
                        page.ContentFromRightToLeft();

                        // 0. Watermark (Background Logo with 7% Opacity)
                        var logoWatermarkPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_center.png");
                        if (File.Exists(logoWatermarkPath))
                        {
                            try 
                            {
                                byte[] watermarkBytes;
                                using (var original = System.Drawing.Image.FromFile(logoWatermarkPath))
                                using (var bmp = new System.Drawing.Bitmap(original.Width, original.Height))
                                {
                                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                                    {
                                        var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.12f }; // Increased opacity to 12%
                                        using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                                        {
                                            attributes.SetColorMatrix(matrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                                            g.DrawImage(original, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, original.Width, original.Height, System.Drawing.GraphicsUnit.Pixel, attributes);
                                        }
                                    }
                                    using (var ms = new MemoryStream())
                                    {
                                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                        watermarkBytes = ms.ToArray();
                                    }
                                }
                                // Center on the A4 page
                                page.Background().AlignMiddle().AlignCenter().Width(380).Image(watermarkBytes);
                            }
                            catch (Exception ex)
                            {
                                LoggerService.LogWarning($"Watermark generation failed: {ex.Message}");
                            }
                        }



                        // 1. Black Border exactly 8pt from all edges (Thickness: 1pt)
                        page.Foreground().Padding(8).Border(1).BorderColor(Colors.Black);

                        // 2. HEADER (repeats on all pages) - Internal padding to keep text safe from border (12pt horizontal, 10pt top)
                        page.Header().PaddingHorizontal(12).PaddingTop(10).Element(c => ComposeHeader(c, certificate, samples));

                        // 3. CONTENT - Internal padding to match header (12pt horizontal)
                        page.Content().PaddingHorizontal(12).PaddingVertical(5).Column(column =>
                        {
                            // Group samples into chunks of 6
                            var displaySamples = samples ?? new List<Sample>();
                            var sampleList = displaySamples.ToList();
                            
                            var chunks = sampleList.Select((s, i) => new { Value = s, Index = i })
                                                   .GroupBy(x => x.Index / 6)
                                                   .Select(g => g.Select(x => x.Value).ToList())
                                                   .ToList();

                            foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
                            {
                                column.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Table(table =>
                                {
                                    if (certificate.CertificateType == "عينات بيئية")
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(0.5f); // Root (Rightmost)
                                            columns.RelativeColumn(2f);   // Description
                                            columns.RelativeColumn(1f);   // Sample No
                                            columns.RelativeColumn(1.5f); // Date (Wide)
                                            // Isotopes (Left side)
                                            columns.RelativeColumn(0.8f); // K40
                                            columns.RelativeColumn(0.8f); // Ra226
                                            columns.RelativeColumn(0.8f); // Th232
                                            columns.RelativeColumn(0.8f); // Raeq
                                            columns.RelativeColumn(0.8f); // Cs137
                                        });
    
                                        // Header
                                         table.Header(header =>
                                        {
                                            header.Cell().RowSpan(2).Element(HeaderStyle).Text("ر.ت").FontSize(14).Bold();
                                            header.Cell().RowSpan(2).Element(HeaderStyle).Text("وصف العينة").FontSize(14).Bold();
                                            header.Cell().RowSpan(2).Element(HeaderStyle).Text("رقم العينة").FontSize(14).Bold();
                                            header.Cell().RowSpan(2).Element(HeaderStyle).Text("تاريخ القياس").FontSize(14).Bold();
                                            
                                            // Result Header Spanning 5 Columns
                                            header.Cell().ColumnSpan(5).Element(HeaderStyle).Text("نتيجة القياس ( بيكرل / كجم )").FontSize(14).Bold();
    
                                            // Row 2 (Isotopes)
                                            header.Cell().Element(HeaderStyle).Text(t => { t.DefaultTextStyle(x => x.FontSize(14).Bold()); t.Span("K"); t.Span("40").Subscript(); });
                                            header.Cell().Element(HeaderStyle).Text(t => { t.DefaultTextStyle(x => x.FontSize(14).Bold()); t.Span("Ra"); t.Span("226").Subscript(); }); 
                                            header.Cell().Element(HeaderStyle).Text(t => { t.DefaultTextStyle(x => x.FontSize(14).Bold()); t.Span("Th"); t.Span("232").Subscript(); });
                                            header.Cell().Element(HeaderStyle).Text(t => { t.DefaultTextStyle(x => x.FontSize(14).Bold()); t.Span("R"); t.Span("aeq").Subscript(); }); 
                                            header.Cell().Element(HeaderStyle).Text(t => { t.DefaultTextStyle(x => x.FontSize(14).Bold()); t.Span("Cs"); t.Span("137").Subscript(); });
                                        });
    
                                        // Rows
                                        int chunkRowIndex = 1;
                                        foreach (var sample in chunk)
                                        {
                                            var background = chunkRowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                            
                                            // Match Column Order: Root -> Desc -> Sample# -> Date -> K40 -> Ra226 -> Th232 -> Raeq -> Cs137
                                             table.Cell().Element(c => CellStyle(c, background)).Text(sample.Root.ToString()).FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.Description).FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.SampleNumber).FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text($"{sample.MeasurementDate:yyyy/MM/dd}").FontSize(13);
                                            
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.IsotopeK40).FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.IsotopeRa226).FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.IsotopeTh232).FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.IsotopeRa).FontSize(13); // Raeq
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.IsotopeCs137).FontSize(13);
    
                                            chunkRowIndex++;
                                        }
                                    }
                                    else
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(0.5f); // #
                                            columns.RelativeColumn(2.5f); // Description
                                            columns.RelativeColumn(1f);   // Sample No
                                            columns.RelativeColumn(1.5f); // Date (Wide)
                                            columns.RelativeColumn(2.5f); // Result
                                        });
    
                                        // Header
                                         table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderStyle).Text("م").FontSize(14).Bold();
                                            header.Cell().Element(HeaderStyle).Text("وصف العينة").FontSize(14).Bold();
                                            header.Cell().Element(HeaderStyle).Text("رقم العينة").FontSize(14).Bold();
                                            header.Cell().Element(HeaderStyle).Text("تاريخ القياس").FontSize(14).Bold();
                                            header.Cell().Element(HeaderStyle).Text("النتيجة (بيكريل / كجم)").FontSize(14).Bold();
                                        });
    
                                        // Rows
                                        int chunkRowIndex = 1;
                                        foreach (var sample in chunk)
                                        {
                                            var background = chunkRowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                            
                                             table.Cell().Element(c => CellStyle(c, background)).Text(sample.Root.ToString()).FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.Description).FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.SampleNumber).FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text($"{sample.MeasurementDate:yyyy/MM/dd}").FontSize(13);
                                            table.Cell().Element(c => CellStyle(c, background)).Text(sample.Result).FontSize(13);
    
                                            chunkRowIndex++;
                                        }
                                    }
                                });
    
                                // Add page break if not the last chunk
                                if (index < chunks.Count - 1)
                                {
                                    column.Item().Height(10);
                                     column.Item().AlignLeft().Text("يتبع بالصفحة التالية").FontSize(14).Bold();
                                    column.Item().PageBreak();
                                }
                            }
                            
                            // Spacing between table and statements
                            column.Item().Height(5);
                            
                            // Result statements - immediately after table
                            // النص يتغير حسب عدد العينات (مفرد أو جمع)
                            var totalSampleCount = sampleList.Count;
                            var resultStatement = totalSampleCount == 1 
                                ? "النتيجة تمثل العينة المقاسة فقط" 
                                : "النتائج تمثل العينات المقاسة فقط";
                             column.Item().AlignCenter().Text(resultStatement).FontSize(13).Bold();
                             column.Item().Height(5);
                             column.Item().AlignCenter().Text("أعطيت هذه الشهادة لاستخدامها فيما يسمح به القانون").FontSize(13).Bold();

                            // Dynamic Spacing to push signatures to bottom based on row count
                            var lastChunkCount = chunks.Any() ? chunks.Last().Count : 0;
                             // Reduced dynamic spacing to prevent pushing content off-page (Base 6)
                             float dynamicSpacing = 2 + Math.Max(0, (6 - lastChunkCount) * 15);
                             column.Item().Height(dynamicSpacing);
                            
                            // Signatures
                            column.Item().ShowEntire().Row(row =>
                            {
                                 // Specialist (Right in RTL)
                                row.RelativeItem().Column(c => 
                                {
                                    c.Item().AlignCenter().Text("أخصائي القياس").Bold().FontSize(13);
                                    c.Item().Height(10); // Space for name
                                    c.Item().AlignCenter().Text(certificate.SpecialistName ?? "").FontSize(13).Bold();
                                    c.Item().Height(10); // Space for signature line
                                    c.Item().AlignCenter().Text("......................................").FontSize(13).Bold();
                                });
                                
                                // Section Head (Center)
                                row.RelativeItem().Column(c => 
                                {
                                    c.Item().AlignCenter().Text("رئيس قسم قياس مستوى الإشعاع").Bold().FontSize(13);
                                    c.Item().AlignCenter().Text("في السلع الاستهلاكية").Bold().FontSize(13);
                                    c.Item().Height(10); // Space for name
                                    c.Item().AlignCenter().Text(certificate.SectionHeadName ?? "").FontSize(13).Bold();
                                    c.Item().Height(10); // Space for signature line
                                    c.Item().AlignCenter().Text("......................................").FontSize(13).Bold();
                                });
                                
                                // Manager (Left in RTL)
                                row.RelativeItem().Column(c => 
                                {
                                    c.Item().AlignCenter().Text("مدير إدارة القياسات الإشعاعية").Bold().FontSize(13);
                                    c.Item().Height(10); // Space for name
                                    c.Item().AlignCenter().Text(certificate.ManagerName ?? "").FontSize(13).Bold();
                                    c.Item().Height(10); // Space for signature line
                                    c.Item().AlignCenter().Text("......................................").FontSize(13).Bold();
                                });
                            });
                            
                            static IContainer HeaderStyle(IContainer container)
                            {
                                return container.Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().AlignMiddle().ShowOnce();
                            }
                            
                            static IContainer CellStyle(IContainer container, string backgroundColor)
                            {
                                return container.Border(1).BorderColor(Colors.Black).Background(backgroundColor).Padding(5).AlignCenter().AlignMiddle();
                            }
                        });
                        
                        page.Footer().PaddingHorizontal(12).PaddingBottom(10).Column(col => 
                        {
                             col.Item().PaddingTop(5).AlignCenter().Text("طرابلس - خلة الفرجان - 8 كم طريق قصر بن غشير                info@crmt.ly   WWW.CRMT.LY   +218921151020").FontSize(12);
                        });
                    });
                });

                document.GeneratePdf(outputPath);
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error generating certificate PDF", ex);
                return false;
            }
        }

        public bool GenerateAndOpenCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples = null)
        {
            try
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "Enjaz");
                Directory.CreateDirectory(tempFolder);

                string fileName = $"Certificate_{certificate.CertificateNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string filePath = Path.Combine(tempFolder, fileName);

                if (GenerateCertificatePdf(certificate, samples, filePath))
                {
                    _osService.OpenFile(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error generating and opening PDF", ex);
                return false;
            }
        }

        public string? SaveCertificateWithDialog(Certificate certificate, IEnumerable<Sample>? samples = null)
        {
            string? resultPath = null;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = $"شهادة_{certificate.RecipientName}_{certificate.CertificateNumber}",
                        DefaultExt = ".pdf",
                        Filter = "PDF Documents (.pdf)|*.pdf"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        if (GenerateCertificatePdf(certificate, samples, dialog.FileName))
                        {
                            resultPath = dialog.FileName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("Error in SaveCertificateWithDialog", ex);
                }
            });
            return resultPath;
        }

        public bool SaveCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples, string filePath)
        {
            try
            {
                return GenerateCertificatePdf(certificate, samples, filePath);
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error saving certificate PDF", ex);
                return false;
            }
        }

        private void ComposeHeader(IContainer container, Certificate certificate, IEnumerable<Sample>? samples)
        {
            container.Column(column =>
            {
                 // --- Header Content (Two Logos + Text) ---
                column.Item().Row(row =>
                {
                    // Right Side (Logo 1 - Center Logo)
                    var logoCenterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_center.png");
                    if (File.Exists(logoCenterPath))
                    {
                        row.ConstantItem(130).Height(122).AlignBottom().Image(logoCenterPath).FitArea();
                    }
                    else
                    {
                        row.ConstantItem(130).Height(122);
                    }
                    
                    // Center / Main Text
                    row.RelativeItem().Column(col => 
                    {
                        col.Item().Height(4); // Shift down by 4pt
                        col.Spacing(8);
                        col.Item().AlignCenter().Text("دولة ليبيا").FontSize(18).Bold();
                        col.Item().AlignCenter().Text("مؤسسة الطاقة الذرية").FontSize(18).Bold();
                        col.Item().AlignCenter().Text("مركز القياسات الإشعاعية والتدريب").FontSize(18).Bold();
                    });

                    // Left Side (Logo 2 - Institution Logo)
                    var logoLibyaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_libya.png");
                    if (File.Exists(logoLibyaPath))
                    {
                        row.ConstantItem(130).Height(122).AlignMiddle().AlignLeft().Width(80).Image(logoLibyaPath).FitArea();
                    }
                    else
                    {
                        row.ConstantItem(130).Height(122);
                    }
                });
                // ------------------------------------------

                column.Item().PaddingVertical(15).Row(titleRow => 
                {
                    // 1. Barcode (Visual Right - First in RTL)
                    titleRow.ConstantItem(180).AlignRight().AlignMiddle().Column(barCol => 
                    {
                        try 
                        {
                            var writer = new BarcodeWriter<System.Drawing.Bitmap>
                            {
                                Format = BarcodeFormat.CODE_128,
                                Renderer = new BitmapRenderer(),
                                Options = new EncodingOptions
                                {
                                    Height = 60,
                                    Width = 300, // Increased width for better bar definition
                                    PureBarcode = true,
                                    Margin = 10 // Mandatory Quiet Zone for scanners
                                }
                            };
                            using var img = writer.Write(certificate.CertificateNumber);
                            using var stream = new MemoryStream();
                            img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            var barcodeBytes = stream.ToArray();
                            
                             barCol.Item().AlignCenter().Height(50).Image(barcodeBytes).FitArea();
                             
                             // 1. Date Box under Barcode
                             barCol.Item().PaddingTop(5).AlignCenter().Border(1).BorderColor(Colors.Grey.Medium).Padding(6)
                                 .AlignRight()
                                 .Text(t => 
                                 {
                                     t.Span("التاريخ: ").FontSize(14).Bold();
                                     t.Span($"{certificate.IssueDate:yyyy/MM/dd}").FontSize(14);
                                 });
                        }
                        catch (Exception ex)
                        { 
                            barCol.Item().Text($"Error: {ex.Message}").FontSize(8).FontColor(Colors.Red.Medium);
                        }
                    });

                    // 2. Center Title
                     titleRow.RelativeItem().AlignCenter().AlignMiddle().Text("شهادة تحليل عينات")
                        .FontFamily("Arial").FontSize(22).Bold();

                    // 3. QR Code with Certificate Number (Visual Left - Last in RTL)
                    titleRow.ConstantItem(180).AlignLeft().AlignMiddle().Column(qrCol => 
                    {
                            try
                            {
                            string qrContent = $"الجهة المرسلة: {certificate.Sender}\nالمورد: {certificate.Supplier}\nرقم الإخطار: {certificate.NotificationNumber}\nرقم الإيصال: {certificate.FinancialReceiptNumber}\nرقم الشهادة: {certificate.CertificateNumber}";
                            QRCodeGenerator qrGenerator = new QRCodeGenerator();
                            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                            // Using PngByteQRCode with a smaller scale and explicit background
                            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                            // Setting drawQuietZones: true is CRUCIAL for camera scanning
                            byte[] qrCodeImage = qrCode.GetGraphic(10, new byte[] { 0, 0, 0 }, new byte[] { 255, 255, 255 }, true);
                            
                             qrCol.Item().AlignCenter().Height(55).Width(55).Image(qrCodeImage);
                             
                             // 2. Certificate number box under QR
                             qrCol.Item().PaddingTop(5).AlignCenter().Border(1).BorderColor(Colors.Grey.Medium).Padding(6)
                                 .AlignRight()
                                 .Text(t => 
                                 {
                                     t.Span("رقم الشهادة: ").FontSize(14).Bold();
                                     t.Span(certificate.CertificateNumber).FontSize(14);
                                 });
                            }
                            catch (Exception qrEx)
                            {
                                LoggerService.LogWarning($"QR Code generation failed: {qrEx.Message}");
                            }
                    });
                });
                
                // Spacing before data header
                column.Item().Height(10);
                
                // 1. Recipient Row (Both label and value are now Blue - Dynamic Width)
                 column.Item().Row(row =>
                {
                    row.ConstantItem(110).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background("#1B4F72").Padding(5).AlignCenter().AlignMiddle()
                        .Text("السادة").FontColor(Colors.White).Bold().FontSize(14);
                    row.AutoItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background("#1B4F72").Padding(5).AlignRight().AlignMiddle()
                        .Text(certificate.Sender).FontColor(Colors.White).Bold().FontSize(14);
                });

                column.Item().Height(5); // Increased spacing between Recipient and Supplier

                // 1.1 Supplier Row (Dedicated Row)
                 column.Item().Row(row =>
                {
                    row.ConstantItem(110).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background("#1B4F72").Padding(5).AlignCenter().AlignMiddle()
                        .Text("المورد").FontColor(Colors.White).Bold().FontSize(14);
                    row.AutoItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten3).Padding(5).AlignRight().AlignMiddle()
                        .Text(certificate.Supplier).FontColor(Colors.Black).Bold().FontSize(13);
                });

                column.Item().Height(8); // Spacing between Supplier and next fields



                column.Item().Height(5);

                // 2. Data Header Table (4 Columns Layout - Reorganized)
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(110); // Label 1
                        cols.RelativeColumn();    // Value 1
                        cols.ConstantColumn(110); // Label 2
                        cols.RelativeColumn();    // Value 2
                    });

                    // Utility for styling cells
                    static IContainer Lbl(IContainer container) => container.Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background("#1B4F72").Padding(4).AlignCenter().AlignMiddle();
                    static IContainer Val(IContainer container) => container.Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().AlignMiddle();

                     // Row 1: رقم الإخطار | رقم الإقرار الجمركي
                    table.Cell().Element(Lbl).Text("رقم الإخطار").FontColor(Colors.White).Bold().FontSize(12);
                    table.Cell().Element(Val).Text(certificate.NotificationNumber).FontSize(12);
                    table.Cell().Element(Lbl).Text("رقم الإقرار الجمركي").FontColor(Colors.White).Bold().FontSize(12);
                    table.Cell().Element(Val).Text(certificate.DeclarationNumber).FontSize(12);

                    // Row 2: بلد المنشأ | رقم الإيصال المالي
                    table.Cell().Element(Lbl).Text("بلد المنشأ").FontColor(Colors.White).Bold().FontSize(12);
                    table.Cell().Element(Val).Text(certificate.Origin).FontSize(12);
                    table.Cell().Element(Lbl).Text("رقم الإيصال المالي").FontColor(Colors.White).Bold().FontSize(12);
                    table.Cell().Element(Val).Text(certificate.FinancialReceiptNumber).FontSize(12);
                });

                column.Item().Height(10);
                
                // Analysis Type
                 if (certificate.CertificateType != null && !certificate.CertificateType.Contains("استهلاكية"))
                {
                    column.Item().Border(1).Padding(5).AlignCenter().AlignMiddle().Text(t => { t.DefaultTextStyle(x => x.FontSize(13).Bold()); t.Span("نوع التحليل: "); t.Span(certificate.AnalysisType ?? "تحليل مبدئي (دون الوصول لحالة الاتزان)"); });
                }

                column.Item().Height(10);
                
                var safeSamples = samples ?? new List<Sample>();
                var headerStatement = safeSamples.Count() == 1 
                    ? "نفيدكم بأن النشاط الإشعاعي للعينة المحالة كما يلي:" 
                    : "نفيدكم بأن النشاط الإشعاعي للعينات المحالة كما يلي:";
                 column.Item().Text(headerStatement).Bold().FontSize(13);
                column.Item().Height(5);
            });
        }
        
        public bool PrintCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples = null)
        {
            try
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "Enjaz");
                Directory.CreateDirectory(tempFolder);

                string fileName = $"Print_{certificate.CertificateNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string filePath = Path.Combine(tempFolder, fileName);

                if (GenerateCertificatePdf(certificate, samples, filePath))
                {
                    return PrintPdfFile(filePath);
                }
                return false;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error in PrintCertificatePdf", ex);
                return false;
            }
        }

        public bool GenerateReferralLetterPdf(
            IEnumerable<Certificate> certificates, 
            string outputPath, 
            string recipientName, 
            bool includeCertNum, 
            bool includeSupplier, 
            bool includeSamples, 
            bool includeNotification)
        {
            try
            {
                var certificateList = certificates.ToList();
                var document = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4.Portrait());
                        page.Margin(0);
                        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));
                        page.ContentFromRightToLeft();

                        // Black Border
                        page.Foreground().Padding(8).Border(1).BorderColor(QuestPDF.Helpers.Colors.Black);

                        // Header
                page.Header().PaddingHorizontal(12).PaddingTop(10).Column(column =>
                {
                    // Use the exact same branding header as the certificate
                    column.Item().Row(row =>
                    {
                        // Right Side (Logo 1 - Center Logo)
                        var logoCenterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_center.png");
                        if (File.Exists(logoCenterPath))
                        {
                            row.ConstantItem(130).Height(122).AlignBottom().Image(logoCenterPath).FitArea();
                        }
                        else
                        {
                            row.ConstantItem(130).Height(122);
                        }
                        
                        // Center / Main Text
                        row.RelativeItem().Column(col => 
                        {
                            col.Item().Height(4); // Shift down by 4pt
                            col.Spacing(8);
                            col.Item().AlignCenter().Text("دولة ليبيا").FontSize(18).Bold();
                            col.Item().AlignCenter().Text("مؤسسة الطاقة الذرية").FontSize(18).Bold();
                            col.Item().AlignCenter().Text("مركز القياسات الإشعاعية والتدريب").FontSize(18).Bold();
                        });

                        // Left Side (Logo 2 - Institution Logo)
                        var logoLibyaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_libya.png");
                        if (File.Exists(logoLibyaPath))
                        {
                            row.ConstantItem(130).Height(122).AlignMiddle().AlignLeft().Width(80).Image(logoLibyaPath).FitArea();
                        }
                        else
                        {
                            row.ConstantItem(130).Height(122);
                        }
                    });
                    
                    column.Item().PaddingVertical(15);
                });


                        page.Content().PaddingHorizontal(30).PaddingVertical(20).Column(column =>
                        {
                            // Date and Reference
                            column.Item().AlignLeft().Column(c => 
                            {
                                c.Item().Text($"التاريخ: {DateTime.Now:yyyy/MM/dd}");
                                c.Item().Text($"رقم اشارى : .............................");
                            });

                            column.Item().Height(20);

                            // Recipient
                            column.Item().Text($"السادة/ {recipientName}").FontSize(14).Bold();
                            column.Item().Height(10);
                            column.Item().PaddingRight(20).Text("بعد التحية،،،").FontSize(13).Bold();

                            column.Item().Height(15);

                            // Subject/Body
                    column.Item().AlignCenter().Text("الموضوع : احالة شهادات تحليل اشعاعى لعينات").FontSize(14).Bold().Underline();
                            column.Item().Height(15);
                            column.Item().Text("نحيل إلى حضرتكم شهادات التحليل الإشعاعي الخاصة بالعينات الموضحة بياناتها بالجدول المرفق، وذلك لغرض الاستلام والاطلاع واتخاذ ما يلزم حيالها وفق الإجراءات المعمول بها.").FontSize(12).LineHeight(1.5f);

                            column.Item().Height(20);

                            // Table of certificates
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30); // #
                                    if (includeCertNum) columns.RelativeColumn(2);
                                    if (includeSupplier) columns.RelativeColumn(3);
                                    if (includeSamples) columns.RelativeColumn(2);
                                    if (includeNotification) columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HS).Text("م").Bold();
                                    if (includeCertNum) header.Cell().Element(HS).Text("رقم الشهادة").Bold();
                                    if (includeSupplier) header.Cell().Element(HS).Text("اسم المورد").Bold();
                                    if (includeSamples) header.Cell().Element(HS).Text("أرقام العينات").Bold();
                                    if (includeNotification) header.Cell().Element(HS).Text("رقم الإخطار").Bold();
                                    
                                    QuestPDF.Infrastructure.IContainer HS(QuestPDF.Infrastructure.IContainer container) => 
                                        container.Border(1).Padding(5).AlignCenter().Background(QuestPDF.Helpers.Colors.Grey.Lighten3);
                                });

                                int i = 1;
                                foreach (var cert in certificateList)
                                {
                                    table.Cell().Element(CS).Text(i++.ToString());
                                    if (includeCertNum) table.Cell().Element(CS).Text(cert.CertificateNumber);
                                    if (includeSupplier) table.Cell().Element(CS).Text(cert.Supplier ?? "---");
                                    if (includeSamples) table.Cell().Element(CS).Text(cert.Samples != null && cert.Samples.Any() ? string.Join(", ", cert.Samples.Select(s => s.SampleNumber)) : "---");
                                    if (includeNotification) table.Cell().Element(CS).Text(cert.NotificationNumber ?? "---");

                                    QuestPDF.Infrastructure.IContainer CS(QuestPDF.Infrastructure.IContainer container) => 
                                        container.Border(1).Padding(5).AlignCenter();
                                }
                            });

                            column.Item().Height(30);


                            column.Item().Height(40);


                            // Signatures
                            column.Item().ShowEntire().Row(row =>
                            {
                                row.RelativeItem().PaddingRight(200).Column(c => 
                                {
                                    c.Item().AlignLeft().Text("اسم المستلم: .........................").Bold().FontSize(13);
                                    c.Item().Height(15);
                                    c.Item().AlignLeft().Text("الـــتوقـــيع: ..........................").Bold().FontSize(13);
                                });
                            });
                        });

                        page.Footer().PaddingHorizontal(12).PaddingBottom(10).Column(col => 
                        {
                             col.Item().PaddingTop(5).AlignCenter().Text("طرابلس - خلة الفرجان - 8 كم طريق قصر بن غشير                info@crmt.ly   WWW.CRMT.LY   +218921151020").FontSize(10);
                        });


                    });
                });

                document.GeneratePdf(outputPath);
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error generating referral letter PDF", ex);
                return false;
            }
        }

        public bool PrintReferralLetterPdf(
            IEnumerable<Certificate> certificates,
            string recipientName,
            bool includeCertNum,
            bool includeSupplier,
            bool includeSamples,
            bool includeNotification)
        {
            try
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "Enjaz");
                Directory.CreateDirectory(tempFolder);

                string fileName = $"Print_Referral_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string filePath = Path.Combine(tempFolder, fileName);

                if (GenerateReferralLetterPdf(certificates, filePath, recipientName, includeCertNum, includeSupplier, includeSamples, includeNotification))
                {
                    return PrintPdfFile(filePath);
                }
                return false;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error in PrintReferralLetterPdf", ex);
                return false;
            }
        }

        private bool PrintPdfFile(string filePath)
        {
            try
            {
                bool success = false;
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null) return false;

                dispatcher.Invoke(() =>
                {
                    try
                    {
                        var printDialog = new System.Windows.Controls.PrintDialog();
                        if (printDialog.ShowDialog() == true)
                        {
                            _osService.PrintFileTo(filePath, printDialog.PrintQueue.FullName);
                            success = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogError("Error during printing process", ex);
                    }
                });
                return success;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error in PrintPdfFile", ex);
                return false;
            }
        }


        static IContainer HeaderStyle(IContainer container)
        {
            return container.Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().AlignMiddle().ShowOnce();
        }
        
        static IContainer CellStyle(IContainer container, string backgroundColor)
        {
            return container.Border(1).BorderColor(Colors.Black).Background(backgroundColor).Padding(5).AlignCenter().AlignMiddle();
        }
    }
}
