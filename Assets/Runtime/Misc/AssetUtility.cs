using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace XGAsset.Runtime.Misc
{
    public static class AssetUtility
    {
        private static readonly string[] Unit = { "", "KB", "MB", "GB", "TB" };

        public static string GetFileMD5(string path)
        {
            if (!File.Exists(path))
                return string.Empty;
            try
            {
                using var fs = new FileStream(path, FileMode.Open);
                var md5 = new MD5CryptoServiceProvider();
                var retVal = md5.ComputeHash(fs);
                fs.Close();

                var sb = new StringBuilder();
                foreach (var t in retVal)
                {
                    sb.Append(t.ToString("x2"));
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("md5file() fail, error:" + ex.Message);
            }
        }

        public static ulong GetFileSize(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return (ulong)info.Length;
            }
            catch (Exception ex)
            {
                throw new Exception("GetFileSize() fail, error:" + ex.Message);
            }
        }

        public static string GetFileCRC32(string path)
        {
            if (!File.Exists(path))
                return string.Empty;
            using var crc32 = new Crc32();
            using var fs = new FileStream(path, FileMode.Open);
            var hash = crc32.ComputeHash(fs);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static void CreateZipFile(string zipFilePath, string[] filesToAdd)
        {
            using var zipToCreate = new FileStream(zipFilePath, FileMode.Create);
            using var archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create);
            foreach (var fileToAdd in filesToAdd)
            {
                var entry = archive.CreateEntry(Path.GetFileName(fileToAdd));
                using var fileStream = new FileStream(fileToAdd, FileMode.Open);
                using var entryStream = entry.Open();
                fileStream.CopyTo(entryStream);
            }
        }

        public static void ExtractZipFile(string zipFilePath, string extractPath)
        {
            using var zipToOpen = new FileStream(zipFilePath, FileMode.Open);
            using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                var destinationPath = Path.Combine(extractPath, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                using var entryStream = entry.Open();
                using var fileStream = new FileStream(destinationPath, FileMode.Create);
                entryStream.CopyTo(fileStream);
            }
        }

        public static string GetSizeUnit(long totalSize)
        {
            var index = 0;
            var size = Convert.ToDouble(totalSize);
            while (size > 1000)
            {
                size = size / 1024;
                index++;
            }

            return size.ToString("0.00") + Unit[index];
        }
    }
}