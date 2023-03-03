using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TheGame.GoogleSheet
{
    public class ImporterConfig : ScriptableObject
    {
        public string fileId;
        public string ouputPath;
        public List<string> sheets;

        [Button]
        private void Import()
        {
            var outputPath = Path.Combine(Application.dataPath, ouputPath);

            ImportUtil.ImportRecursive(this, outputPath, 0);
        }
    }
}