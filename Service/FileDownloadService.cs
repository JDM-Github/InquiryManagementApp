public class FileDownloadService
{
    public async Task<byte[]> DownloadFileFromUrlAsync(string fileUrl)
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetByteArrayAsync(fileUrl);
    }
}
