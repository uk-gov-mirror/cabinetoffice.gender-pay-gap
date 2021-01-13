﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GenderPayGap.WebUI.Tests.TestHelpers
{
    public static class CodeQualityTestHelpers
    {
        public static string GetRootCodeFolder()
        {
            string compiledDllFilename = Assembly.GetExecutingAssembly().Location;
            Console.WriteLine($"compiledDllFilename is [{compiledDllFilename}]");
            string compiledDllDirectory = new FileInfo(compiledDllFilename).Directory.FullName;

            string rootCodeFolder = new DirectoryInfo($"{compiledDllDirectory}\\..\\..\\..\\..\\..\\").FullName;
            return rootCodeFolder;
        }

        public static bool FileIsExcluded(string filePathSuffix, List<string> excludedFilesAndFolders)
        {
            foreach (string excludedFilePath in excludedFilesAndFolders)
            {
                if (filePathSuffix.StartsWith(excludedFilePath))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
