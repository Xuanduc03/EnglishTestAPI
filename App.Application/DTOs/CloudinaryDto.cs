using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{
    public class UploadSignatureDto
    {
        public string CloudName { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public long Timestamp { get; set; }
        public string Signature { get; set; } = null!;
        public string Folder { get; set; } = null!;
    }

}
