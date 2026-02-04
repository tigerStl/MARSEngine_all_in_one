using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.MarsConfig
{
    public class DataCompareConfig
    {
        public string TemplatePath { get; internal set; }
        public string OutputPath { get; internal set; }

        public DataCompareConfig(string templatePath, string outputPath)
        {
           TemplatePath = templatePath;
           OutputPath = outputPath;
        }
    }
}
