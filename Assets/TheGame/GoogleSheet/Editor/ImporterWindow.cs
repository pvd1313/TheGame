using System;
using System.IO;
using System.Text;
using GoogleSheetsToUnity;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace TheGame.GoogleSheet.Editor
{
    public class ImporterWindow : EditorWindow
    {
        private ImporterConfig _config;

        private void OnGUI()
        {
            _config = (ImporterConfig) EditorGUILayout.ObjectField(_config, typeof(ImporterConfig), false);
            
            if (_config == null)
            {
                EditorGUILayout.LabelField("Select config to begin");
                return;
            }

            if (GUILayout.Button("Import"))
            {
                var outputPath = Path.Combine(Application.dataPath, _config.ouputPath);

                ImportRecursive(outputPath, 0);
            }
        }

        private void ImportRecursive(string outputPath, int depth)
        {
            if (_config.sheets.Count == depth)
            {
                Debug.Log("All done");
                return;
            }

            var sheetName = _config.sheets[depth];
            
            Debug.Log($"Fetching '{sheetName}' ({depth} / {_config.sheets.Count})...");
            
            GoogleSheetManager.Read(new GoogleSheetRequest(_config.fileId, sheetName), 
                data =>
                {
                    TryParseAndSaveData(outputPath, sheetName, data);
                    
                    ImportRecursive(outputPath, depth + 1);
                });
        }

        private void TryParseAndSaveData(string outputFolder, string sheetName, SpreadsheetData data)
        {
            if (data?.values == null || data.values.Count == 0 || data.values[0] == null)
            {
                Debug.LogError($"{sheetName} has no values");
                return;
                
            }

            SheetMetadata metadata = default;

            try
            {
                metadata = JsonConvert.DeserializeObject<SheetMetadata>(data.values[0][0]);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to parse metadata in {sheetName}");
            }

            var builder = new StringBuilder();
            const string tab = "    ";
            
            builder.AppendLine("[");

            var metaRow = data.values[0];

            for (var row = metadata.headerRowCount; row < data.values.Count; row++)
            {
                var currentRow = data.values[row];
                
                var anyCellWithValue = false;
                for (var col = 1; col < Math.Min(currentRow.Count, metaRow.Count); col++)
                {
                    if (string.IsNullOrEmpty(metaRow[col]) ||
                        string.IsNullOrEmpty(currentRow[col]))
                    {
                        continue;
                    }

                    anyCellWithValue = true;
                    break;
                }

                if (!anyCellWithValue)
                {
                    continue;
                }
                
                builder.Append(tab);
                builder.AppendLine("{");

                for (var col = 1; col < Math.Min(currentRow.Count, metaRow.Count); col++)
                {
                    if (string.IsNullOrEmpty(metaRow[col]) ||
                        string.IsNullOrEmpty(currentRow[col]))
                    {
                        continue;
                    }

                    var cellData = currentRow[col];
                    cellData = cellData.Replace("$", "");

                    builder.Append(tab);
                    builder.Append(tab);
                    builder.Append('"');
                    builder.Append(metaRow[col]);
                    builder.Append('"');
                    builder.Append(": \"");
                    builder.Append(cellData);
                    builder.Append('"');

                    if (col + 1 < Math.Min(currentRow.Count, metaRow.Count))
                    {
                        builder.Append(',');
                    }

                    builder.AppendLine();
                }
                
                builder.Append(tab);
                builder.Append('}');

                if (row + 1 < data.values.Count)
                {
                    builder.Append(',');
                }

                builder.AppendLine();
            }

            builder.AppendLine("]");
            
            File.WriteAllText(Path.Combine(outputFolder, metadata.fileName), builder.ToString(), Encoding.Unicode);
        }
    }
}