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
        public double xStarterOffset;
        public double yStarterOffset;

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
            XmlNode xOffxmlNode = xmldoc.GetElementsByTagName("xoffset")[0];
            xStarterOffset = Double.Parse(xOffxmlNode.ChildNodes[0].InnerText);
            XmlNode yOffxmlNode = xmldoc.GetElementsByTagName("yoffset")[0];
            yStarterOffset = Double.Parse(yOffxmlNode.ChildNodes[0].InnerText);

            for (i = 0; i <= xmlnode.Count - 1; i++)
            {
                ht.Add(xmlnode[i].ChildNodes[0].InnerText, xmlnode[i].ChildNodes[1].InnerText);
            }
        }
    }
}
