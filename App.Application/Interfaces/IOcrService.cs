using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Interfaces
{
    public interface IOcrService
    {
        Task<string> ExtractTextAsync(byte[] imageBytes, CancellationToken ct = default);
        Task<string> ExtractTextMultipleAsync(List<byte[]> images, CancellationToken ct = default);
    }
}
