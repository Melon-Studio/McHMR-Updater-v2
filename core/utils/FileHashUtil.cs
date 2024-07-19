using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McHMR_Updater_v2.core.utils
{
    public class FileHashUtil
    {
        //MD5方式计算文件哈希值
        public string CalculateHash(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
