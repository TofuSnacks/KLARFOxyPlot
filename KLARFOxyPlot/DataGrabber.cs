using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace KLARFOxyPlot
{
    public class DataGrabber
    {
        public CenterSpace.NMath.Stats.DataFrame df;
        public double fileVersion;
        public double xDiePit;
        public double yDiePit;
        public double xDieOri;
        public double yDieOri;
        public String fileName;
        public double xCenter;
        public double yCenter;
        public double totalClassLookup;
        public double waferSize;
        public double notch;
        public DataGrabber(string loadFileName)
        {
            fileName = loadFileName;
            df = new CenterSpace.NMath.Stats.DataFrame();

            StreamReader reader = File.OpenText(fileName);
            string text = reader.ReadToEnd();
            string[] lines = text.Split('\n');

            bool hasCols = false;
            int numberOfVariables = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string[] splitString = lines[i].Replace(";", "").Split(" ");
                if (lines[i].Contains("FileVersion"))
                {
                    fileVersion = Double.Parse(splitString[1] + "." + splitString[2]);
                }
                else if (lines[i].Contains("DiePitch "))
                {
                    xDiePit = Double.Parse(splitString[1]);
                    yDiePit = Double.Parse(splitString[2]);
                }
                else if (lines[i].Contains("SampleCenterLocation "))
                {
                    xCenter = Double.Parse(splitString[1]);
                    yCenter = Double.Parse(splitString[2]);
                }
                else if (lines[i].Contains("DieOrigin "))
                {
                    xDieOri = Double.Parse(splitString[1]);
                    yDieOri = Double.Parse(splitString[2]);
                }
                else if (lines[i].Contains("ClassLookup "))
                {
                    totalClassLookup = Double.Parse(splitString[1]);
                }
                else if (lines[i].Contains("SampleSize "))
                {
                    waferSize = Double.Parse(splitString[2]) * 1000;
                }
                else if (lines[i].Contains("OrientationMarkLocation "))
                {
                    String n = splitString[1];

                    if(n.Contains("UP"))
                    {
                        notch = (Math.PI / 180) * 0;
                    }
                    else if(n.Contains("RIGHT"))
                    {
                        notch = (Math.PI / 180) * 90;
                    }
                    else if (n.Contains("DOWN"))
                    {
                        notch = (Math.PI / 180) * 180;
                    }
                    else if (n.Contains("LEFT"))
                    {
                        notch = (Math.PI / 180) * 270;
                    }
                }
                splitString = splitString.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                //Creates the column variables
                if (hasCols == false && lines[i].Contains("DefectRecordSpec"))
                {
                    hasCols = true;

                    var headers = new List<string>();
                    headers.AddRange(splitString.Skip(2));
                    numberOfVariables = headers.Count();

                    for (int a = 0; a < headers.Count; a++)
                    {
                        if (headers[a] == "")
                        {
                            headers.RemoveAt(a);
                        }
                    }

                    //Code specifically for v1.7 where we only have 19 specified columns but we have 21 numbers 
                    if (fileVersion == 1.7)
                    {
                        headers.Add("ExtraCol1");
                        headers.Add("ExtraCol2");

                        numberOfVariables = 21;
                    }

                    //Adds in the columns into our df
                    foreach (string colName in headers)
                    {
                        df.AddColumn(new CenterSpace.NMath.Stats.DFNumericColumn(colName));
                    }

                    //Since our lines aren't delimited by semicolons on v1.1 and v1.2, we do our parsing here 
                    if (fileVersion == 1.1)
                    {
                        string data = lines[i + 1];
                        string[] dataLines = data.Split("\n").Skip(2).ToArray();
                        for (int l = 0; l < dataLines.Length; l++)
                        {
                            string[] tempLine = dataLines[l].Split(" ");
                            tempLine = tempLine.Skip(1).ToArray();

                            List<double> SingleTiffInfo = new List<double>();
                            foreach (string ssss in tempLine)
                            {
                                SingleTiffInfo.Add(Double.Parse(ssss));
                            }
                            df.AddRow(SingleTiffInfo);
                        }

                    }
                    else if (fileVersion == 1.2)
                    {
                        string data = lines[i + 1];
                        string[] dataLines = data.Split("\n").Skip(2).ToArray();
                        for (int l = 0; l < dataLines.Length; l++)
                        {
                            string[] tempLine = dataLines[l].Split(" ");
                            tempLine = tempLine.Skip(1).ToArray();

                            List<double> SingleTiffInfo = new List<double>();

                            foreach (string ssss in tempLine)
                            {
                                SingleTiffInfo.Add(Double.Parse(ssss));
                            }
                            df.AddRow(SingleTiffInfo);
                        }
                    }
                }
                else if (numberOfVariables == splitString.Length)
                {
                    //Creates a list that represents a row in the df
                    List<double> SingleTiffInfo = new List<double>();
                    foreach (string S in splitString)
                    {
                        SingleTiffInfo.Add(Convert.ToDouble(S));
                    }

                    //Add in this row into the df
                    df.AddRow(SingleTiffInfo);
                }
            }
            //Data has been parsed, we want to add in another column for our X/Y calculations

            var colX = new CenterSpace.NMath.Stats.DFNumericColumn("CalcX");
            var colY = new CenterSpace.NMath.Stats.DFNumericColumn("CalcY");

            for (int i = 0; i < df.Rows; i++)
            {
                colX.Add((double)df[df.IndexOfColumn("XINDEX")][i] * xDiePit - xDieOri + (double)df[df.IndexOfColumn("XREL")][i]);
                colY.Add((double)df[df.IndexOfColumn("YINDEX")][i] * yDiePit - yDieOri + (double)df[df.IndexOfColumn("YREL")][i]);
            }

            df.AddColumn(colX);
            df.AddColumn(colY);
        }
    }
}
