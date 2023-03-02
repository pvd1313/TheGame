using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace GoogleSheetsToUnity
{
    public class GoogleSheetManager
    {
        static GoogleSheetConfig _config;
        
        public static GoogleSheetConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = (GoogleSheetConfig) Resources.Load("GSTU_Config");
                }

                return _config;
            }
        }
        
        static IEnumerator CheckForRefreshToken()
        {
            if (Application.isPlaying)
            {
                yield return new Task(GoogleAuthHelper.CheckForRefreshOfToken());
            }
#if UNITY_EDITOR
            else
            {
                yield return EditorCoroutineRunner.StartCoroutine(GoogleAuthHelper.CheckForRefreshOfToken());
            }
#endif
        }    
        
        public static void Read(GoogleSheetRequest search, UnityAction<SpreadsheetData> callback, bool containsMergedCells = false)
        {
            var sb = new StringBuilder();
            sb.Append("https://sheets.googleapis.com/v4/spreadsheets");
            sb.Append("/" + search.sheetId);
            sb.Append("/values");
            sb.Append("/" + search.worksheetName + "!" + search.startCell + ":" + search.endCell);
            sb.Append("?access_token=" + Config.gdr.access_token);

            var request = UnityWebRequest.Get(sb.ToString());

            if (Application.isPlaying)
            {
                new Task(Read(request, search, containsMergedCells, callback));
            }
#if UNITY_EDITOR
            else
            {
                EditorCoroutineRunner.StartCoroutine(Read(request,  search, containsMergedCells, callback));
            }
#endif
        }

        private static IEnumerator Read(UnityWebRequest request, GoogleSheetRequest search, bool containsMergedCells, UnityAction<SpreadsheetData> callback)
        {
            if (Application.isPlaying)
            {
                yield return new Task(CheckForRefreshToken());
            }
#if UNITY_EDITOR
            else
            {
                yield return EditorCoroutineRunner.StartCoroutine(CheckForRefreshToken());
            }
#endif

            using (request)
            {
                yield return request.SendWebRequest();

                if (string.IsNullOrEmpty(request.downloadHandler.text) || request.downloadHandler.text == "{}")
                {
                    Debug.LogError("SpreedSheetManager: Unable to retrieve data from google sheets");

                    callback?.Invoke(null);
                    
                    yield break;
                }

                if (request.responseCode == 404)
                {
                    Debug.LogError("Received 404 code as response");
                    
                    callback?.Invoke(null);
                    
                    yield break;
                }

                var rawData = JsonConvert.DeserializeObject<SpreadsheetData>(request.downloadHandler.text);;

                callback?.Invoke(rawData);
            }
        }
    }
}