using System;
using System.IO;
using System.Text;
using UnityEngine;
using System.Collections;
using System.IO.Compression;
using System.Collections.Generic;

namespace Extensions
{
	public static class SystemExtensions
	{
		public delegate void ProgressDelegate (string message);

		public static FileInfo[] GetAllFiles (string folderPath)
		{
			List<FileInfo> output = new List<FileInfo>();
			List<DirectoryInfo> foldersRemaining = new List<DirectoryInfo>() { new DirectoryInfo(folderPath) };
			while (foldersRemaining.Count > 0)
			{
				DirectoryInfo folder = foldersRemaining[0];
				output.AddRange(folder.GetFiles());
				foldersRemaining.AddRange(folder.GetDirectories());
				foldersRemaining.RemoveAt(0);
			}
			return output.ToArray(); 
		}
		
		public static void CompressFile (string sDir, string sRelativePath, GZipStream zipStream)
		{
			char[] chars = sRelativePath.ToCharArray();
			zipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
			foreach (char c in chars)
				zipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));
			byte[] bytes = File.ReadAllBytes(Path.Combine(sDir, sRelativePath));
			zipStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
			zipStream.Write(bytes, 0, bytes.Length);
		}

		public static bool DecompressFile (string sDir, GZipStream zipStream, ProgressDelegate progress = null)
		{
			byte[] bytes = new byte[sizeof(int)];
			int Readed = zipStream.Read(bytes, 0, sizeof(int));
			if (Readed < sizeof(int))
				return false;
			int iNameLen = BitConverter.ToInt32(bytes, 0);
			bytes = new byte[sizeof(char)];
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < iNameLen; i++)
			{
				zipStream.Read(bytes, 0, sizeof(char));
				char c = BitConverter.ToChar(bytes, 0);
				sb.Append(c);
			}
			string sFileName = sb.ToString();
			if (progress != null)
				progress(sFileName);
			bytes = new byte[sizeof(int)];
			zipStream.Read(bytes, 0, sizeof(int));
			int iFileLen = BitConverter.ToInt32(bytes, 0);
			bytes = new byte[iFileLen];
			zipStream.Read(bytes, 0, bytes.Length);
			string sFilePath = Path.Combine(sDir, sFileName);
			string sFinalDir = Path.GetDirectoryName(sFilePath);
			if (!Directory.Exists(sFinalDir))
				Directory.CreateDirectory(sFinalDir);
			using (FileStream outFile = new FileStream(sFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				outFile.Write(bytes, 0, iFileLen);
			
			return true;
		}
		
		public static void CompressDirectory (string sInDir, string sOutFile, ProgressDelegate progress = null)
		{
			string[] sFiles = Directory.GetFiles(sInDir, "*.*", SearchOption.AllDirectories);
			int iDirLen = sInDir[sInDir.Length - 1] == Path.DirectorySeparatorChar ? sInDir.Length : sInDir.Length + 1;
			using (FileStream outFile = new FileStream(sOutFile, FileMode.Create, FileAccess.Write, FileShare.None))
				using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress))
					foreach (string sFilePath in sFiles)
					{
						string sRelativePath = sFilePath.Substring(iDirLen);
						if (progress != null)
							progress(sRelativePath);
						CompressFile(sInDir, sRelativePath, str);
					}
		}
		
		public static void DecompressToDirectory (string sCompressedFile, string sDir, ProgressDelegate progress = null)
		{
			using (FileStream inFile = new FileStream(sCompressedFile, FileMode.Open, FileAccess.Read, FileShare.None))
				using (GZipStream zipStream = new GZipStream(inFile, CompressionMode.Decompress, true))
					while (DecompressFile(sDir, zipStream, progress));
		}
	}
}