using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Enjaz.Models;
using Enjaz.Services.Repositories;

namespace PerformanceTests
{
    public class LoadTester
    {
        private readonly CertificateRepository _repo;

        public LoadTester(CertificateRepository repo)
        {
            _repo = repo;
        }

        public async Task RunStressTest(int rounds = 100)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"Starting Stress Test: {rounds} paginated fetches...");

            for (int i = 0; i < rounds; i++)
            {
                await _repo.GetCertificatesPaginatedAsync(1, 50);
            }

            sw.Stop();
            Console.WriteLine($"Total Time for {rounds} rounds: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Avg Time per request: {sw.ElapsedMilliseconds / (double)rounds}ms");
        }
    }
}
