namespace Enjaz.Services;

public interface IDialogService
{
	string? ShowSaveFileDialog(string filter, string defaultExt, string defaultFileName);

	string? ShowOpenFileDialog(string filter);

	string? ShowFolderBrowserDialog();
}
