using McHMR_Updater_v2.core.entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace McHMR_Updater_v2.core.utils
{
    public class NoFileUtil
    {
        public async Task<List<string>> CheckFiles(List<HashEntity> hashPath, string whiteList, string fileDir)
        {
            List<string> notInListFiles = new List<string>();
            HashSet<string> tempWhiteListSet = new HashSet<string>();
            string[] whitelistArrayBefore = whiteList.Split(Environment.NewLine.ToCharArray());
            string[] whitelistArrayAfter = whitelistArrayBefore.Where(s => !string.IsNullOrEmpty(s)).ToArray();
            HashSet<string> hashPathSet = new HashSet<string>();
            HashSet<string> whiteListSet = new HashSet<string>();

            foreach (string entry in whitelistArrayAfter)
            {
                string tempPhat = entry;
                if (tempPhat[0] == '/' || tempPhat[0] == '\\')
                {
                    tempPhat = entry.Substring(1);
                }
                tempPhat = tempPhat.Replace('/', '\\');
                string filePath = fileDir + "\\" + tempPhat;
                if (Directory.Exists(filePath))
                {
                    AddDirectoryFilesToSet(filePath, tempWhiteListSet);
                }
                else
                {
                    tempWhiteListSet.Add(filePath);
                }
            }

            foreach (string entry in tempWhiteListSet)
            {
                whiteListSet.Add(entry.Replace("/", "\\"));
            }

            foreach (HashEntity entry in hashPath)
            {
                string relativePath = fileDir + entry.filePath;
                hashPathSet.Add(relativePath.Replace("/", "\\"));
            }

            string[] filesInParam3 = Directory.GetFiles(fileDir, "*", SearchOption.AllDirectories);

            foreach (string file in filesInParam3)
            {
                if (!hashPathSet.Contains(file) && !whiteListSet.Contains(file))
                {
                    notInListFiles.Add(file);
                }
            }

            return notInListFiles;
        }
        private void AddDirectoryFilesToSet(string directory, HashSet<string> fileSet)
        {
            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                fileSet.Add(file);
            }
        }

        public async Task RemoveEmptyDirectories(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                // 递归调用以检查子目录
                await RemoveEmptyDirectories(directory);

                // 如果目录是空的（没有文件和子目录），则删除它
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory);
                    Console.WriteLine($"Deleted empty directory: {directory}");
                }
            }
        }
    }
}
