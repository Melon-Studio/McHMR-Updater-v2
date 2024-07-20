using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McHMR_Updater_v2.core.entity;

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
                if (Directory.Exists(entry))
                {
                    AddDirectoryFilesToSet(entry, tempWhiteListSet, fileDir);
                }
                else
                {
                    tempWhiteListSet.Add(entry);
                }
            }

            foreach (string entry in tempWhiteListSet)
            {
                string relativePath = fileDir + "\\" + entry;
                whiteListSet.Add(relativePath.Replace("/", "\\"));
            }

            foreach (HashEntity entry in hashPath)
            {
                string relativePath = fileDir + "\\" + entry.filePath;
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

        private void AddDirectoryFilesToSet(string directory, HashSet<string> fileSet, string fileDir)
        {
            string[] files = Directory.GetFiles(fileDir + "\\" + directory, "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                fileSet.Add(file);
            }
        }
    }
}
