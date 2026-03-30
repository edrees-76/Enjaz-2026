using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;

namespace Enjaz.ViewModels
{
    /// <summary>
    /// عمليات CRUD والتحقق — Certificate CRUD, Validation & Form Management
    /// </summary>
    public partial class CertificatesViewModel
    {
        #region Methods — CRUD & Form

        public async System.Threading.Tasks.Task LoadCertificatesAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "جاري تحميل الشهادات...";
                
                TotalRecords = await _certificateRepository.GetTotalCertificatesCountAsync();
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                if (TotalPages == 0) TotalPages = 1;
                
                // Ensure Current Page is valid
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (CurrentPage < 1) CurrentPage = 1;

                var result = await _certificateRepository.GetCertificatesPaginatedAsync(CurrentPage, PageSize);
                
                Certificates.Clear();
                for (int i = 0; i < result.Count; i++)
                {
                    result[i].Sequence = ((CurrentPage - 1) * PageSize) + i + 1;
                    Certificates.Add(result[i]);
                }
                
                var counts = await _certificateRepository.GetCertificateCountsByTypeAsync();
                TotalConsumableCertificates = counts.consumable;
                TotalEnvironmentalCertificates = counts.environmental;
                TotalSamplesSum = counts.totalSamples;

                StatusMessage = $"تم تحميل {Certificates.Count} شهادة (صفحة {CurrentPage})";
                
                // Update Commands state
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في تحميل الشهادات: {ex.Message}";
                _notificationService.ShowError("فشل تحميل الشهادات");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async System.Threading.Tasks.Task SearchCertificatesAsync(bool isManualSearch = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText) && SearchCriteria != "التاريخ")
                {
                    await LoadCertificatesAsync();
                    return;
                }

                IsBusy = true;
                StatusMessage = "جاري البحث عن الشهادات...";
                
                var result = await _certificateRepository.SearchCertificatesAsync(SearchText, SearchCriteria, CurrentPage, PageSize, SearchStartDate, SearchEndDate);
                
                Certificates.Clear();
                for (int i = 0; i < result.Count; i++)
                {
                    result[i].Sequence = ((CurrentPage - 1) * PageSize) + i + 1;
                    Certificates.Add(result[i]);
                }

                TotalRecords = await _certificateRepository.GetSearchCertificatesCountAsync(SearchText, SearchCriteria, SearchStartDate, SearchEndDate);
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                if (TotalPages == 0) TotalPages = 1;

                if (TotalRecords == 0 && isManualSearch)
                {
                    SetNotification("لا توجد نتائج", "لا يوجد شهادة بهذه البيانات", NotificationType.Information);
                }

                StatusMessage = $"تم العثور على {TotalRecords} نتيجة";
                CommandManager.InvalidateRequerySuggested();

                var counts = await _certificateRepository.GetCertificateCountsByTypeAsync();
                TotalConsumableCertificates = counts.consumable;
                TotalEnvironmentalCertificates = counts.environmental;
                TotalSamplesSum = counts.totalSamples;

