using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TheGame.GoogleSheet
{
    public class ImporterConfig : ScriptableObject
    {
        public string fileId;
        public string outputPath;
        public List<string> sheets;

        [Button]
        private void Import()
        {
            string path = Path.Combine(Application.dataPath, outputPath);

            ImporterUtil.AsyncImportRecursive(this, path, 0, 0);
        }
    }
}