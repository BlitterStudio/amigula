using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;

namespace Amigula
{
    public class UserStartup
    {
        private String m_fileName;
        private String m_slaveDir;
        private String m_slaveName;
        private Dictionary<String, String> m_paramts;

        public bool open(String fileName)
        {
            if (!File.Exists(fileName)) return false;
            try
            {
                var reader = new StreamReader(fileName);
                string contentLine;
                while ((contentLine = reader.ReadLine()) != null)
                {
                    if (contentLine.StartsWith("cd ", StringComparison.CurrentCultureIgnoreCase))
                    {
                        int length = contentLine.Length - 3;
                        if (contentLine.EndsWith(";")) --length;
                        m_slaveDir = contentLine.Substring(3, length).Trim();
                    }
                    else if (contentLine.StartsWith("whdload ", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string[] tokens = contentLine.Split(new Char[] {' '});
                        // example of expected line contents: whdload TimesOfLore.slave PRELOAD;
                        if (tokens.Length < 1) return false;
                        // slavename is the 2nd token
                        m_slaveName = tokens[1];
                        for (int i = 2; i < tokens.Length; i++)
                        {
                            if (tokens[i] == ";") break; // rest is commented out
                            if (tokens[i + 1] == "=")
                            {
                                if (i + 2 < tokens.Length)
                                {
                                    m_paramts[tokens[i].ToUpper()] = tokens[i + 2];
                                    i += 3;
                                }
                                else break; // something is wrong at the end
                            }
                            else
                            {
                                m_paramts[tokens[i].ToUpper()] = String(); // switch
                                ++i;
                            }
                        }
                    }
                }
                reader.Close();        
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                            "Sorry, an exception has occured while trying to read the user-startup file!\n\n" +
                            ex.Message);
            }
            return true;
        }

        public UserStartup(String fileName, bool* ok = NULL);

        public bool save(String fileName = String());

        public String filename() {return m_fileName; }
        public String slaveDir() { return m_slaveDir; }
        public String slaveName() { return m_slaveName; }
        public Dictionary<String, String> paramts() { return m_paramts; }
        public String get(String key) { return m_paramts[key]; }
        public void set(String key, String value) { m_paramts[key] = value; }


    }
}
