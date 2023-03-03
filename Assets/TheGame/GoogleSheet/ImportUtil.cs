using System;
using System.IO;
using System.Text;
using GoogleSheetsToUnity;
using Newtonsoft.Json;
using UnityEngine;

namespace TheGame.GoogleSheet
{
    public static class ImportUtil
    {
        public static void ImportRecursive(ImporterConfig config, string outputPath, int depth)
        {
            if (config.sheets.Count == depth)
            {
                Debug.Log("All done");
                return;
            }

            string sheetName = config.sheets[depth];
            
            Debug.Log($"Fetching '{sheetName}' ({depth} / {config.sheets.Count})...");
            
            GoogleSheetManager.Read(new GoogleSheetRequest(config.fileId, sheetName), 
                data =>
                {
                    TryParseAndSaveData(outputPath, sheetName, data);
                    
                    ImportRecursive(config, outputPath, depth + 1);
                });
        }

        private static void TryParseAndSaveData(string outputFolder, string sheetName, SpreadsheetData data)
        {
            if (data?.values == null || data.values.Count == 0 || data.values[0] == null)
            {
                Debug.LogError($"{sheetName} has no values");
                return;
                
            }

            Metadata metadata = default;

            try
            {
                metadata = JsonConvert.DeserializeObject<Metadata>(data.values[0][0]);
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

            for (int row = metadata.headerRowCount; row < data.values.Count; row++)
            {
                var currentRow = data.values[row];
                
                bool anyCellWithValue = false;
                for (int col = 1; col < Math.Min(currentRow.Count, metaRow.Count); col++)
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

                for (int col = 1; col < Math.Min(currentRow.Count, metaRow.Count); col++)
                {
                    if (string.IsNullOrEmpty(metaRow[col]) ||
                        string.IsNullOrEmpty(currentRow[col]))
                    {
                        continue;
                    }

                    string cellData = currentRow[col];
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