namespace Enjaz.Services;

public interface IOSService
{
	void OpenFile(string path);

	void OpenDirectory(string path);

	void PrintFile(string path);

	void PrintFileTo(string path, string printerName);
}
