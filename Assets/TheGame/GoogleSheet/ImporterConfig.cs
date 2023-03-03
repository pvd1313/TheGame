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
            string outputPath = Path.Combine(Application.dataPath, this.outputPath);

            ImportUtil.ImportRecursive(this, outputPath, 0);
        }
    }
}