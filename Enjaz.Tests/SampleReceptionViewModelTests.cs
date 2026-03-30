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
    
    public class SampleReceptionViewModelTests
    {
        private Mock<INotificationService> _mockNotificationService;

        [SetUp]
        public void Setup()
        {
            _mockNotificationService = new Mock<INotificationService>();
        }

        [Test]
        public void SampleReceptionsViewModel_Initialization_SetsDefaultValues()
        {
            var viewModel = new SampleReceptionsViewModel(null!, null!, null!, _mockNotificationService.Object, null!);

            Assert.IsNotNull(viewModel.Receptions);
            Assert.IsFalse(viewModel.IsEditing);
            Assert.IsFalse(viewModel.IsSelectingType);
            Assert.IsNull(viewModel.SelectedReception);
        }

        [Test]
        public void AddReceptionCommand_ShowsTypeSelection()
        {
            var viewModel = new SampleReceptionsViewModel(null!, null!, null!, _mockNotificationService.Object, null!);

            viewModel.AddReceptionCommand.Execute(null);

            Assert.IsTrue(viewModel.IsSelectingType);
            Assert.IsFalse(viewModel.IsEditing);
        }

        [Test]
        public void ConfirmTypeCommand_InitializesEditing_WhenTypeIsSelected()
        {
            var viewModel = new SampleReceptionsViewModel(null!, null!, null!, _mockNotificationService.Object, null!);
            viewModel.SelectedCertificateType = "عينات بيئية";

            viewModel.ConfirmTypeCommand.Execute(null);

            Assert.IsFalse(viewModel.IsSelectingType);
            Assert.IsTrue(viewModel.IsEditing);
            Assert.IsNotNull(viewModel.EditingReception);
            Assert.AreEqual("عينات بيئية", viewModel.EditingReception.CertificateType);
        }
    }
}
