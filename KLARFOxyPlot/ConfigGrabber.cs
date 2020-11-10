using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace KLARFOxyPlot
{
    public class ConfigGrabber
    {
        public string fileName;
        public Dictionary<String, String> ht;
        public ConfigGrabber(String loadFileName)
        {
            ht = new Dictionary<String, String>();
            fileName = loadFileName;
            XmlDocument xmldoc = new XmlDocument();
            XmlNodeList xmlnode;
            int i = 0;
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            xmldoc.Load(fs);
            xmlnode = xmldoc.GetElementsByTagName("entry");

            for (i = 0; i <= xmlnode.Count - 1; i++)
            {
                ht.Add(xmlnode[i].ChildNodes[0].InnerText, xmlnode[i].ChildNodes[1].InnerText);
            }
        }
    }
}
