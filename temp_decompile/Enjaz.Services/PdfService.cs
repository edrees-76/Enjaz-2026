using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Enjaz.Models;
using Microsoft.Win32;
using QRCoder;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace Enjaz.Services;

public class PdfService : IPdfService
{
	private readonly IOSService _osService;

	public PdfService(IOSService osService)
	{
		_osService = osService;
		Settings.License = LicenseType.Community;
	}

	public bool GenerateCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples, string outputPath)
	{
		try
		{
			Document document = Document.Create(delegate(IDocumentContainer container)
			{
				container.Page(delegate(PageDescriptor page)
				{
					page.Size(PageSizes.A4.Portrait());
					page.Margin(0f);
					page.DefaultTextStyle((TextStyle x) => x.FontFamily("Arial").FontSize(11f));
					page.ContentFromRightToLeft();
					string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_center.png");
					if (File.Exists(text))
					{
						try
						{
							byte[] imageData;
							using (System.Drawing.Image image = System.Drawing.Image.FromFile(text))
							{
								using Bitmap bitmap = new Bitmap(image.Width, image.Height);
								using (Graphics graphics = Graphics.FromImage(bitmap))
								{
									ColorMatrix newColorMatrix = new ColorMatrix
									{
										Matrix33 = 0.12f
									};
									using ImageAttributes imageAttributes = new ImageAttributes();
									imageAttributes.SetColorMatrix(newColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
									graphics.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
								}
								using MemoryStream memoryStream = new MemoryStream();
								bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
								imageData = memoryStream.ToArray();
							}
							page.Background().AlignMiddle().AlignCenter()
								.Width(380f)
								.Image(imageData);
						}
						catch (Exception ex2)
						{
							LoggerService.LogWarning("Watermark generation failed: " + ex2.Message);
						}
					}
					page.Foreground().Padding(8f).Border(1f)
						.BorderColor(Colors.Black);
					page.Header().PaddingHorizontal(12f).PaddingTop(10f)
						.Element(delegate(IContainer c)
						{
							ComposeHeader(c, certificate, samples);
						}, "c => ComposeHeader(c, certificate, samples)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 88);
					page.Content().PaddingHorizontal(12f).PaddingVertical(5f)
						.Column(delegate(ColumnDescriptor column)
						{
							IEnumerable<Sample> source = samples ?? new List<Sample>();
							List<Sample> list = source.ToList();
							List<List<Sample>> list2 = (from x in list.Select((Sample s, int i) => new
								{
									Value = s,
									Index = i
								})
								group x by x.Index / 6 into g
								select g.Select(x => x.Value).ToList()).ToList();
							foreach (var item in list2.Select((List<Sample> c, int i) => (c: c, i: i)))
							{
								var (chunk, num) = item;
								column.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
									.Table(delegate(TableDescriptor table)
									{
										if (certificate.CertificateType == "عينات بيئية")
										{
											table.ColumnsDefinition(delegate(TableColumnsDefinitionDescriptor columns)
											{
												columns.RelativeColumn(0.5f);
												columns.RelativeColumn(2f);
												columns.RelativeColumn();
												columns.RelativeColumn(1.5f);
												columns.RelativeColumn(0.8f);
												columns.RelativeColumn(0.8f);
												columns.RelativeColumn(0.8f);
												columns.RelativeColumn(0.8f);
												columns.RelativeColumn(0.8f);
											});
											table.Header(delegate(TableCellDescriptor header)
											{
												((IContainer)header.Cell().RowSpan(2u)).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 125).Text("ر.ت").FontSize(14f)
													.Bold();
												((IContainer)header.Cell().RowSpan(2u)).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 126).Text("وصف العينة").FontSize(14f)
													.Bold();
												((IContainer)header.Cell().RowSpan(2u)).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 127).Text("رقم العينة").FontSize(14f)
													.Bold();
												((IContainer)header.Cell().RowSpan(2u)).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 128).Text("تاريخ القياس").FontSize(14f)
													.Bold();
												((IContainer)header.Cell().ColumnSpan(5u)).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 131).Text("نتيجة القياس ( بيكرل / كجم )").FontSize(14f)
													.Bold();
												((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 134).Text(delegate(TextDescriptor t)
												{
													t.DefaultTextStyle((TextStyle x) => x.FontSize(14f).Bold());
													t.Span("K");
													t.Span("40").Subscript();
												});
												((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 135).Text(delegate(TextDescriptor t)
												{
													t.DefaultTextStyle((TextStyle x) => x.FontSize(14f).Bold());
													t.Span("Ra");
													t.Span("226").Subscript();
												});
												((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 136).Text(delegate(TextDescriptor t)
												{
													t.DefaultTextStyle((TextStyle x) => x.FontSize(14f).Bold());
													t.Span("Th");
													t.Span("232").Subscript();
												});
												((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 137).Text(delegate(TextDescriptor t)
												{
													t.DefaultTextStyle((TextStyle x) => x.FontSize(14f).Bold());
													t.Span("R");
													t.Span("aeq").Subscript();
												});
												((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 138).Text(delegate(TextDescriptor t)
												{
													t.DefaultTextStyle((TextStyle x) => x.FontSize(14f).Bold());
													t.Span("Cs");
													t.Span("137").Subscript();
												});
											});
											int num3 = 1;
											{
												foreach (Sample item2 in chunk)
												{
													QuestPDF.Infrastructure.Color background = ((num3 % 2 == 0) ? Colors.White : Colors.Grey.Lighten4);
													table.Cell().Element((IContainer c) => CellStyle(c, background), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 148).Text(item2.Root.ToString())
														.FontSize(13f);
													table.Cell().Element((IContainer c) => CellStyle(c, background), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 149).Text(item2.Description)
														.FontSize(13f);
													table.Cell().Element((IContainer c) => CellStyle(c, background), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 150).Text(item2.SampleNumber)
														.FontSize(13f);
													table.Cell().Element((IContainer c) => CellStyle(c, background), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 151).Text($"{item2.MeasurementDate:yyyy/MM/dd}")
														.FontSize(13f);
													table.Cell().Element((IContainer c) => CellStyle(c, background), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 153).Text(item2.IsotopeK40)
														.FontSize(13f);
													table.Cell().Element((IContainer c) => CellStyle(c, background), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 154).Text(item2.IsotopeRa226)
														.FontSize(13f);
													table.Cell().Element((IContainer c) => CellStyle(c, background), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 155).Text(item2.IsotopeTh232)
														.FontSize(13f);
													table.Cell().Element((IContainer c) => CellStyle(c, background), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 156).Text(item2.IsotopeRa)
														.FontSize(13f);
													table.Cell().Element((IContainer c) => CellStyle(c, background), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 157).Text(item2.IsotopeCs137)
														.FontSize(13f);
													num3++;
												}
												return;
											}
										}
										table.ColumnsDefinition(delegate(TableColumnsDefinitionDescriptor columns)
										{
											columns.RelativeColumn(0.5f);
											columns.RelativeColumn(2.5f);
											columns.RelativeColumn();
											columns.RelativeColumn(1.5f);
											columns.RelativeColumn(2.5f);
										});
										table.Header(delegate(TableCellDescriptor header)
										{
											((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 176).Text("م").FontSize(14f)
												.Bold();
											((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 177).Text("وصف العينة").FontSize(14f)
												.Bold();
											((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 178).Text("رقم العينة").FontSize(14f)
												.Bold();
											((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 179).Text("تاريخ القياس").FontSize(14f)
												.Bold();
											((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HeaderStyle, "HeaderStyle", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 180).Text("النتيجة (بيكريل / كجم)").FontSize(14f)
												.Bold();
										});
										int num4 = 1;
										foreach (Sample item3 in chunk)
										{
											QuestPDF.Infrastructure.Color background2 = ((num4 % 2 == 0) ? Colors.White : Colors.Grey.Lighten4);
											table.Cell().Element((IContainer c) => CellStyle(c, background2), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 189).Text(item3.Root.ToString())
												.FontSize(13f);
											table.Cell().Element((IContainer c) => CellStyle(c, background2), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 190).Text(item3.Description)
												.FontSize(13f);
											table.Cell().Element((IContainer c) => CellStyle(c, background2), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 191).Text(item3.SampleNumber)
												.FontSize(13f);
											table.Cell().Element((IContainer c) => CellStyle(c, background2), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 192).Text($"{item3.MeasurementDate:yyyy/MM/dd}")
												.FontSize(13f);
											table.Cell().Element((IContainer c) => CellStyle(c, background2), "c => CellStyle(c, background)", "GenerateCertificatePdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 193).Text(item3.Result)
												.FontSize(13f);
											num4++;
										}
									});
								if (num < list2.Count - 1)
								{
									column.Item().Height(10f);
									column.Item().AlignLeft().Text("يتبع بالصفحة التالية")
										.FontSize(14f)
										.Bold();
									column.Item().PageBreak();
								}
							}
							column.Item().Height(5f);
							int count = list.Count;
							string text2 = ((count == 1) ? "النتيجة تمثل العينة المقاسة فقط" : "النتائج تمثل العينات المقاسة فقط");
							column.Item().AlignCenter().Text(text2)
								.FontSize(13f)
								.Bold();
							column.Item().Height(5f);
							column.Item().AlignCenter().Text("أعطيت هذه الشهادة لاستخدامها فيما يسمح به القانون")
								.FontSize(13f)
								.Bold();
							int num2 = (list2.Any() ? list2.Last().Count : 0);
							float value = 2 + Math.Max(0, (6 - num2) * 15);
							column.Item().Height(value);
							column.Item().ShowEntire().Row(delegate(RowDescriptor row)
							{
								row.RelativeItem().Column(delegate(ColumnDescriptor c)
								{
									c.Item().AlignCenter().Text("أخصائي القياس")
										.Bold()
										.FontSize(13f);
									c.Item().Height(10f);
									c.Item().AlignCenter().Text(certificate.SpecialistName ?? "")
										.FontSize(13f)
										.Bold();
									c.Item().Height(10f);
									c.Item().AlignCenter().Text("......................................")
										.FontSize(13f)
										.Bold();
								});
								row.RelativeItem().Column(delegate(ColumnDescriptor c)
								{
									c.Item().AlignCenter().Text("رئيس قسم قياس مستوى الإشعاع")
										.Bold()
										.FontSize(13f);
									c.Item().AlignCenter().Text("في السلع الاستهلاكية")
										.Bold()
										.FontSize(13f);
									c.Item().Height(10f);
									c.Item().AlignCenter().Text(certificate.SectionHeadName ?? "")
										.FontSize(13f)
										.Bold();
									c.Item().Height(10f);
									c.Item().AlignCenter().Text("......................................")
										.FontSize(13f)
										.Bold();
								});
								row.RelativeItem().Column(delegate(ColumnDescriptor c)
								{
									c.Item().AlignCenter().Text("مدير إدارة القياسات الإشعاعية")
										.Bold()
										.FontSize(13f);
									c.Item().Height(10f);
									c.Item().AlignCenter().Text(certificate.ManagerName ?? "")
										.FontSize(13f)
										.Bold();
									c.Item().Height(10f);
									c.Item().AlignCenter().Text("......................................")
										.FontSize(13f)
										.Bold();
								});
							});
						});
					page.Footer().PaddingHorizontal(12f).PaddingBottom(10f)
						.Column(delegate(ColumnDescriptor col)
						{
							col.Item().PaddingTop(5f).AlignCenter()
								.Text("طرابلس - خلة الفرجان - 8 كم طريق قصر بن غشير                info@crmt.ly   WWW.CRMT.LY   +218921151020")
								.FontSize(12f);
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
		static IContainer CellStyle(IContainer container, string backgroundColor)
		{
			return container.Border(1f).BorderColor(Colors.Black).Background(backgroundColor)
				.Padding(5f)
				.AlignCenter()
				.AlignMiddle();
		}
		static IContainer HeaderStyle(IContainer container)
		{
			return container.Border(1f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3)
				.Padding(5f)
				.AlignCenter()
				.AlignMiddle()
				.ShowOnce();
		}
	}

	public bool GenerateAndOpenCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples = null)
	{
		try
		{
			string text = Path.Combine(Path.GetTempPath(), "Enjaz");
			Directory.CreateDirectory(text);
			string path = $"Certificate_{certificate.CertificateNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
			string text2 = Path.Combine(text, path);
			if (GenerateCertificatePdf(certificate, samples, text2))
			{
				_osService.OpenFile(text2);
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
		string resultPath = null;
		((DispatcherObject)Application.Current).Dispatcher.Invoke((Action)delegate
		{
			try
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					FileName = "شهادة_" + certificate.RecipientName + "_" + certificate.CertificateNumber,
					DefaultExt = ".pdf",
					Filter = "PDF Documents (.pdf)|*.pdf"
				};
				if (saveFileDialog.ShowDialog() == true && GenerateCertificatePdf(certificate, samples, saveFileDialog.FileName))
				{
					resultPath = saveFileDialog.FileName;
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
		container.Column(delegate(ColumnDescriptor column)
		{
			column.Item().Row(delegate(RowDescriptor row)
			{
				string text2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_center.png");
				if (File.Exists(text2))
				{
					row.ConstantItem(130f).Height(122f).AlignBottom()
						.Image(text2)
						.FitArea();
				}
				else
				{
					row.ConstantItem(130f).Height(122f);
				}
				row.RelativeItem().Column(delegate(ColumnDescriptor col)
				{
					col.Item().Height(4f);
					col.Spacing(8f);
					col.Item().AlignCenter().Text("دولة ليبيا")
						.FontSize(18f)
						.Bold();
					col.Item().AlignCenter().Text("مؤسسة الطاقة الذرية")
						.FontSize(18f)
						.Bold();
					col.Item().AlignCenter().Text("مركز القياسات الإشعاعية والتدريب")
						.FontSize(18f)
						.Bold();
				});
				string text3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_libya.png");
				if (File.Exists(text3))
				{
					row.ConstantItem(130f).Height(122f).AlignMiddle()
						.AlignLeft()
						.Width(80f)
						.Image(text3)
						.FitArea();
				}
				else
				{
					row.ConstantItem(130f).Height(122f);
				}
			});
			column.Item().PaddingVertical(15f).Row(delegate(RowDescriptor titleRow)
			{
				titleRow.ConstantItem(180f).AlignRight().AlignMiddle()
					.Column(delegate(ColumnDescriptor barCol)
					{
						try
						{
							BarcodeWriter<Bitmap> barcodeWriter = new BarcodeWriter<Bitmap>
							{
								Format = BarcodeFormat.CODE_128,
								Renderer = new BitmapRenderer(),
								Options = new EncodingOptions
								{
									Height = 60,
									Width = 300,
									PureBarcode = true,
									Margin = 10
								}
							};
							using Bitmap bitmap = barcodeWriter.Write(certificate.CertificateNumber);
							using MemoryStream memoryStream = new MemoryStream();
							bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
							byte[] imageData = memoryStream.ToArray();
							barCol.Item().AlignCenter().Height(50f)
								.Image(imageData)
								.FitArea();
							barCol.Item().PaddingTop(5f).AlignCenter()
								.Border(1f)
								.BorderColor(Colors.Grey.Medium)
								.Padding(6f)
								.AlignRight()
								.Text(delegate(TextDescriptor t)
								{
									t.Span("التاريخ: ").FontSize(14f).Bold();
									t.Span($"{certificate.IssueDate:yyyy/MM/dd}").FontSize(14f);
								});
						}
						catch (Exception ex)
						{
							barCol.Item().Text("Error: " + ex.Message).FontSize(8f)
								.FontColor(Colors.Red.Medium);
						}
					});
				titleRow.RelativeItem().AlignCenter().AlignMiddle()
					.Text("شهادة تحليل عينات")
					.FontFamily("Arial")
					.FontSize(22f)
					.Bold();
				titleRow.ConstantItem(180f).AlignLeft().AlignMiddle()
					.Column(delegate(ColumnDescriptor qrCol)
					{
						try
						{
							string plainText = $"الجهة المرسلة: {certificate.Sender}\nالمورد: {certificate.Supplier}\nرقم الإخطار: {certificate.NotificationNumber}\nرقم الإيصال: {certificate.FinancialReceiptNumber}\nرقم الشهادة: {certificate.CertificateNumber}";
							QRCodeGenerator qRCodeGenerator = new QRCodeGenerator();
							QRCodeData data = qRCodeGenerator.CreateQrCode(plainText, QRCodeGenerator.ECCLevel.Q);
							PngByteQRCode pngByteQRCode = new PngByteQRCode(data);
							byte[] graphic = pngByteQRCode.GetGraphic(10, new byte[3], new byte[3] { 255, 255, 255 });
							qrCol.Item().AlignCenter().Height(55f)
								.Width(55f)
								.Image(graphic);
							qrCol.Item().PaddingTop(5f).AlignCenter()
								.Border(1f)
								.BorderColor(Colors.Grey.Medium)
								.Padding(6f)
								.AlignRight()
								.Text(delegate(TextDescriptor t)
								{
									t.Span("رقم الشهادة: ").FontSize(14f).Bold();
									t.Span(certificate.CertificateNumber).FontSize(14f);
								});
						}
						catch (Exception ex)
						{
							LoggerService.LogWarning("QR Code generation failed: " + ex.Message);
						}
					});
			});
			column.Item().Height(10f);
			column.Item().Row(delegate(RowDescriptor row)
			{
				row.ConstantItem(110f).Border(0.5f).BorderColor(Colors.Grey.Lighten1)
					.Background("#1B4F72")
					.Padding(5f)
					.AlignCenter()
					.AlignMiddle()
					.Text("السادة")
					.FontColor(Colors.White)
					.Bold()
					.FontSize(14f);
				row.AutoItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1)
					.Background("#1B4F72")
					.Padding(5f)
					.AlignRight()
					.AlignMiddle()
					.Text(certificate.Sender)
					.FontColor(Colors.White)
					.Bold()
					.FontSize(14f);
			});
			column.Item().Height(5f);
			column.Item().Row(delegate(RowDescriptor row)
			{
				row.ConstantItem(110f).Border(0.5f).BorderColor(Colors.Grey.Lighten1)
					.Background("#1B4F72")
					.Padding(5f)
					.AlignCenter()
					.AlignMiddle()
					.Text("المورد")
					.FontColor(Colors.White)
					.Bold()
					.FontSize(14f);
				row.AutoItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1)
					.Background(Colors.Grey.Lighten3)
					.Padding(5f)
					.AlignRight()
					.AlignMiddle()
					.Text(certificate.Supplier)
					.FontColor(Colors.Black)
					.Bold()
					.FontSize(13f);
			});
			column.Item().Height(8f);
			column.Item().Height(5f);
			column.Item().Table(delegate(TableDescriptor table)
			{
				table.ColumnsDefinition(delegate(TableColumnsDefinitionDescriptor cols)
				{
					cols.ConstantColumn(110f);
					cols.RelativeColumn();
					cols.ConstantColumn(110f);
					cols.RelativeColumn();
				});
				((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)Lbl, "Lbl", "ComposeHeader", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 520).Text("رقم الإخطار").FontColor(Colors.White)
					.Bold()
					.FontSize(12f);
				((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)Val, "Val", "ComposeHeader", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 521).Text(certificate.NotificationNumber).FontSize(12f);
				((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)Lbl, "Lbl", "ComposeHeader", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 522).Text("رقم الإقرار الجمركي").FontColor(Colors.White)
					.Bold()
					.FontSize(12f);
				((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)Val, "Val", "ComposeHeader", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 523).Text(certificate.DeclarationNumber).FontSize(12f);
				((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)Lbl, "Lbl", "ComposeHeader", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 526).Text("بلد المنشأ").FontColor(Colors.White)
					.Bold()
					.FontSize(12f);
				((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)Val, "Val", "ComposeHeader", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 527).Text(certificate.Origin).FontSize(12f);
				((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)Lbl, "Lbl", "ComposeHeader", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 528).Text("رقم الإيصال المالي").FontColor(Colors.White)
					.Bold()
					.FontSize(12f);
				((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)Val, "Val", "ComposeHeader", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 529).Text(certificate.FinancialReceiptNumber).FontSize(12f);
			});
			column.Item().Height(10f);
			if (certificate.CertificateType != null && !certificate.CertificateType.Contains("استهلاكية"))
			{
				column.Item().Border(1f).Padding(5f)
					.AlignCenter()
					.AlignMiddle()
					.Text(delegate(TextDescriptor t)
					{
						t.DefaultTextStyle((TextStyle x) => x.FontSize(13f).Bold());
						t.Span("نوع التحليل: ");
						t.Span(certificate.AnalysisType ?? "تحليل مبدئي (دون الوصول لحالة الاتزان)");
					});
			}
			column.Item().Height(10f);
			IEnumerable<Sample> source = samples ?? new List<Sample>();
			string text = ((source.Count() == 1) ? "نفيدكم بأن النشاط الإشعاعي للعينة المحالة كما يلي:" : "نفيدكم بأن النشاط الإشعاعي للعينات المحالة كما يلي:");
			column.Item().Text(text).Bold()
				.FontSize(13f);
			column.Item().Height(5f);
		});
		static IContainer Lbl(IContainer element)
		{
			return element.Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background("#1B4F72")
				.Padding(4f)
				.AlignCenter()
				.AlignMiddle();
		}
		static IContainer Val(IContainer element)
		{
			return element.Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten3)
				.Padding(4f)
				.AlignCenter()
				.AlignMiddle();
		}
	}

	public bool PrintCertificatePdf(Certificate certificate, IEnumerable<Sample>? samples = null)
	{
		try
		{
			string text = Path.Combine(Path.GetTempPath(), "Enjaz");
			Directory.CreateDirectory(text);
			string path = $"Print_{certificate.CertificateNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
			string text2 = Path.Combine(text, path);
			if (GenerateCertificatePdf(certificate, samples, text2))
			{
				return PrintPdfFile(text2);
			}
			return false;
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Error in PrintCertificatePdf", ex);
			return false;
		}
	}

	public bool GenerateReferralLetterPdf(IEnumerable<Certificate> certificates, string outputPath, string recipientName, bool includeCertNum, bool includeSupplier, bool includeSamples, bool includeNotification)
	{
		try
		{
			List<Certificate> certificateList = certificates.ToList();
			Document document = Document.Create(delegate(IDocumentContainer container)
			{
				container.Page(delegate(PageDescriptor page)
				{
					page.Size(PageSizes.A4.Portrait());
					page.Margin(0f);
					page.DefaultTextStyle((TextStyle x) => x.FontFamily("Arial").FontSize(11f));
					page.ContentFromRightToLeft();
					page.Foreground().Padding(8f).Border(1f)
						.BorderColor(Colors.Black);
					page.Header().PaddingHorizontal(12f).PaddingTop(10f)
						.Column(delegate(ColumnDescriptor column)
						{
							column.Item().Row(delegate(RowDescriptor row)
							{
								string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_center.png");
								if (File.Exists(text))
								{
									row.ConstantItem(130f).Height(122f).AlignBottom()
										.Image(text)
										.FitArea();
								}
								else
								{
									row.ConstantItem(130f).Height(122f);
								}
								row.RelativeItem().Column(delegate(ColumnDescriptor col)
								{
									col.Item().Height(4f);
									col.Spacing(8f);
									col.Item().AlignCenter().Text("دولة ليبيا")
										.FontSize(18f)
										.Bold();
									col.Item().AlignCenter().Text("مؤسسة الطاقة الذرية")
										.FontSize(18f)
										.Bold();
									col.Item().AlignCenter().Text("مركز القياسات الإشعاعية والتدريب")
										.FontSize(18f)
										.Bold();
								});
								string text2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_libya.png");
								if (File.Exists(text2))
								{
									row.ConstantItem(130f).Height(122f).AlignMiddle()
										.AlignLeft()
										.Width(80f)
										.Image(text2)
										.FitArea();
								}
								else
								{
									row.ConstantItem(130f).Height(122f);
								}
							});
							column.Item().PaddingVertical(15f);
						});
					page.Content().PaddingHorizontal(30f).PaddingVertical(20f)
						.Column(delegate(ColumnDescriptor column)
						{
							column.Item().AlignLeft().Column(delegate(ColumnDescriptor c)
							{
								c.Item().Text($"التاريخ: {DateTime.Now:yyyy/MM/dd}");
								c.Item().Text("رقم اشارى : .............................");
							});
							column.Item().Height(20f);
							column.Item().Text("السادة/ " + recipientName).FontSize(14f)
								.Bold();
							column.Item().Height(10f);
							column.Item().PaddingRight(20f).Text("بعد التحية،،،")
								.FontSize(13f)
								.Bold();
							column.Item().Height(15f);
							column.Item().AlignCenter().Text("الموضوع : احالة شهادات تحليل اشعاعى لعينات")
								.FontSize(14f)
								.Bold()
								.Underline();
							column.Item().Height(15f);
							column.Item().Text("نحيل إلى حضرتكم شهادات التحليل الإشعاعي الخاصة بالعينات الموضحة بياناتها بالجدول المرفق، وذلك لغرض الاستلام والاطلاع واتخاذ ما يلزم حيالها وفق الإجراءات المعمول بها.").FontSize(12f)
								.LineHeight(1.5f);
							column.Item().Height(20f);
							column.Item().Table(delegate(TableDescriptor table)
							{
								table.ColumnsDefinition(delegate(TableColumnsDefinitionDescriptor columns)
								{
									columns.ConstantColumn(30f);
									if (includeCertNum)
									{
										columns.RelativeColumn(2f);
									}
									if (includeSupplier)
									{
										columns.RelativeColumn(3f);
									}
									if (includeSamples)
									{
										columns.RelativeColumn(2f);
									}
									if (includeNotification)
									{
										columns.RelativeColumn(2f);
									}
								});
								table.Header(delegate(TableCellDescriptor header)
								{
									((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HS, "HS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 680).Text("م").Bold();
									if (includeCertNum)
									{
										((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HS, "HS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 681).Text("رقم الشهادة").Bold();
									}
									if (includeSupplier)
									{
										((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HS, "HS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 682).Text("اسم المورد").Bold();
									}
									if (includeSamples)
									{
										((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HS, "HS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 683).Text("أرقام العينات").Bold();
									}
									if (includeNotification)
									{
										((IContainer)header.Cell()).Element((Func<IContainer, IContainer>)HS, "HS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 684).Text("رقم الإخطار").Bold();
									}
								});
								int num = 1;
								foreach (Certificate item in certificateList)
								{
									((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)CS, "CS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 693).Text(num++.ToString());
									if (includeCertNum)
									{
										((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)CS, "CS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 694).Text(item.CertificateNumber);
									}
									if (includeSupplier)
									{
										((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)CS, "CS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 695).Text(item.Supplier ?? "---");
									}
									if (includeSamples)
									{
										((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)CS, "CS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 696).Text((item.Samples != null && item.Samples.Any()) ? string.Join(", ", item.Samples.Select((Sample s) => s.SampleNumber)) : "---");
									}
									if (includeNotification)
									{
										((IContainer)table.Cell()).Element((Func<IContainer, IContainer>)CS, "CS", "GenerateReferralLetterPdf", "D:\\منظومة انجاز 2026\\Services\\PdfService.cs", 697).Text(item.NotificationNumber ?? "---");
									}
								}
							});
							column.Item().Height(30f);
							column.Item().Height(40f);
							column.Item().ShowEntire().Row(delegate(RowDescriptor row)
							{
								row.RelativeItem().PaddingRight(200f).Column(delegate(ColumnDescriptor c)
								{
									c.Item().AlignLeft().Text("اسم المستلم: .........................")
										.Bold()
										.FontSize(13f);
									c.Item().Height(15f);
									c.Item().AlignLeft().Text("الـــتوقـــيع: ..........................")
										.Bold()
										.FontSize(13f);
								});
							});
						});
					page.Footer().PaddingHorizontal(12f).PaddingBottom(10f)
						.Column(delegate(ColumnDescriptor col)
						{
							col.Item().PaddingTop(5f).AlignCenter()
								.Text("طرابلس - خلة الفرجان - 8 كم طريق قصر بن غشير                info@crmt.ly   WWW.CRMT.LY   +218921151020")
								.FontSize(10f);
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
		static IContainer CS(IContainer container)
		{
			return container.Border(1f).Padding(5f).AlignCenter();
		}
		static IContainer HS(IContainer container)
		{
			return container.Border(1f).Padding(5f).AlignCenter()
				.Background(Colors.Grey.Lighten3);
		}
	}

	public bool PrintReferralLetterPdf(IEnumerable<Certificate> certificates, string recipientName, bool includeCertNum, bool includeSupplier, bool includeSamples, bool includeNotification)
	{
		try
		{
			string text = Path.Combine(Path.GetTempPath(), "Enjaz");
			Directory.CreateDirectory(text);
			string path = $"Print_Referral_{DateTime.Now:yyyyMMddHHmmss}.pdf";
			string text2 = Path.Combine(text, path);
			if (GenerateReferralLetterPdf(certificates, text2, recipientName, includeCertNum, includeSupplier, includeSamples, includeNotification))
			{
				return PrintPdfFile(text2);
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
			Application current = Application.Current;
			Dispatcher val = ((current != null) ? ((DispatcherObject)current).Dispatcher : null);
			if (val == null)
			{
				return false;
			}
			val.Invoke((Action)delegate
			{
				try
				{
					PrintDialog printDialog = new PrintDialog();
					if (printDialog.ShowDialog() == true)
					{
						_osService.PrintFileTo(filePath, printDialog.PrintQueue.FullName);
						success = true;
					}
				}
				catch (Exception ex2)
				{
					LoggerService.LogError("Error during printing process", ex2);
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

	private static IContainer HeaderStyle(IContainer container)
	{
		return container.Border(1f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3)
			.Padding(5f)
			.AlignCenter()
			.AlignMiddle()
			.ShowOnce();
	}

	private static IContainer CellStyle(IContainer container, string backgroundColor)
	{
		return container.Border(1f).BorderColor(Colors.Black).Background(backgroundColor)
			.Padding(5f)
			.AlignCenter()
			.AlignMiddle();
	}
}
