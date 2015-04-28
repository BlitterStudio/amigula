using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace Amigula.Helpers
{
    public sealed class FilesHelper
    {
        public static void ReplaceInFile(string filePath, IDictionary<int, string> searchText,
            IDictionary<int, string> replaceText)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                var reader = new StreamReader(filePath);
                string content = reader.ReadToEnd();
                reader.Close();

                if (searchText.Count == replaceText.Count)
                {
                    for (int i = 0; i < searchText.Count; i++)
                    {
                        if (Regex.IsMatch(content, searchText[i]))
                            content = Regex.Replace(content, searchText[i], replaceText[i]);
                        else
                            content += "\r\n" + replaceText[i];
                    }
                }
                var writer = new StreamWriter(filePath);
                writer.Write(content);
                writer.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Sorry, an exception has occured while trying to read/write to a file!\n\n" +
                    ex.Message);
            }          
        }
    }
}