                // تصفير شريط البحث بعد البحث اليدوي (طلب المستخدم)
                if (isManualSearch && !string.IsNullOrWhiteSpace(SearchText))
                {
                    _isInternalClear = true;
                    SearchText = string.Empty;
                    _isInternalClear = false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في البحث: {ex.Message}";
                _notificationService.ShowError("فشل عملية البحث");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async System.Threading.Tasks.Task NextPageAsync()
        {
            if (IsLastPage)
            {
                CurrentPage++;
                await LoadCertificatesAsync();
            }
        }

        private async System.Threading.Tasks.Task PreviousPageAsync()
        {
            if (IsFirstPage)
            {
                CurrentPage--;
                await LoadCertificatesAsync();
            }
        }

        private void StartAddCertificate()
        {
            ClearCertificateForm();
            SelectedCertificate = null;

            // فتح نافذة البحث عن نموذج استلام عبر الخدمة (MVVM compliant)
            var selectedReception = _receptionSearchService.ShowSearchDialog();

            if (selectedReception != null)
            {
                _linkedReceptionId = selectedReception.Id;
                LoadFromReception(selectedReception);
                IsEditing = true;
                RequestNavigation?.Invoke(NavigationDestination.CertificateForm);
            }
        }

        private async System.Threading.Tasks.Task StartEditCertificate()
        {
            if (SelectedCertificate != null)
            {
                await LoadCertificateForEditAsync(SelectedCertificate);
                IsEditing = true;
                RequestNavigation?.Invoke(NavigationDestination.CertificateForm);
            }
        }

        private void ClearCertificateForm()
        {
            RecipientName = string.Empty;
            CertificateType = "شهادة خلو من الاشعاع"; 
            Description = string.Empty;
            IssueDate = DateTime.Now;
            ExpiryDate = null;
            IssuingAuthority = "ادارة الرقابة"; 
            
            AnalysisType = string.Empty;
            Sender = string.Empty;
            Supplier = string.Empty;
            Origin = string.Empty;
            DeclarationNumber = string.Empty;
            PolicyNumber = string.Empty;
            NotificationNumber = string.Empty;
            FinancialReceiptNumber = string.Empty;
            SpecialistName = string.Empty;
            SectionHeadName = string.Empty;
            ManagerName = string.Empty;
            Notes = string.Empty;
            Samples = new ObservableCollection<Sample>();
            SelectedPendingReception = null;
            _linkedReceptionId = null;
        }

        private async System.Threading.Tasks.Task LoadPendingReceptionsAsync()
        {
            try
            {
                var dict = await _sampleReceptionRepository.GetPendingReceptionsAsync();
                PendingReceptions.Clear();
                foreach (var rec in dict)
                {
                    PendingReceptions.Add(rec);
                }
            }
            catch(Exception)
            {
                _notificationService.ShowError("خطأ في جلب بيانات استلام العينات.");
            }
        }

        private void LoadFromReception(SampleReception rec)
        {
            CertificateType = rec.CertificateType;
            Sender = rec.Sender ?? string.Empty;
            Supplier = rec.Supplier ?? string.Empty;
            Origin = rec.Origin ?? string.Empty;
            DeclarationNumber = rec.DeclarationNumber ?? string.Empty;
            NotificationNumber = rec.NotificationNumber ?? string.Empty;
            PolicyNumber = rec.PolicyNumber ?? string.Empty;
            FinancialReceiptNumber = rec.FinancialReceiptNumber ?? string.Empty;
            
            Samples.Clear();
            if (rec.Samples != null)
            {
                foreach(var s in rec.Samples)
                {
                    Samples.Add(new Sample
                    {
                        Root = int.TryParse(s.Root, out int r) ? r : 0,
                        SampleNumber = s.SampleNumber ?? string.Empty,
                        Description = s.Description ?? string.Empty,
                        MeasurementDate = DateTime.Now
                    });
                }
            }
        }

        private async System.Threading.Tasks.Task LoadCertificateForEditAsync(Certificate certificate)
        {
            RecipientName = certificate.RecipientName;
            CertificateType = certificate.CertificateType;
            Description = certificate.Description;
            IssueDate = certificate.IssueDate;
            ExpiryDate = certificate.ExpiryDate;
            IssuingAuthority = certificate.IssuingAuthority;

            AnalysisType = certificate.AnalysisType ?? string.Empty;
            Sender = certificate.Sender ?? string.Empty;
            Supplier = certificate.Supplier ?? string.Empty;
            Origin = certificate.Origin ?? string.Empty;
            DeclarationNumber = certificate.DeclarationNumber ?? string.Empty;
            PolicyNumber = certificate.PolicyNumber ?? string.Empty;
            NotificationNumber = certificate.NotificationNumber ?? string.Empty;
            FinancialReceiptNumber = certificate.FinancialReceiptNumber ?? string.Empty;
            SpecialistName = certificate.SpecialistName ?? string.Empty;
            SectionHeadName = certificate.SectionHeadName ?? string.Empty;
            ManagerName = certificate.ManagerName ?? string.Empty;
            Notes = certificate.Notes ?? string.Empty;
            
            // Load samples from database
            IsBusy = true;
            BusyMessage = "جاري تحميل بيانات العينات...";
            try
            {
                var samples = await _certificateRepository.GetSamplesByCertificateIdAsync(certificate.Id);
                Samples = new ObservableCollection<Sample>(samples);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSaveCertificate()
        {
            // السماح دائماً بالنقر على الحفظ لإظهار رسائل الخطأ التفصيلية للمستخدم
            return true;
        }

        private bool ValidateRequiredFields()
        {
            bool isValid = true;
            var missingFields = new System.Collections.Generic.List<string>();

            // 1. الحقول الأساسية المطلوبة للجميع
            if (string.IsNullOrWhiteSpace(CertificateType)) missingFields.Add("نوع الشهادة");
            
            if (string.IsNullOrWhiteSpace(IssuingAuthority)) 
            {
                IssuingAuthority = "ادارة الرقابة"; // Fallback if missing or empty
            }

            // حقل اسم المستلم مطلوب في قاعدة البيانات ولكنه مخفي في الواجهة بناء على طلب المستخدم
            if (string.IsNullOrWhiteSpace(RecipientName))
            {
                RecipientName = "عام"; // قيمة افتراضية لضمان قبول قاعدة البيانات
            }

            // 2. الحقول المطلوبة لشهادات العينات
            if (IsSampleCertificate)
            {
                if (string.IsNullOrWhiteSpace(Sender)) missingFields.Add("الجهة المرسلة");
                if (string.IsNullOrWhiteSpace(Supplier)) missingFields.Add("المورد");
                if (string.IsNullOrWhiteSpace(Origin)) missingFields.Add("بلد المنشأ");
                if (string.IsNullOrWhiteSpace(DeclarationNumber)) missingFields.Add("رقم الإقرار الجمركي");
                if (string.IsNullOrWhiteSpace(NotificationNumber)) missingFields.Add("رقم الإخطار");
                if (string.IsNullOrWhiteSpace(PolicyNumber)) missingFields.Add("رقم البوليصة");
                if (string.IsNullOrWhiteSpace(FinancialReceiptNumber)) missingFields.Add("رقم الإيصال المالي");

                // 3. التحقق من وجود عينات
                if (Samples == null || Samples.Count == 0)
                {
                    missingFields.Add("إضافة عينة واحدة على الأقل للجدول");
                }
                else
                {
                    if (IsEnvironmentalCertificate)
                    {
                        // 4. التحقق من وجود كافة بيانات العينة والنظائر (K40, Cs-137, Raeq, Th232, Ra226) لكل عينة بيئية
                        bool allSamplesComplete = true;
                        foreach (var sample in Samples)
                        {
                            if (string.IsNullOrWhiteSpace(sample.SampleNumber) || 
                                string.IsNullOrWhiteSpace(sample.Description) || 
                                string.IsNullOrWhiteSpace(sample.IsotopeK40) ||
                                string.IsNullOrWhiteSpace(sample.IsotopeCs137) || 
                                string.IsNullOrWhiteSpace(sample.IsotopeRa) || 
                                string.IsNullOrWhiteSpace(sample.IsotopeTh232) || 
                                string.IsNullOrWhiteSpace(sample.IsotopeRa226))
                            {
                                allSamplesComplete = false;
                                break;
                            }
                        }
                        if (!allSamplesComplete)
                        {
                            missingFields.Add("بيانات العينة (الرقم، الوصف) ونتائج النظائر الخمسة (K40, Cs-137, Raeq, Th232, Ra226) لكافة العينات");
                        }
                    }
                    else
                    {
                        // 4. التحقق من وجود وصف عينة ونتيجة تحليل لكل عينة استهلاكية
                        bool allSamplesComplete = true;
                        foreach (var sample in Samples)
                        {
                            if (string.IsNullOrWhiteSpace(sample.Description) || string.IsNullOrWhiteSpace(sample.Result))
                            {
                                allSamplesComplete = false;
                                break;
                            }
                        }
                        if (!allSamplesComplete)
                        {
                            missingFields.Add("بيانات العينة (الوصف) ونتيجة التحليل لكافة العينات");
                        }
                    }
                }
            }

            if (missingFields.Count > 0)
            {
                SetNotification("بيانات ناقصة", "لا يمكن حفظ الشهادة. يرجى إكمال الحقول والمتطلبات التالية:\n\n• " + string.Join("\n• ", missingFields), NotificationType.Error);
                
                StatusMessage = "⚠️ البيانات غير مكتملة - يرجى مراجعة التنبيه";
                isValid = false;
            }

            return isValid;
        }

        private async System.Threading.Tasks.Task<bool> SaveCertificateAsync(bool closeAfterSave = true)
        {
            try
            {
                // RBAC Check
                if (SelectedCertificate == null && (_userService.CurrentUser == null || !_userService.CurrentUser.CanIssueCertificates))
                {
                    SetNotification("عذراً", "لا تملك صلاحية لإصدار شهادات جديدة.", NotificationType.Warning);
                    return false;
                }
                
                if (SelectedCertificate != null && (_userService.CurrentUser == null || !_userService.CurrentUser.CanEditCertificates))
                {
                     SetNotification("عذراً", "لا تملك صلاحية لتعديل الشهادات المعتمدة.", NotificationType.Warning);
                     return false;
                }

                // 1. التحقق من الحقول الإلزامية
                if (!ValidateRequiredFields()) return false;

                // 2. التحقق من تكرار رقم الإيصال المالي
                if (!string.IsNullOrWhiteSpace(FinancialReceiptNumber))
                {
                    IsBusy = true;
                    StatusMessage = "جاري التحقق من رقم الإيصال المالي...";
                    
                    bool isDuplicate = await _certificateRepository.IsFinancialReceiptDuplicateAsync(
                        FinancialReceiptNumber, 
                        SelectedCertificate?.Id);
                    
                    IsBusy = false;

                    if (isDuplicate)
                    {
                        SetNotification("تكرار رقم الإيصال", $"إن رقم الإيصال المالي ({FinancialReceiptNumber}) مستخدم من قبل في شهادة أخرى.\nيرجى التأكد من الرقم والمحاولة مرة أخرى.", NotificationType.Warning);
                        
                        StatusMessage = "⚠️ رقم إيصال مكرر - يرجى مراجعة التنبيه";
                        return false;
                    }
                }

                IsBusy = true;
                BusyMessage = "جاري التحقق من البيانات...";

                if (SelectedCertificate == null)
                {
                    BusyMessage = "جاري حفظ الشهادة الجديدة...";
                    var newCertificate = new Certificate
                    {
                        CertificateNumber = "AUTO",
                        RecipientName = RecipientName,
                        CertificateType = CertificateType,
                        Description = Description,
                        IssueDate = IssueDate,
                        ExpiryDate = ExpiryDate,
                        IssuingAuthority = IssuingAuthority,
                        
                        AnalysisType = AnalysisType,
                        Sender = Sender,
                        Supplier = Supplier,
                        Origin = Origin,
                        DeclarationNumber = DeclarationNumber,
                        PolicyNumber = PolicyNumber,
                        NotificationNumber = NotificationNumber,
                        FinancialReceiptNumber = FinancialReceiptNumber,
                        SpecialistName = SpecialistName,
                        SectionHeadName = SectionHeadName,
                        ManagerName = ManagerName,
                        Notes = Notes,
                        ReceptionId = _linkedReceptionId,
                        Samples = new ObservableCollection<Sample>(Samples),
                        CreatedBy = _userService.CurrentUser?.Id ?? 1,
                        CreatedByName = _userService.CurrentUser?.FullName ?? "مدير النظام"
                    };

                    if (await _certificateRepository.AddCertificateAsync(newCertificate) > 0)
                    {
                        // تحديث حالة الاستلام المرتبط إلى "تم إصدار شهادة"
                        if (_linkedReceptionId.HasValue)
                        {
                            var linkedReception = await _sampleReceptionRepository.GetReceptionByIdAsync(_linkedReceptionId.Value);
                            if (linkedReception != null)
                            {
                                linkedReception.FinancialReceiptNumber = newCertificate.FinancialReceiptNumber;
                                linkedReception.Status = "تم إصدار شهادة";
                                await _sampleReceptionRepository.UpdateSampleReceptionAsync(linkedReception);
                            }
                            _linkedReceptionId = null;
                        }

                        StatusMessage = "تم إضافة الشهادة بنجاح";
                        CertificateSaved?.Invoke();
                        await LoadCertificatesAsync();
                        _ = LoadSuggestionsAsync();

                        // Auto-generate and open the PDF for the newly created certificate
                        try
                        {
                            StatusMessage = "جاري إنشاء وفتح الشهادة بصيغة PDF...";
                            await System.Threading.Tasks.Task.Run(() => _pdfService.GenerateAndOpenCertificatePdf(newCertificate, newCertificate.Samples));
                        }
                        catch (Exception)
                        {
                            // Silent fail for PDF auto-open or show minor warning, but don't stop the save flow
                        }
                        
                        if (closeAfterSave) 
                        {
                            CancelEdit();
                        }

                        IsBusy = false;
                        SetNotification("تم بنجاح", "تم إضافة وقبول الشهادة الجديدة وحفظها في قاعدة البيانات بنجاح.", NotificationType.Success);
                        
                        return true;
                    }
                    else
                    {
                        IsBusy = false;
                        SetNotification("فشل الحفظ", "حدث خطأ غير متوقع أثناء محاولة حفظ الشهادة. يرجى المحاولة مرة أخرى أو الاتصال بالدعم الفني.", NotificationType.Error);
                        
                        StatusMessage = "فشل في إضافة الشهادة. راجع السجلات.";
                        return false;
                    }
                }
                else
                {
                    BusyMessage = "جاري تحديث بيانات الشهادة...";
                    SelectedCertificate.RecipientName = RecipientName;
                    SelectedCertificate.CertificateType = CertificateType;
                    SelectedCertificate.Description = Description;
                    SelectedCertificate.IssueDate = IssueDate;
                    SelectedCertificate.ExpiryDate = ExpiryDate;
                    SelectedCertificate.IssuingAuthority = IssuingAuthority;
                    
                    SelectedCertificate.AnalysisType = AnalysisType;
                    SelectedCertificate.Sender = Sender;
                    SelectedCertificate.Supplier = Supplier;
                    SelectedCertificate.Origin = Origin;
                    SelectedCertificate.DeclarationNumber = DeclarationNumber;
                    SelectedCertificate.PolicyNumber = PolicyNumber;
                    SelectedCertificate.NotificationNumber = NotificationNumber;
                    SelectedCertificate.FinancialReceiptNumber = FinancialReceiptNumber;
                    SelectedCertificate.SpecialistName = SpecialistName;
                    SelectedCertificate.SectionHeadName = SectionHeadName;
                    SelectedCertificate.ManagerName = ManagerName;
                    SelectedCertificate.Notes = Notes;
                    SelectedCertificate.Samples = new ObservableCollection<Sample>(Samples);

                    // Set Audit Data
                    SelectedCertificate.UpdatedBy = _userService.CurrentUser?.Id;
                    SelectedCertificate.UpdatedByName = _userService.CurrentUser?.FullName;
                    SelectedCertificate.UpdatedAt = DateTime.Now;

                    if (await _certificateRepository.UpdateCertificateAsync(SelectedCertificate))
                    {
                        // مزامنة التعديلات مع استلام العينات المرتبط
                        if (SelectedCertificate.ReceptionId.HasValue)
                        {
                            var linkedReception = await _sampleReceptionRepository.GetReceptionByIdAsync(SelectedCertificate.ReceptionId.Value);
                            if (linkedReception != null)
                            {
                                linkedReception.Sender = SelectedCertificate.Sender;
                                linkedReception.Supplier = SelectedCertificate.Supplier;
                                linkedReception.Origin = SelectedCertificate.Origin;
                                linkedReception.DeclarationNumber = SelectedCertificate.DeclarationNumber;
                                linkedReception.PolicyNumber = SelectedCertificate.PolicyNumber;
                                linkedReception.NotificationNumber = SelectedCertificate.NotificationNumber;
                                linkedReception.FinancialReceiptNumber = SelectedCertificate.FinancialReceiptNumber;
                                linkedReception.Status = "تم إصدار شهادة";
                                await _sampleReceptionRepository.UpdateSampleReceptionAsync(linkedReception);
                            }
                        }
                        // Capture ID locally before list update potentially breaks selection binding
                        int certId = SelectedCertificate.Id;
                        
                        // Notify success (Triggers View Switch to List)
                        StatusMessage = "تم تحديث الشهادة بنجاح";
                        CertificateSaved?.Invoke();
                        
                        // Reload List
                        await LoadCertificatesAsync();
                        _ = LoadSuggestionsAsync();
                        
                        // تحديث سجل النشاط فوراً ليعكس التعديلات الأخيرة باستخدام المعرف المحفوظ
                        var history = await _certificateRepository.GetCertificateHistoryAsync(certId);
                        SelectedCertificateHistory = new ObservableCollection<AuditLog>(history);

                        if (closeAfterSave)
                        {
                            CancelEdit();
                        }

                        IsBusy = false;
                        SetNotification("تم التحديث", "تم تحديث كافة بيانات الشهادة وحفظ التغييرات بنجاح.", NotificationType.Success);

                        return true;
                    }
                    else
                    {
                        IsBusy = false;
                        SetNotification("فشل التحديث", "حدث خطأ أثناء محاولة تحديث بيانات الشهادة. يرجى المحاولة مرة أخرى.", NotificationType.Error);
                        
                        StatusMessage = "فشل في تحديث الشهادة";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                StatusMessage = $"خطأ: {ex.Message}";
                _notificationService.ShowError($"حدث خطأ غير متوقع: {ex.Message}");
                return false;
            }
        }

        private void AddSample()
        {
            int nextRoot = 1;
            if (Samples.Any())
            {
                nextRoot = Samples.Max(s => s.Root) + 1;
            }

            var newSample = new Sample
            {
                Root = nextRoot,
                SampleNumber = string.Empty,
                Description = string.Empty,
                MeasurementDate = DateTime.Now,
                Result = string.Empty
            };
            Samples.Add(newSample);
        }

        private void RemoveSample()
        {
            if (SelectedSample != null)
            {
                Samples.Remove(SelectedSample);
            }
        }

        private void CancelEdit()
        {
            // First navigate away to ensure visual transition
            RequestNavigation?.Invoke(NavigationDestination.Certificates);
            
            IsEditing = false;
            IsViewingDetails = false;
            ClearCertificateForm();
            
            // Do NOT nullify SelectedCertificate here. 
            // This prevents binding errors if the previous view is still unloading,
            // and allows the list to keep the edited item selected if it exists.
        }

        #endregion
    }
}
