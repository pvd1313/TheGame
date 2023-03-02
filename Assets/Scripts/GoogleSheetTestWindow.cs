using GoogleSheetsToUnity;
using UnityEditor;
using UnityEngine;

namespace TheGame
{
    public class GoogleSheetTestWindow : EditorWindow
    {
        private string sheetId;
        private string worksheetName;
        
        [MenuItem(nameof(TheGame) + "/" + nameof(GoogleSheetTestWindow))]
        private static void Initialize()
        {
            var window = (GoogleSheetTestWindow) GetWindow(typeof(GoogleSheetTestWindow));
            
            window.Show();
        }

        private void OnGUI()
        {
            sheetId = GUILayout.TextField(sheetId);
            worksheetName = GUILayout.TextField(worksheetName);
            
            if (GUILayout.Button("Do it!"))
            {
                var manager = new SpreadsheetManager();
                
                SpreadsheetManager.Read(new GoogleSheetsRequest(sheetId, worksheetName),
                    (data) =>
                    {
                        int b = 2;
                    });
            }
        }
    }
}