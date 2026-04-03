using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Enjaz.Models;
using Enjaz.Services;

namespace Enjaz.ViewModels
{
    /// <summary>
    /// عمليات PDF والطباعة والإكمال التلقائي — PDF, Print & AutoComplete
    /// </summary>
    public partial class CertificatesViewModel
    {
        #region PDF & Printing

        private async System.Threading.Tasks.Task GeneratePdfAsync()
        {
            if (SelectedCertificate == null) return;

            try
            {
                IsBusy = true;
                BusyMessage = "جاري تحضير ملف PDF...";
                // Always load fresh samples from database
                var samples = await _certificateRepository.GetSamplesByCertificateIdAsync(SelectedCertificate.Id);
                
                bool success = await System.Threading.Tasks.Task.Run(() => _pdfService.GenerateAndOpenCertificatePdf(SelectedCertificate, samples));
                if (success)
                {
                    StatusMessage = "تم إنشاء PDF بنجاح";
                    _notificationService.ShowSuccess("تم إنشاء الشهادة");
                }
                else
                {
                    StatusMessage = "فشل في إنشاء PDF";
                    _notificationService.ShowError("فشل إنشاء الشهادة");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ: {ex.Message}";
                _notificationService.ShowError("حدث خطأ أثناء إنشاء PDF");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async System.Threading.Tasks.Task SavePdfAsync()
        {
            if (SelectedCertificate == null) return;

            try
            {
                // Note: Keep dialog on UI thread
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"Certificate_{SelectedCertificate.CertificateNumber}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    IsBusy = true;
                    BusyMessage = "جاري حفظ ملف PDF...";
                    // Always load fresh samples from database
                    var samples = await _certificateRepository.GetSamplesByCertificateIdAsync(SelectedCertificate.Id);
                    
                    bool success = await System.Threading.Tasks.Task.Run(() => _pdfService.SaveCertificatePdf(SelectedCertificate, samples, saveDialog.FileName));
                    if (success)
                    {
                        StatusMessage = "تم حفظ PDF بنجاح";
                        _notificationService.ShowSuccess($"تم حفظ الشهادة في: {saveDialog.FileName}");
                    }
                    else
                    {
                        StatusMessage = "فشل في حفظ PDF";
                        _notificationService.ShowError("فشل حفظ الشهادة");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ: {ex.Message}";
                _notificationService.ShowError("حدث خطأ أثناء حفظ PDF");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async System.Threading.Tasks.Task PrintCertificateAsync()
        {
            if (SelectedCertificate == null) return;

            try
            {
                // Auto-save if editing
                if (IsEditing)
                {
                    bool saveSuccess = await SaveCertificateAsync(false);
                    if (!saveSuccess)
                    {
                        return; // Save failed, abort print
                    }
                }

                IsBusy = true;
                BusyMessage = "جاري إرسال الشهادة للطباعة...";
                // Always load fresh samples from database
                var samples = await _certificateRepository.GetSamplesByCertificateIdAsync(SelectedCertificate.Id);
                
                bool success = await System.Threading.Tasks.Task.Run(() => _pdfService.PrintCertificatePdf(SelectedCertificate, samples));
                if (success)
                {
                    StatusMessage = "تم إرسال الشهادة للطباعة";
                    _notificationService.ShowSuccess("تم إرسال الشهادة للطباعة");
                }
                else
                {
                    StatusMessage = "فشل في طباعة الشهادة";
                    _notificationService.ShowError("فشل في طباعة الشهادة");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ: {ex.Message}";
                _notificationService.ShowError("حدث خطأ أثناء الطباعة");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region View Details

        private async System.Threading.Tasks.Task ViewCertificateAsync()
        {
            if (SelectedCertificate == null) return;
            
            try
            {
                IsBusy = true;
                BusyMessage = "جاري تحميل تفاصيل الشهادة...";
                // Load samples for the selected certificate from database
                var samples = await _certificateRepository.GetSamplesByCertificateIdAsync(SelectedCertificate.Id);
                Samples = new ObservableCollection<Sample>(samples);

                // Load action history
                var history = await _certificateRepository.GetCertificateHistoryAsync(SelectedCertificate.Id);
                SelectedCertificateHistory = new ObservableCollection<AuditLog>(history);

                IsViewingDetails = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في تحميل تفاصيل الشهادة: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void CloseDetails()
        {
            IsViewingDetails = false;
        }

        #endregion

        #region AutoComplete

        public async System.Threading.Tasks.Task LoadSuggestionsAsync()
        {
            try
            {
                var senders = await _certificateRepository.GetDistinctFieldValuesAsync("Sender");
                foreach (var s in senders)
                {
                    if (!AvailableSenders.Contains(s))
                        AvailableSenders.Add(s);
                }

                var analysisTypes = await _certificateRepository.GetDistinctFieldValuesAsync("AnalysisType");
                if (!analysisTypes.Contains("تحليل مبدئي (دون الوصول لحالة الاتزان)")) 
                    analysisTypes.Insert(0, "تحليل مبدئي (دون الوصول لحالة الاتزان)");
                UpdateCollection(AvailableAnalysisTypes, analysisTypes);

                var results = await _certificateRepository.GetDistinctFieldValuesAsync("Result");
                // التأكد من وجود الجملة الافتراضية في بداية القائمة
                results.Remove("خالية من العناصر المشعة المصنعة"); 
                results.Insert(0, "خالية من العناصر المشعة المصنعة");
                UpdateCollection(AvailableResults, results);

                var recipients = await _certificateRepository.GetDistinctFieldValuesAsync("RecipientName");
                UpdateCollection(AvailableRecipientNames, recipients);

                var suppliers = await _certificateRepository.GetDistinctFieldValuesAsync("Supplier");
                UpdateCollection(AvailableSuppliers, suppliers);

                var origins = await _certificateRepository.GetDistinctFieldValuesAsync("Origin");
                UpdateCollection(AvailableOrigins, origins);

                var specialists = await _certificateRepository.GetDistinctFieldValuesAsync("SpecialistName");
                UpdateCollection(AvailableSpecialistNames, specialists);

                var sectionHeads = await _certificateRepository.GetDistinctFieldValuesAsync("SectionHeadName");
                UpdateCollection(AvailableSectionHeadNames, sectionHeads);

                var managers = await _certificateRepository.GetDistinctFieldValuesAsync("ManagerName");
                UpdateCollection(AvailableManagerNames, managers);
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to load AutoComplete suggestions", ex);
            }
        }

        private void UpdateCollection(ObservableCollection<string> collection, List<string> newValues)
        {
            collection.Clear();
            foreach (var val in newValues)
            {
                if (!string.IsNullOrWhiteSpace(val))
                    collection.Add(val);
            }
        }

        #endregion
    }
}
