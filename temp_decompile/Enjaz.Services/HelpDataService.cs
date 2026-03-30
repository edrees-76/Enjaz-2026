using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Models;
using Microsoft.Data.Sqlite;

namespace Enjaz.Services;

public class HelpDataService
{
	private readonly DatabaseService _databaseService;

	public HelpDataService(DatabaseService databaseService)
	{
		_databaseService = databaseService;
	}

	public async Task<List<HelpContent>> GetAllTopicsAsync()
	{
		return await _databaseService.ExecuteWithRetryAsync(async delegate
		{
			List<HelpContent> topics = new List<HelpContent>();
			using SqliteConnection connection = new SqliteConnection(_databaseService.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT * FROM HelpContent ORDER BY Category, Title;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				topics.Add(MapToHelpContent(reader));
			}
			return topics;
		}, "GetAllHelpTopics");
	}

	public async Task<List<HelpContent>> SearchTopicsAsync(string searchText)
	{
		if (string.IsNullOrWhiteSpace(searchText))
		{
			return await GetAllTopicsAsync();
		}
		return await _databaseService.ExecuteWithRetryAsync(async delegate
		{
			List<HelpContent> topics = new List<HelpContent>();
			using SqliteConnection connection = new SqliteConnection(_databaseService.ConnectionString);
			await connection.OpenAsync();
			string query = "\n                    SELECT * FROM HelpContent \n                    WHERE Title LIKE @Search OR Keywords LIKE @Search OR Abstract LIKE @Search OR ContentSimple LIKE @Search \n                    ORDER BY Title;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Search", "%" + searchText + "%");
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				topics.Add(MapToHelpContent(reader));
			}
			return topics;
		}, "SearchHelpTopics");
	}

	public async Task<List<HelpContent>> GetTopicsByCategoryAsync(string category)
	{
		return await _databaseService.ExecuteWithRetryAsync(async delegate
		{
			List<HelpContent> topics = new List<HelpContent>();
			using SqliteConnection connection = new SqliteConnection(_databaseService.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT * FROM HelpContent WHERE Category LIKE @Category ORDER BY Title;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Category", category.Trim());
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				topics.Add(MapToHelpContent(reader));
			}
			return topics;
		}, "GetHelpTopicsByCategory");
	}

	public async Task IncrementsViewsAsync(int topicId)
	{
		await _databaseService.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_databaseService.ConnectionString);
			await connection.OpenAsync();
			string query = "UPDATE HelpContent SET Views = Views + 1 WHERE Id = @Id;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Id", topicId);
			await command.ExecuteNonQueryAsync();
		}, "IncrementHelpTopicViews");
	}

	public async Task SeedInitialDataAsync()
	{
		List<HelpContent> initialTopics = new List<HelpContent>
		{
			new HelpContent
			{
				Title = "دليل البدء السريع",
				Abstract = "تعرف على كيفية التنقل في المنظومة وأهم الأقسام بالتفصيل.",
				ContentSimple = "أهلا\u064b بك في منظومة إنجاز الرقمية. للبدء:\n1. استخدم القائمة الجانبية للتنقل بين الأقسام.\n2. قسم (استلام العينات) مخصص لإدخال العينات الجديدة.\n3. قسم (الشهادات) يتيح لك مراجعة وإصدار الشهادات.\n4. يمكنك دائما\u064b العودة للشاشة الرئيسية عبر النقر على شعار المنظومة في الأعلى.\n\nتذكر: النظام يحفظ بياناتك تلقائيا\u064b في أغلب الشاشات، ولكن تأكد دائما\u064b من النقر على زر (حفظ) عند الانتهاء.",
				Category = "النظام",
				IconKind = "RocketLaunch",
				Keywords = "بدء، جولة، مساعدة، نظام"
			},
			new HelpContent
			{
				Title = "كيفية استلام عينة جديدة",
				Abstract = "خطوات تسجيل استلام عينة استهلاكية أو بيئية للمبتدئين.",
				ContentSimple = "لتسجيل عينة جديدة:\n- اذهب إلى قسم (استلام العينات) من القائمة.\n- اختر نوع العينة (استهلاكية / بيئية).\n- املأ بيانات جهة التكليف والبيانات الفنية للعينة.\n- بعد الحفظ، ستظهر العينة في قائمة الانتظار للمراجعة وإصدار الشهادة.\n\nملاحظة: تأكد من إدخال رقم التشغيلة وتاريخ الاستلام بدقة لضمان تتبع العينة بشكل صحيح.",
				Category = "الاستلامات",
				IconKind = "FlaskPlus",
				Keywords = "استلام، شحنة، عينة، تسجيل"
			},
			new HelpContent
			{
				Title = "إصدار وإدارة الشهادات",
				Abstract = "شرح مفصل لعملية مراجعة العينات وإصدار الشهادات النهائية.",
				ContentSimple = "بعد حفظ الاستلام، ينتقل الطلب إلى قسم (الشهادات):\n- ابحث عن العينة باستخدام الرقم أو الاسم.\n- قم بإدخال نتائج التحاليل المطلوبة في الحقول المخصصة.\n- سيقوم النظام تلقائيا\u064b بتحديث الحالة وتجهيز مسودة الشهادة للطباعة.\n- يمكنك النقر على (عرض الشهادة) لمعاينتها قبل الطباعة النهائية.",
				Category = "الشهادات",
				IconKind = "Certificate",
				Keywords = "شهادة، نتائج، تحليل، طباعة"
			},
			new HelpContent
			{
				Title = "نظام التقارير والإحصائيات",
				Abstract = "تعرف على كيفية استخراج التقارير اليومية والشهرية بضغطة زر.",
				ContentSimple = "توفر المنظومة نظاما\u064b مرنا\u064b للتقارير:\n- اذهب إلى (التقارير) من القائمة الجانبية.\n- حدد نوع التقرير المطلوب (مالي / فني).\n- حدد الفترة الزمنية (من تاريخ / إلى تاريخ).\n- يمكنك تصدير التقارير بصيغة PDF أو طباعتها مباشرة.\n- التقارير تساعد الإدارة في متابعة حجم العمل والإنتاجية.",
				Category = "التقارير",
				IconKind = "FileChart",
				Keywords = "تقرير، إحصاء، دقة، مالي، فني"
			},
			new HelpContent
			{
				Title = "إجراءات الأمان والخصوصية",
				Abstract = "كيفية حماية بياناتك وإدارة الجلسات في المنظومة بكل أمان.",
				ContentSimple = "الأمان هو أولويتنا في إنجاز:\n- يتم تسجيل خروجك تلقائيا\u064b بعد فترة من الخمول (timeout) لحماية حسابك.\n- لا تشارك كلمة مرورك مع أي شخص.\n- يمكنك مراجعة سجل العمليات الخاص بحسابك من إعدادات الملف الشخصي.\n- تأكد دائما\u064b من تسجيل الخروج عند الانتهاء من العمل على جهاز مشترك.",
				Category = "الأمان",
				IconKind = "ShieldLock",
				Keywords = "أمان، خصوصية، تسجيل، خروج"
			}
		};
		foreach (HelpContent topic in initialTopics)
		{
			HelpContent existing = await _databaseService.ExecuteWithRetryAsync(async delegate
			{
				using SqliteConnection connection = new SqliteConnection(_databaseService.ConnectionString);
				await connection.OpenAsync();
				using SqliteCommand command = new SqliteCommand("SELECT * FROM HelpContent WHERE Title = @Title", connection);
				command.Parameters.AddWithValue("@Title", topic.Title);
				using SqliteDataReader reader = await command.ExecuteReaderAsync();
				return (await reader.ReadAsync()) ? MapToHelpContent(reader) : null;
			});
			if (existing == null)
			{
				await _databaseService.ExecuteWithRetryAsync(async delegate
				{
					using SqliteConnection connection = new SqliteConnection(_databaseService.ConnectionString);
					await connection.OpenAsync();
					string query = "\n                            INSERT INTO HelpContent (Title, Abstract, ContentSimple, ContentAdvanced, Category, IconKind, Keywords, RelatedView)\n                            VALUES (@Title, @Abstract, @ContentSimple, @ContentAdvanced, @Category, @IconKind, @Keywords, @RelatedView);";
					using SqliteCommand command = new SqliteCommand(query, connection);
					command.Parameters.AddWithValue("@Title", topic.Title);
					command.Parameters.AddWithValue("@Abstract", topic.Abstract);
					command.Parameters.AddWithValue("@ContentSimple", topic.ContentSimple);
					command.Parameters.AddWithValue("@ContentAdvanced", string.Empty);
					command.Parameters.AddWithValue("@Category", topic.Category);
					command.Parameters.AddWithValue("@IconKind", topic.IconKind);
					command.Parameters.AddWithValue("@Keywords", topic.Keywords);
					command.Parameters.AddWithValue("@RelatedView", topic.RelatedView ?? string.Empty);
					await command.ExecuteNonQueryAsync();
				});
				continue;
			}
			await _databaseService.ExecuteWithRetryAsync(async delegate
			{
				using SqliteConnection connection = new SqliteConnection(_databaseService.ConnectionString);
				await connection.OpenAsync();
				string query = "\n                            UPDATE HelpContent \n                            SET Abstract=@Abstract, ContentSimple=@ContentSimple, Category=@Category, IconKind=@IconKind, Keywords=@Keywords \n                            WHERE Id=@Id;";
				using SqliteCommand command = new SqliteCommand(query, connection);
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

	private HelpContent MapToHelpContent(SqliteDataReader reader)
	{
		return new HelpContent
		{
			Id = Convert.ToInt32(reader["Id"]),
			Title = (reader["Title"].ToString() ?? ""),
			Abstract = (reader["Abstract"].ToString() ?? ""),
			ContentSimple = (reader["ContentSimple"].ToString() ?? ""),
			ContentAdvanced = (reader["ContentAdvanced"].ToString() ?? ""),
			Category = (reader["Category"].ToString() ?? ""),
			IconKind = (reader["IconKind"].ToString() ?? "HelpCircleOutline"),
			Keywords = (reader["Keywords"].ToString() ?? ""),
			RelatedView = (reader["RelatedView"].ToString() ?? ""),
			VideoUrl = (reader["VideoUrl"].ToString() ?? "")
		};
	}
}
