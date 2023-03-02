using System.Collections.Generic;
using UnityEngine;

namespace TheGame.GoogleSheet
{
    public class ImporterConfig : ScriptableObject
    {
        public string fileId;
        public string ouputPath;
        public List<string> sheets;
    }
}