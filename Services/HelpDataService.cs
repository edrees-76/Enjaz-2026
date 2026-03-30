using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Enjaz.Models;

namespace Enjaz.Services
{
    public class HelpDataService
    {
        private readonly DatabaseService _databaseService;

        public HelpDataService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<HelpContent>> GetAllTopicsAsync()
        {
            return await _databaseService.ExecuteWithRetryAsync(async () =>
            {
                var topics = new List<HelpContent>();
                using var connection = new SqliteConnection(_databaseService.ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT * FROM HelpContent ORDER BY Category, Title;";
                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    topics.Add(MapToHelpContent(reader));
                }
                return topics;
            }, "GetAllHelpTopics");
        }

        public async Task<List<HelpContent>> SearchTopicsAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return await GetAllTopicsAsync();

            return await _databaseService.ExecuteWithRetryAsync(async () =>
            {
                var topics = new List<HelpContent>();
                using var connection = new SqliteConnection(_databaseService.ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT * FROM HelpContent 
                    WHERE Title LIKE @Search OR Keywords LIKE @Search OR Abstract LIKE @Search OR ContentSimple LIKE @Search 
                    ORDER BY Title;";
                
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Search", $"%{searchText}%");
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    topics.Add(MapToHelpContent(reader));
                }
                return topics;
            }, "SearchHelpTopics");
        }

        public async Task<List<HelpContent>> GetTopicsByCategoryAsync(string category)
        {
            return await _databaseService.ExecuteWithRetryAsync(async () =>
            {
                var topics = new List<HelpContent>();
                using var connection = new SqliteConnection(_databaseService.ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT * FROM HelpContent WHERE Category LIKE @Category ORDER BY Title;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Category", category.Trim());

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    topics.Add(MapToHelpContent(reader));
                }
                return topics;
            }, "GetHelpTopicsByCategory");
        }

