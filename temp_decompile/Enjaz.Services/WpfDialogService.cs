using Microsoft.Win32;

namespace Enjaz.Services;

public class WpfDialogService : IDialogService
{
	public string? ShowSaveFileDialog(string filter, string defaultExt, string defaultFileName)
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = filter,
			DefaultExt = defaultExt,
			FileName = defaultFileName
		};
		return (saveFileDialog.ShowDialog() == true) ? saveFileDialog.FileName : null;
	}

	public string? ShowOpenFileDialog(string filter)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = filter
		};
		return (openFileDialog.ShowDialog() == true) ? openFileDialog.FileName : null;
	}

	public string? ShowFolderBrowserDialog()
	{
		OpenFolderDialog openFolderDialog = new OpenFolderDialog();
		return (openFolderDialog.ShowDialog() == true) ? openFolderDialog.FolderName : null;
	}
}
