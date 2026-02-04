using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    public class WordReportFilter
    {
        Dictionary<string, List<string>> projectDict = new Dictionary<string, List<string>>();

        public WordReportFilter(string configFilePath)
        {
            StreamReader reader = File.OpenText(configFilePath);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] items = line.Split(':');
                string project = items[0];
                string storyboards = items[1];

                List<string> storyboardList = Array.ConvertAll(storyboards.Split(','), p => p.Trim()).ToList();
                projectDict.Add(project, storyboardList);
            }
        }

        public bool ProjectIsRequired(string projectName)
        {
            bool rc = false;

            if (projectDict.ContainsKey(projectName))
                rc = true;
            return rc;
        }

        public bool StoryboardIsRequired(string projectName, string storyboardName)
        {
            bool rc = false;

            if (projectDict.ContainsKey(projectName))
            {
                List<string> storyboardList = projectDict[projectName];
                if (storyboardList.Contains(storyboardName) || storyboardList.Contains("ALL"))
                    rc = true;
            }

            return rc;
        }

    }
}
