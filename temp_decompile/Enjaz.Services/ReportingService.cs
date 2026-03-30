using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enjaz.Models;
using Enjaz.Services.Repositories;

namespace Enjaz.Services;

public class ReportingService
{
	private readonly CertificateRepository _certificateRepository;

	public ReportingService(CertificateRepository certificateRepository)
	{
		_certificateRepository = certificateRepository;
	}

	public async Task<List<Certificate>> GetCertificatesByDateRangeAsync(DateTime startDate, DateTime endDate)
	{
		return await _certificateRepository.GetCertificatesByDateRangeAsync(startDate, endDate);
	}

	public async Task<List<Sample>> GetSamplesByDateRangeAsync(DateTime startDate, DateTime endDate)
	{
		return await _certificateRepository.GetSamplesByDateRangeAsync(startDate, endDate);
	}

	public async Task<ReportSummary> GetReportSummaryAsync(DateTime startDate, DateTime endDate)
	{
		List<Certificate> certificates = await GetCertificatesByDateRangeAsync(startDate, endDate);
		List<Sample> samples = await GetSamplesByDateRangeAsync(startDate, endDate);
		return new ReportSummary
		{
			TotalCertificates = certificates.Count,
			TotalSamples = samples.Count,
			EnvironmentalCertificates = certificates.Count((Certificate c) => c.CertificateType?.Contains("بيئية") ?? false),
			ConsumableCertificates = certificates.Count((Certificate c) => c.CertificateType?.Contains("استهلاكية") ?? false),
			EnvironmentalSamples = certificates.Sum((Certificate c) => c.EnvironmentalSampleCount),
			ConsumableSamples = certificates.Sum((Certificate c) => c.ConsumableSampleCount),
			StartDate = startDate,
			EndDate = endDate
		};
	}

	public async Task<List<DistributionItem>> GetTopSuppliersAsync(DateTime startDate, DateTime endDate, int top = 10)
	{
		return (from c in await GetCertificatesByDateRangeAsync(startDate, endDate)
			where !string.IsNullOrEmpty(c.Supplier)
			group c by c.Supplier into g
			select new DistributionItem
			{
				Name = g.Key,
				Count = g.Count()
			} into x
			orderby x.Count descending
			select x).Take(top).ToList();
	}

	public async Task<List<DistributionItem>> GetTopSendersAsync(DateTime startDate, DateTime endDate, int top = 10)
	{
		return (from c in await GetCertificatesByDateRangeAsync(startDate, endDate)
			where !string.IsNullOrEmpty(c.Sender)
			group c by c.Sender into g
			select new DistributionItem
			{
				Name = g.Key,
				Count = g.Count()
			} into x
			orderby x.Count descending
			select x).Take(top).ToList();
	}

	public async Task<List<DistributionItem>> GetTopOriginsAsync(DateTime startDate, DateTime endDate, int top = 10)
	{
		return (from c in await GetCertificatesByDateRangeAsync(startDate, endDate)
			where !string.IsNullOrEmpty(c.Origin)
			group c by c.Origin into g
			select new DistributionItem
			{
				Name = g.Key,
				Count = g.Count()
			} into x
			orderby x.Count descending
			select x).Take(top).ToList();
	}

	public async Task<List<string>> GetUniqueSendersAsync()
	{
		return await _certificateRepository.GetDistinctFieldValuesAsync("Sender");
	}

	public async Task<(ReportSummary Summary, List<Certificate> Certificates)> GetSenderReportDataAsync(DateTime startDate, DateTime endDate, string sender)
	{
		List<Certificate> senderCerts = (await GetCertificatesByDateRangeAsync(startDate, endDate)).Where((Certificate c) => c.Sender == sender).ToList();
		ReportSummary summary = new ReportSummary
		{
			TotalCertificates = senderCerts.Count,
			TotalSamples = senderCerts.Sum((Certificate c) => c.SampleCount),
			EnvironmentalCertificates = senderCerts.Count(delegate(Certificate c)
			{
				string certificateType = c.CertificateType;
				int result;
				if (certificateType == null || !certificateType.Contains("بيئية"))
				{
					string certificateType2 = c.CertificateType;
					if (certificateType2 == null || !certificateType2.Contains("بيييه"))
					{
						result = ((c.CertificateType?.Contains("بيئيه") ?? false) ? 1 : 0);
						goto IL_004c;
					}
				}
				result = 1;
				goto IL_004c;
				IL_004c:
				return (byte)result != 0;
			}),
			ConsumableCertificates = senderCerts.Count((Certificate c) => c.CertificateType?.Contains("استهلاكية") ?? false),
			EnvironmentalSamples = senderCerts.Sum((Certificate c) => c.EnvironmentalSampleCount),
			ConsumableSamples = senderCerts.Sum((Certificate c) => c.ConsumableSampleCount),
			StartDate = startDate,
			EndDate = endDate
		};
		return (Summary: summary, Certificates: senderCerts);
	}

	public async Task<List<DistributionItem>> GetMonthlyDistributionAsync(DateTime startDate, DateTime endDate)
	{
		return (from c in await GetCertificatesByDateRangeAsync(startDate, endDate)
			group c by new
			{
				c.IssueDate.Year,
				c.IssueDate.Month
			} into g
			select new DistributionItem
			{
				Name = $"{g.Key.Year}/{g.Key.Month:D2}",
				Count = g.Count()
			} into x
			orderby x.Name
			select x).ToList();
	}

	public async Task<List<DistributionItem>> GetTopAnalysisTypesAsync(DateTime startDate, DateTime endDate, int top = 10)
	{
		return (from c in await GetCertificatesByDateRangeAsync(startDate, endDate)
			where !string.IsNullOrEmpty(c.AnalysisType)
			group c by c.AnalysisType into g
			select new DistributionItem
			{
				Name = g.Key,
				Count = g.Count()
			} into x
			orderby x.Count descending
			select x).Take(top).ToList();
	}
}
