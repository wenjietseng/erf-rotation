using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Globalization;

namespace DataFileIO
{
    // https://frl.nyu.edu/write-data-locally-on-oculus-quest/
    public class WriteFile
    {
        private StreamWriter sw;
        private string full_path_to_file;
        public void CreateFile(string fileName, string fileHeader, bool useHeader = true)
        {
            // string full_path_to_file = "Assets/Resources/Data/" + fileName + ".txt";
            // full_path_to_file = Application.dataPath + "/CSV/" + fileName + ".csv";    // win
            full_path_to_file = Application.persistentDataPath + "/" + fileName + ".csv"; // quest
            Debug.Log(full_path_to_file);
            sw = new StreamWriter(full_path_to_file, true);
            if (useHeader) WriteData(fileHeader);
        }

        public void WriteData(string line, bool debug = false)
        {
            line = line + "\n";
            sw.Write(line);
            if (debug) Debug.Log(line);
            sw.Flush();
            // sw.Close();
            // sw.Dispose();
        }
    }

    public class ReadFile
    {
        // data type setting, modify if needed
        public List<Vector3> posList;
        // read setting
        private FileInfo sourceFile = null;
        private StreamReader sr = null;
        private string full_path_to_file;
        public void OpenFile(string fileName, string type = ".txt", bool header = true)
        {
            full_path_to_file = Application.persistentDataPath + "/Gesture/" + fileName + type;
            sourceFile = new FileInfo(full_path_to_file);
            sr = sourceFile.OpenText();
            // parse header
            string text = "";
            if (header)
            {
                text = sr.ReadLine();
            }
            int numOfLine = 0;
            bool endOfLine = false;
            
            posList = new List<Vector3>();
            while (!endOfLine)
            {
                if (numOfLine < 72)
                {
                    text = sr.ReadLine();
                    // Debug.Log(text);
                    numOfLine++;
                    // modify data format if needed
                    List<float> nums = ParseLine(text);
                    if (nums.Count == 3) posList.Add(new Vector3(nums[0], nums[1], nums[2]));
                }
                else
                {
                    endOfLine = true;
                    sr.Close();
                    Debug.Log("Load " + fileName + type);
                }
            }
        }

        public List<float> ParseLine(string input)
        {
            // String spiltPattern = ",";
            // String[] elements = text.Split(spiltPattern.ToCharArray());

            string tmp = input ?? "";
            string[] elements = tmp.Split(',');
            List<float> outNums = new List<float>();

            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == "" || elements[i] == null) continue;
                outNums.Add(float.Parse(elements[i], CultureInfo.InvariantCulture.NumberFormat));
            }

            return outNums;
        }
    }
}