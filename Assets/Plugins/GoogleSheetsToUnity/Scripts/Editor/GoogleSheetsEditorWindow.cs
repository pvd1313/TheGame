using System.Net;
using UnityEditor;
using UnityEngine;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace GoogleSheetsToUnity.Editor
{
    public class GoogleSheetsEditorWindow : EditorWindow
    {
        private GoogleSheetsConfig _config;
        private bool _showSecret;
        private int _tabID;

        [MenuItem("Window/GoogleSheetsToUnity/Open Config")]
        private static void Open()
        {
            GoogleSheetsEditorWindow win = GetWindow<GoogleSheetsEditorWindow>("Google Sheets To Unity");
            ServicePointManager.ServerCertificateValidationCallback = Validator;

            win.Initialize();
        }

        private static bool Validator(object in_sender, X509Certificate in_certificate, X509Chain in_chain, SslPolicyErrors in_sslPolicyErrors)
        {
            return true;
        }

        private void Initialize()
        {
            _config = (GoogleSheetsConfig)Resources.Load("GSTU_Config");
        }

        private void OnGUI()
        {
            _tabID = GUILayout.Toolbar(_tabID, new string[] {"Private", "Private (Legacy)", "Public"});

            if (_config == null)
            {
                Debug.LogError("Error: no config file");
                return;
            }

            switch (_tabID)
            {
                case 0:
                    _config.CLIENT_ID = EditorGUILayout.TextField("Client ID", _config.CLIENT_ID);

                    GUILayout.BeginHorizontal();
                    if (_showSecret)
                    {
                        _config.CLIENT_SECRET = EditorGUILayout.TextField("Client Secret Code", _config.CLIENT_SECRET);
                    }
                    else
                    {
                        _config.CLIENT_SECRET = EditorGUILayout.PasswordField("Client Secret Code", _config.CLIENT_SECRET);

                    }

                    _showSecret = GUILayout.Toggle(_showSecret, "Show");
                    GUILayout.EndHorizontal();

                    _config.PORT = EditorGUILayout.IntField("Port number", _config.PORT);

                    if (GUILayout.Button("Build Connection"))
                    {
                        GoogleAuthHelper.BuildHttpListener();
                    }

                    break;
                case 1:
                    GUILayout.Label(
                        "This is the legacy version of GSTU and will be removed at a future date, if you wish to use it please press the button below");
                    if (GUILayout.Button("Use Legacy Version"))
                    {
                        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, (defines + ";" + "GSTU_Legacy"));
                    }

                    break;

                case 2:
                    _config.API_Key = EditorGUILayout.TextField("API Key", _config.API_Key);
                    break;
            }

            EditorUtility.SetDirty(_config);
        }
    }
}