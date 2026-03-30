using System;
using System.Threading.Tasks;
using Enjaz.ViewModels;
using Enjaz.Services;

using Enjaz.Models;
using Moq;
using NUnit.Framework;
using Enjaz.Services.Repositories;

namespace Enjaz.Tests
{
    
    public class CertificateViewModelTests
    {
        private Mock<INotificationService> _mockNotificationService;
        private Mock<IPdfService> _mockPdfService;
        private Mock<IReceptionSearchService> _mockReceptionSearchService;

        [SetUp]
        public void Setup()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockPdfService = new Mock<IPdfService>();
            _mockReceptionSearchService = new Mock<IReceptionSearchService>();
        }

        [Test]
        public void CertificatesViewModel_Initialization_SetsDefaultValues()
        {
            // We use null! for concrete services here just to assert constructor logic without deep DB initialization
            var viewModel = new CertificatesViewModel(null!, _mockPdfService.Object, _mockNotificationService.Object, null!, null!, null!, _mockReceptionSearchService.Object);

            Assert.IsNotNull(viewModel.Certificates);
            Assert.IsFalse(viewModel.IsEditing);
            Assert.IsNull(viewModel.SelectedCertificate);
            Assert.AreEqual(1, viewModel.CurrentPage);
        }

        [Test]
        public void AddCertificateCommand_SetsIsEditingTrue()
        {
            var viewModel = new CertificatesViewModel(null!, _mockPdfService.Object, _mockNotificationService.Object, null!, null!, null!, _mockReceptionSearchService.Object);

            viewModel.AddCertificateCommand.Execute(null);

            Assert.IsTrue(viewModel.IsEditing);
            Assert.IsNotNull(viewModel.SelectedCertificate);
            Assert.AreEqual("ENZ-", viewModel.SelectedCertificate.CertificateNumber.Substring(0, 4));
        }
    }
}
