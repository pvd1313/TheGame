using System;
using System.Collections.Generic;

namespace GoogleSheetsToUnity
{
    [Serializable]
    public class SpreadsheetData
    {
        public string range = "";
        public string majorDimension;
        public List<List<string>> values;
    }
}