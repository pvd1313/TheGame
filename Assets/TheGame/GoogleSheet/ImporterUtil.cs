using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GoogleSheetsToUnity;
using Newtonsoft.Json;
using UnityEngine;

namespace TheGame.GoogleSheet
{
    public static class ImporterUtil
    {
        private static readonly Regex ArrayElementRegex;

        static ImporterUtil()
        {
            try
            {
                ArrayElementRegex = new Regex(@"([a-zA-Z]{1,})\[([0-9]{1,})].([a-zA-Z]{1,})$", RegexOptions.Compiled);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        public static void AsyncImportRecursive(ImporterConfig config, string outputPath, int depth, int successCount)
        {
            if (config.sheets.Count == depth)
            {
                if (successCount == depth)
                {
                    Debug.Log("All done");
                }
                else
                {
                    Debug.LogError($"Finished with errors. Check log for details. Imported {successCount} / {config.sheets.Count}");
                }
                
                return;
            }

            string sheetName = config.sheets[depth];
            
            Debug.Log($"Fetching '{sheetName}' ({depth} / {config.sheets.Count})...");
            
            GoogleSheetManager.Read(new GoogleSheetRequest(config.fileId, sheetName), 
                data =>
                {
                    bool success = TryParseAndSaveData(outputPath, sheetName, data);
                    
                    AsyncImportRecursive(config, outputPath, depth + 1, successCount + (success ? 1 : 0));
                });
        }

        private static bool TryParseAndSaveData(string outputFolder, string sheetName, SpreadsheetData data)
        {
            if (data?.values == null || data.values.Count == 0 || data.values[0] == null)
            {
                Debug.LogError($"{sheetName} has no values");
                return false;
            }

            FileMeta fileMeta;

            try
            {
                fileMeta = JsonConvert.DeserializeObject<FileMeta>(data.values[0][0]);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to deserialize {nameof(FileMeta)} in '{sheetName}'");
                return false;
            }

            try
            {
                List<string> metaRow = data.values[0];
                List<JsonObject> objects = new ();
                List<bool> headerExists = new ();
                List<HeaderMeta> headersMeta = new ();

                headerExists.Add(false);
                headersMeta.Add(default);

                for (int i = 1; i < metaRow.Count; i++)
                {
                    if (string.IsNullOrEmpty(metaRow[i]))
                    {
                        headerExists.Add(false);
                        headersMeta.Add(default);
                    }
                    else
                    {
                        headerExists.Add(true);
                        headersMeta.Add(ParseHeader(metaRow[i]));
                    }
                }

                for (int row = fileMeta.headerRowCount; row < data.values.Count; row++)
                {
                    List<string> currentRow = data.values[row];

                    JsonObject jsonObject = new ();

                    for (int col = 1; col < Math.Min(currentRow.Count, metaRow.Count); col++)
                    {
                        if (!headerExists[col] || string.IsNullOrEmpty(currentRow[col]))
                        {
                            continue;
                        }

                        jsonObject.Add(headersMeta[col], currentRow[col].Replace("$", ""));
                    }

                    objects.Add(jsonObject);
                }

                JsonOutput jsonOutput = new ()
                {
                    stream = new StringBuilder(),
                    lineSeparator = "\r\n",
                    space = " ",
                    tab = "    "
                };

                JsonObject.PrintArray(jsonOutput, string.Empty, objects);

                File.WriteAllText(Path.Combine(outputFolder, fileMeta.fileName), jsonOutput.stream.ToString(), Encoding.Unicode);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Exception when importing '{sheetName}'");
                return false;
            }

            return true;
        }

        private static HeaderMeta ParseHeader(string header)
        {
            Match arrayMatch = ArrayElementRegex.Match(header);
            
            if (arrayMatch.Success)
            {
                return new HeaderMeta
                {
                    isArrayElement = true,
                    arrayName = arrayMatch.Groups[1].Value,
                    arrayElementIndex = int.Parse(arrayMatch.Groups[2].Value),
                    parameterName = arrayMatch.Groups[3].Value
                };
            }

            return new HeaderMeta
            {
                parameterName = header
            };
        }
    }
}