        public async Task IncrementsViewsAsync(int topicId)
        {
            await _databaseService.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_databaseService.ConnectionString);
                await connection.OpenAsync();
                var query = "UPDATE HelpContent SET Views = Views + 1 WHERE Id = @Id;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Id", topicId);
                await command.ExecuteNonQueryAsync();
            }, "IncrementHelpTopicViews");
        }

        public async Task SeedInitialDataAsync()
        {
            var initialTopics = new List<HelpContent>()
            {
                new HelpContent { 
                    Title = "البداية: دليل التنقل العام", 
                    Abstract = "تعرف على كيفية التنقل في المنظومة وأهم الأقسام بالتفصيل من اللحظة الأولى.",
                    ContentSimple = "أهلاً بك في منظومة إنجاز الرقمية. إليك كيفية البدء:\n\n1. **القائمة الجانبية**: هي محرك التنقل الرئيسي. بمجرد النقر على أي أيقونة، سيتم تحميل الصفحة المطلوبة فوراً.\n2. **لوحة القيادة**: تعطيك نظرة سريعة على حجم العمل (إجمالي الشهادات، العينات المستلمة، والرسوم البيانية).\n3. **الشعار العلوي**: في أي وقت، انقر على شعار (إنجاز) في الأعلى للعودة للشاشة الرئيسية.\n4. **تنبيهات النظام**: ستنبثق لك رسائل تأكيد عند كل عملية حفظ ناجحة.\n\n*نصيحة*: النظام مصمم ليكون سريعاً جداً، إذا كان هناك أي بطء في التنقل، تأكد من تحديث النسخة أو مراجعة قسم الدعم الفني.",
                    Category = "النظام",
                    IconKind = "Rocket",
                    Keywords = "بدء، جولة، مساعدة، نظام، تنقل"
                },
                new HelpContent { 
                    Title = "تحدي الأمان وتسجيل الدخول", 
                    Abstract = "شرح لأول خطوة أمان تواجهها عند فتح المنظومة لحماية البيانات الحساسة.",
                    ContentSimple = "لحماية بياناتك، تمر عملية الدخول بمرحلتين:\n\n1. **تحدي الأمان الأولي**: عند فتح التطبيق، قد يُطلب منك إدخال (كلمة مرور النظام) وهي إجراء إضافي لمنع الدخول غير المصرح به حتى قبل الوصول لشاشة المستخدمين.\n2. **اختيار المستخدم**: بعد تجاوز التحدي، قم باختيار اسمك من القائمة وأدخل كلمة المرور الخاصة بك.\n3. **الأمان ذو الطبقة المزدوجة**: يضمن هذا النظام حماية سجلاتك القانونية والمالية من العبث.\n\n*تنبيه*: في حال نسيان كلمة المرور، يرجى مراجعة المسؤول (Admin) فوراً.",
                    Category = "الأمان",
                    IconKind = "ShieldCheck",
                    Keywords = "أمان، دخول، تشفير، حماية، كلمة مرور"
                },
                new HelpContent { 
                    Title = "استلام العينات (خطوة بخطوة)", 
                    Abstract = "دليل مفصل لكيفية تسجيل شحنة عينات جديدة من البداية.",
                    ContentSimple = "تسجيل العينات هو عصب المنظومة، وتتم العملية كالتالي:\n\n1. **اختيار النوع**: بمجرد النقر على (إضافة عينات جديدة)، ستنبثق نافذة تطلب منك تحديد نوع العينات (استهلاكية / بيئية).\n2. **البيانات اللوجستية**: ستقوم بتعبئة رقم طلب التحليل، الجهة المرسلة، والمورد. (النظام يقترح عليك الأسماء المسجلة سابقاً لتوفير الوقت).\n3. **بناء قائمة العينات**: في الجزء السفلي من الصفحة، أدخل (رقم العينة) و (الوصف) ثم انقر (إضافة). يمكنك تكرار هذه الخطوة لكل عينات الشحنة.\n4. **الحفظ النهائي**: لا تنسى النقر على (حفظ الاستلام) لتظهر البيانات في الجداول الرسمية وفي قسم الشهادات.\n\n*ملاحظة*: يمكنك دائماً تعديل العينات قبل إصدار الشهادة النهائية لها من خلال زر (تعديل) في الجدول الرئيسي.",
                    Category = "الاستلامات",
                    IconKind = "Flask",
                    Keywords = "استلام، شحنة، عينة، تسجيل، خطوة"
                },
                 new HelpContent { 
                    Title = "إصدار الشهادات واعتماد النتائج", 
                    Abstract = "تحويل العينات المستلمة إلى شهادات رسمية معتمدة ونتائج تحاليل دقيقة.",
                    ContentSimple = "بمجرد حفظ استلام العينات، ستجدها تلقائياً في قسم (الشهادات):\n\n1. **البحث والترشيح**: استخدم شريط البحث للعثور على العينة المطلوبة (عبر رقم الإخطار أو الإيصال).\n2. **إدخال النتائج**: انقر على (تعديل) لإدخال نتائج التحاليل (مثل K-40, Ra-226, Raeq للعينات البيئية).\n3. **المعاينة والطباعة**: قبل الاعتماد النهائي، انقر على زر (العين) لمشاهدة تفاصيل الشهادة والتأكد من صحة البيانات.\n4. **توليد الـ PDF**: استخدم زر (PDF) لحفظ نسخة رقمية على جهازك، أو زر (طباعة) لإرسالها للطابعة فوراً.\n\n*معادلات الأمان*: يقوم النظام بحساب بعض القيم تلقائياً لضمان الدقة وتجنب الأخطاء البشرية.",
                    Category = "الشهادات",
                    IconKind = "Certificate",
                    Keywords = "شهادة، نتائج، تحليل، طباعة، صدور"
                },
                new HelpContent { 
                    Title = "إنتاج التقارير والإحصائيات", 
                    Abstract = "كيفية استخراج التقارير التحليلية والمالية بدقة عالية.",
                    ContentSimple = "يوفر قسم التقارير أدوات قوية لصناع القرار:\n\n1. **تحديد الفترة الزمنية**: اختر تاريخ البداية والنهاية للتقرير.\n2. **تصفية البيانات**: يمكنك استخراج تقارير مخصصة لجهة معينة أو لمورد محدد.\n3. **أنواع التقارير**: يتوفر تقرير (إحصائيات عامة) للعينات، وتقارير تفصيلية للنتائج السلبية أو الإيجابية.\n4. **تصدير إكسل**: يمكنك تحويل أي جدول بيانات إلى ملف Excel بضغطة زر لمزيد من المعالجة.\n\n*فائدة*: استخدم الرسوم البيانية في لوحة القيادة للحصول على نظرة سريعة دون الحاجة لاستخراج تقارير ورقية.",
                    Category = "التقارير",
                    IconKind = "ChartLine",
                    Keywords = "تقرير، إحصاء، دقة، مالي، فني، تصدير"
                },
                new HelpContent { 
                    Title = "إدارة النظام والنسخ الاحتياطي", 
                    Abstract = "إرشادات لمسؤولي النظام حول إدارة الحسابات وحماية قاعدة البيانات.",
                    ContentSimple = "إذا كنت تمتلك صلاحيات مسؤول (Admin):\n\n1. **إدارة المستخدمين**: يمكنك إضافة مستخدمين جدد وتحديد صلاحياتهم (أخصائي / مدير / مسؤول).\n2. **النسخ الاحتياطي**: ننصح دائماً بالنقر على (أخذ نسخة احتياطية) أسبوعياً من قسم الإعدادات لضمان عدم فقدان أي بيانات في حال تعطل الجهاز.\n3. **الأرشفة**: يمكنك أرشفة السجلات القديمة لتسريع أداء المنظومة.\n4. **سجل العمليات**: راقب (سجل الأحداث) لمعرفة من قام بأي تعديل وفي أي وقت لضمان الشفافية.\n\n*تحذير*: لا تقم بحذف أي مستخدم لديه سجلات تاريخية، بل استخدم خاصية (إيقاف الحساب) بدلاً من ذلك.",
                    Category = "النظام",
                    IconKind = "Cog",
                    Keywords = "إدارة، مستخدم، نسخة احتياطية، قاعدة بيانات، مسؤول",
                },
                new HelpContent { 
                    Title = "الموسوعة التقنية الشاملة: منظومة إنجاز 2026", 
                    Abstract = "دليل استراتيجي متكامل يشرح الرؤية، المعمارية البرمجية، المكونات، والأمان لمنظومة إنجاز بالتفصيل.",
                    ContentSimple = "تعد منظومة إنجاز 2026 حجر الزاوية في التحول الرقمي للمختبرات والمؤسسات الرقابية. تعتمد المنظومة على تقنيات .NET 8 و C# مع قاعدة بيانات SQLite WAL الفائقة.\n\n### أهم ملامح المنظومة:\n1. **الأمان السيادي**: حماية البيانات باستخدام تشفير PBKDF2 المتقدم.\n2. **محرك البحث الذكي**: استخدام تقنية FTS5 للبحث اللحظي.\n3. **الأداء المستقر**: معالجة غير متزامنة تمنع تجمد الواجهات.\n4. **التقارير الاحترافية**: تصدير ذكي بصيغ PDF و Excel.\n\n*ملاحظة*: تم تصميم هذه المنظومة لتكون جسراً نحو المستقبل الرقمي الآمن والخالي من الأوراق.",
                    Category = "النظام",
                    IconKind = "BookOpenPageVariant",
                    Keywords = "موسوعة، دليل، نظام، تقنية، إنجاز، رؤية",
                },
                new HelpContent { 
                    Title = "الربط الشبكي: دليل إنشاء قاعدة بيانات مشتركة", 
                    Abstract = "دليل مبسط جداً موجه للمستخدمين الجدد، يشرح خطوة بخطوة كيفية ربط جميع حواسيب المختبر بملف بيانات واحد عبر الشبكة المحلية (LAN).",
                    ContentSimple = "لجعل المنظومة تعمل على أكثر من جهاز في نفس الوقت (مثل جهاز الاستقبال، وجهاز إدارة القياسات)، يرجى اتباع هذه الخطوات المبسطة بدقة:\n\n### أولاً: تجهيز الجهاز الرئيسي (الخادم - Server)\nوهو الجهاز الأقوى والذي سيحتفظ بالبيانات بصورة دائمة:\n1. افتح (جهاز الكمبيوتر) واذهب للقرص (D) أو (C) وأنشئ مجلداً جديداً للشبكة وسمّه بالإنجليزية (Enjaz_Network_DB).\n2. انقر بزر الفأرة الأيمن على هذا المجلد الذي أنشأته، واختر (خصائص - Properties).\n3. من الأعلى، اختر تبويب (مشاركة - Sharing)، ثم اضغط على زر (مشاركة متقدمة - Advanced Sharing).\n4. ضع علامة صح ✅ بجوار (مشاركة هذا المجلد - Share this folder).\n5. تأكد من الضغط على زر (أذونات - Permissions)، ثم ضع علامة صح ✅ في مربع (تحكم كامل - Full Control) للجميع.\n6. اضغط (موافق) لحفظ المشاركة.\n\n### ثانياً: ربط أجهزة الموظفين بالشبكة المجهزة\nالآن اذهب إلى جهاز (كمبيوتر الاستقبال) أو (كمبيوتر القياسات) وقم بما يلي:\n1. افتح منظومة إنجاز من سطح المكتب.\n2. من القائمة الجانبية في المنظومة، اذهب إلى شاشة **(الإعدادات)**.\n3. اختر تبويب **(إدارة البيانات والنسخ)**.\n4. ابحث عن زر يسمى **[ تحديد مسار قاعدة البيانات للربط عبر الشبكة ]** واضغط عليه.\n5. ستفتح لك نافذة وندوز العادية لاختيار المجلدات.. ابحث في الجانب الأيسر عن كلمة **(الشبكة أو Network)** واضغط عليها.\n6. سيظهر لك اسم (الجهاز الرئيسي).. ادخل عليه، وستجد بداخله المجلد الذي سميناه (Enjaz_Network_DB).\n7. افتح هذا المجلد، ثم اضغط على زر (تحديد المجلد - Select Folder) من الأسفل.\n8. أخيراً، اضغط على زر **(حفظ الإعدادات)** في شاشة المنظومة لتحويل المسار.\n\n🎉 تهانينا! المنظومة الآن ستقوم فوراً بمعالجة العمليات وحفظها في الخادم الرئيسي المركزي. بمجرد قيام الموظفين الآخرين بنفس الخطوات في أجهزتهم، سيرى الجميع نفس البيانات المشتركة في ذات اللحظة.",
                    Category = "النظام",
                    IconKind = "ServerNetwork",
                    Keywords = "شبكة، ربط، سيرفر، خادم، مشاركة، المبتدئين"
                },
                new HelpContent { 
                    Title = "الربط السحابي الذكي للنسخ الاحتياطي (Google Drive / OneDrive)", 
                    Abstract = "دليل مبسط لكيفية إعداد النسخ الاحتياطي التلقائي المتزامن مع السحابة لمنع فقدان البيانات.",
                    ContentSimple = "إعداد المزامنة السحابية هي ميزة أمان إضافية وقوية جداً لحماية المنظومة من الكوارث (مثل تلف القرص الصلب، سرقة الجهاز، أو الفيروسات).\n\n### طريقة التفعيل بـ 3 خطوات فقط:\n1. **تنزيل برنامج السحابة**: قم بتنزيل برنامج (Google Drive for Desktop) أو (Microsoft OneDrive) من الإنترنت وقم بتسجيل الدخول بحساب المؤسسة الرسمي.\n2. **تحديد مسار السحابة في المنظومة**: اذهب إلى شاشة **(الإعدادات)** ثم تبويب **(إدارة البيانات والنسخ)**.\n3. ابحث عن صندوق **(مسار النسخ الاحتياطي السحابي ☁️)** واضغط على زر السحابة.\n4. ستفتح لك نافذة، اختر منها مسار مجلد السحابة الذي تم تثبيته للتو (مثلاً Google Drive -> My Drive -> Enjaz_Backups).\n5. اضغط على زر **(حفظ الإعدادات)**.\n\n🎉 تم التفعيل بنجاح! من الآن فصاعداً عندما يصدر أمر (نسخة احتياطية)، ستقوم المنظومة بإرسال نسخة سرية إضافية إلى السحابة فوراً ومباشرة.",
                    Category = "النظام",
                    IconKind = "CloudSync",
                    Keywords = "سحابة، درايف، نسخة، احتياطية، حماية، مزامنة, Google, Drive, OneDrive"
                }
            };

            foreach (var topic in initialTopics)
            {
                var existing = await _databaseService.ExecuteWithRetryAsync(async () =>
                {
                    using var connection = new SqliteConnection(_databaseService.ConnectionString);
                    await connection.OpenAsync();
                    using var command = new SqliteCommand("SELECT * FROM HelpContent WHERE Title = @Title", connection);
                    command.Parameters.AddWithValue("@Title", topic.Title);
                    using var reader = await command.ExecuteReaderAsync();
                    return await reader.ReadAsync() ? MapToHelpContent(reader) : null;
                });

                if (existing == null)
                {
                    await _databaseService.ExecuteWithRetryAsync(async () =>
                    {
                        using var connection = new SqliteConnection(_databaseService.ConnectionString);
                        await connection.OpenAsync();
                        var query = @"
                            INSERT INTO HelpContent (Title, Abstract, ContentSimple, ContentAdvanced, Category, IconKind, Keywords, RelatedView)
                            VALUES (@Title, @Abstract, @ContentSimple, @ContentAdvanced, @Category, @IconKind, @Keywords, @RelatedView);";
                        using var command = new SqliteCommand(query, connection);
                        command.Parameters.AddWithValue("@Title", topic.Title);
                        command.Parameters.AddWithValue("@Abstract", topic.Abstract);
                        command.Parameters.AddWithValue("@ContentSimple", topic.ContentSimple);
                        command.Parameters.AddWithValue("@ContentAdvanced", string.Empty); // Not used anymore
                        command.Parameters.AddWithValue("@Category", topic.Category);
                        command.Parameters.AddWithValue("@IconKind", topic.IconKind);
                        command.Parameters.AddWithValue("@Keywords", topic.Keywords);
                        command.Parameters.AddWithValue("@RelatedView", topic.RelatedView ?? string.Empty);
                        await command.ExecuteNonQueryAsync();
                    });
                }
                else
                {
                    // Update enriched content
                    await _databaseService.ExecuteWithRetryAsync(async () =>
                    {
                        using var connection = new SqliteConnection(_databaseService.ConnectionString);
                        await connection.OpenAsync();
                        var query = @"
                            UPDATE HelpContent 
                            SET Abstract=@Abstract, ContentSimple=@ContentSimple, Category=@Category, IconKind=@IconKind, Keywords=@Keywords 
                            WHERE Id=@Id;";
                        using var command = new SqliteCommand(query, connection);
                        command.Parameters.AddWithValue("@Id", existing.Id);
                        command.Parameters.AddWithValue("@Abstract", topic.Abstract);
                        command.Parameters.AddWithValue("@ContentSimple", topic.ContentSimple);
                        command.Parameters.AddWithValue("@Category", topic.Category);
                        command.Parameters.AddWithValue("@IconKind", topic.IconKind);
                        command.Parameters.AddWithValue("@Keywords", topic.Keywords);
                        await command.ExecuteNonQueryAsync();
                    });
                }
            }
        }

        private HelpContent MapToHelpContent(SqliteDataReader reader)
        {
            return new HelpContent
            {
                Id = Convert.ToInt32(reader["Id"]),
                Title = reader["Title"].ToString() ?? "",
                Abstract = reader["Abstract"].ToString() ?? "",
                ContentSimple = reader["ContentSimple"].ToString() ?? "",
                ContentAdvanced = reader["ContentAdvanced"].ToString() ?? "",
                Category = reader["Category"].ToString() ?? "",
                IconKind = reader["IconKind"].ToString() ?? "HelpCircleOutline",
                Keywords = reader["Keywords"].ToString() ?? "",
                RelatedView = reader["RelatedView"].ToString() ?? "",
                VideoUrl = reader["VideoUrl"].ToString() ?? ""
            };
        }
    }
}
