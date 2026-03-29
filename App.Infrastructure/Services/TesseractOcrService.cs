using App.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace App.Infrastructure.Services
{
    public class TesseractOcrService : IOcrService
    {
        private readonly string _tesseractExe;
        private readonly string _tessDataPath;
        private readonly ILogger<TesseractOcrService> _logger;

        public TesseractOcrService(IConfiguration config, ILogger<TesseractOcrService> logger)
        {
            _tesseractExe = config["Tesseract:ExePath"]
                ?? @"C:\Program Files\Tesseract-OCR\tesseract.exe";
            _tessDataPath = config["Tesseract:TessDataPath"]
                ?? @"D:\PersonalProject\Web_Thi\tessdata";
            _logger = logger;
            _logger.LogInformation("Tesseract exe: {exe} | tessdata: {data}", _tesseractExe, _tessDataPath);
        }

        public async Task<string> ExtractTextAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            var tmpImg = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid()}.png");
            var tmpOut = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid()}");

            try
            {
                await File.WriteAllBytesAsync(tmpImg, imageBytes, ct);

                var args = $"\"{tmpImg}\" \"{tmpOut}\" -l eng " +
                           $"--tessdata-dir \"{_tessDataPath}\" --psm 3";

                var psi = new ProcessStartInfo
                {
                    FileName = _tesseractExe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                };

                using var process = Process.Start(psi)
                    ?? throw new InvalidOperationException("Không thể khởi động Tesseract process");

                var stderr = await process.StandardError.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);

                if (process.ExitCode != 0)
                    throw new InvalidOperationException(
                        $"Tesseract exit {process.ExitCode}: {stderr}. " +
                        $"Exe path: {_tesseractExe}");

                var txtFile = tmpOut + ".txt";
                if (!File.Exists(txtFile))
                    throw new InvalidOperationException(
                        $"Tesseract không sinh output. Kiểm tra exe: {_tesseractExe}");

                var text = await File.ReadAllTextAsync(txtFile, Encoding.UTF8, ct);
                return CleanOcrText(text);
            }
            finally
            {
                if (File.Exists(tmpImg)) File.Delete(tmpImg);
                if (File.Exists(tmpOut + ".txt")) File.Delete(tmpOut + ".txt");
            }
        }

        public async Task<string> ExtractTextMultipleAsync(List<byte[]> images, CancellationToken ct = default)
        {
            var results = new List<string>();
            foreach (var img in images)
            {
                var text = await ExtractTextAsync(img, ct);
                if (!string.IsNullOrWhiteSpace(text))
                    results.Add(text);
            }
            return string.Join("\n\n", results);
        }

        private static string CleanOcrText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var lines = raw.Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 2 && l.Any(char.IsLetter))
                .ToList();

            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.EndsWith('-') && i < lines.Count - 1)
                    sb.Append(line[..^1]);
                else
                {
                    sb.Append(line);
                    if (i < lines.Count - 1) sb.Append(' ');
                }
            }
            return sb.ToString().Trim();
        }
    }
}