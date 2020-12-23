using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QrReader
{
    class Program
    {
        const string QrApiBaseUrl = "http://api.qrserver.com/";
        const int MaxFileSizeInBytes = 1048576;
        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to QrReader!");
            Console.Write("Give me your QR Code image's path: ");
            var imagePath = Console.ReadLine();

            try
            {
                var imageExtension = Path.GetExtension(imagePath);
                var result = await GetContentFromQrCodeImagePath(imagePath, imageExtension);
                Console.WriteLine("Result from QR API:");
                foreach (var symbol in result.SelectMany(r => r.symbol))
                {
                    if (symbol.error != null)
                    {
                        Console.WriteLine($"Error: {symbol.error}");
                    }
                    else
                    {
                        Console.WriteLine($"Data: {symbol.data}");
                    }

                }
            }
            catch (IOException ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Console.WriteLine($"Come on! give me a valid path next time, I could not found the file.");
                Console.WriteLine($"Error details: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Hey! this image is so heavy");
                Console.WriteLine($"Error details: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oops, an unhandled error has ocurred, contact Angel for an explanation of this error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Press Any Key To End The QrReader...");
                Console.ReadKey();
            }
        }

        static async Task<List<QrResponse>> GetContentFromQrCodeImagePath(string imagePath, string imageExtension)
        {

            using (var httpClient = new HttpClient() { BaseAddress = new Uri(QrApiBaseUrl) })
            {
                using (var multipartFormContent = new MultipartFormDataContent())
                {
                    using (var stream = new FileStream(imagePath, FileMode.Open))
                    {
                        if (stream.Length > MaxFileSizeInBytes)
                        {
                            throw new ArgumentException("File too large! Please provide an image with < 1MB");
                        }
                        var streamContent = new StreamContent(stream);
                        multipartFormContent.Add(streamContent, "file", $"file{imageExtension}");

                        var httpResponse = await httpClient.PostAsync("v1/read-qr-code/", multipartFormContent);
                        httpResponse.EnsureSuccessStatusCode();
                        var stringContent = await httpResponse.Content.ReadAsStringAsync();
                        
                        return JsonSerializer.Deserialize<List<QrResponse>>(stringContent);

                    }
                }
            }
        }
    }

    public class QrResponse
    {
        public string type { get; set; }
        public List<Symbol> symbol { get; set; }
    }

    public class Symbol
    {
        public long seq { get; set; }
        public string data { get; set; }
        public object error { get; set; }
    }

}
