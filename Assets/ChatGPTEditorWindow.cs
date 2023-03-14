using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class ChatGPTEditorWindow : EditorWindow
{
    [SerializeField] MonoScript script;
    [SerializeField] ChatGPTClient gptChat = new ChatGPTClient();
    [SerializeField] string customTask;

    [SerializeField] int instanceId = 0;

    [SerializeField] Vector2 scrollCustomTask;
    [SerializeField] Vector2 scroll;
    [SerializeField] Vector2 scroll1;

    [SerializeField] string oldContent;
    [SerializeField] string newContent;

    [SerializeField] bool selectingScript;
    [SerializeField] bool useScriptAsContext = true;

    [MenuItem("Tools/OpenAI/Chat GPT")]
    public static void Create() => GetWindow<ChatGPTEditorWindow>("Chat GPT Helper");

    static string WrapPrompt(string input) =>
                    input
                    + " Don't include any note nor explanation in the response."
                    + " I only need the code body.";

    void OnGUI()
    {
        EditorGUILayout.Space(5);
        script = EditorGUILayout.ObjectField("Select a Script", script, typeof(MonoScript), false) as MonoScript;
        if (script == null) 
            return;

        string content;
        if (selectingScript == false)
        {
            instanceId = script.GetInstanceID();
            content = oldContent = ReadFile(AssetDatabase.GetAssetPath(instanceId));

            void Callback(ChatGPTResponse choice, bool isError)
            {
                EditorUtility.ClearProgressBar();

                if (isError)
                {
                    Debug.LogError("A error occurred while trying to communicate with Open AI Chat GPT");
                    return;
                }

                content = choice.Choices[0].Message.Content;

                selectingScript = true;

                int n = 0;
                foreach (char item in content)
                {
                    if (item == '\n')
                        n++;
                    else
                        break;
                }

                content = content.Remove(0, n);
                newContent = content;
            }

            if (GUILayout.Button("Optimize my code"))
            {
                string refactor = WrapPrompt("Refactor this Unity C# code to make it more optimized: " + content);
                gptChat.SendPrompt(refactor, Callback);

                EditorUtility.DisplayProgressBar("Communicating with OpenAI", "Generating...", 0.4f);
            }

            if (GUILayout.Button("Comment my code"))
            {
                string refactor = WrapPrompt("Add comments to this Unity C# code to make it more clear: " + content);
                gptChat.SendPrompt(refactor, Callback);

                EditorUtility.DisplayProgressBar("Communicating with OpenAI", "Generating...", 0.4f);
            }

            EditorGUILayout.Space(10);

            using (var v = new EditorGUILayout.ScrollViewScope(scrollCustomTask, GUILayout.Height(60), GUILayout.ExpandWidth(false)))
            {
                scrollCustomTask = v.scrollPosition;
                customTask = EditorGUILayout.TextArea(customTask, GUILayout.ExpandHeight(true));
            }
            if (GUILayout.Button("Custom task"))
            {
                string refactor = WrapPrompt(customTask + (useScriptAsContext ? ": " + content : ""));
                gptChat.SendPrompt(refactor, Callback);

                EditorUtility.DisplayProgressBar("Communicating with OpenAI", "Generating...", 0.4f);
            }
            useScriptAsContext = GUILayout.Toggle(useScriptAsContext, "Add Script as Context");

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Content");
            using (var v = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = v.scrollPosition;
                EditorGUILayout.TextArea(oldContent, EditorStyles.helpBox);
            }
        }
        else
        {
            EditorGUILayout.Space(10);
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Old Content");

                    if (GUILayout.Button("Choose"))
                        selectingScript = false;
                }
                using (var s = new EditorGUILayout.ScrollViewScope(scroll))
                {
                    scroll = s.scrollPosition;

                    EditorGUILayout.TextArea(oldContent, EditorStyles.helpBox);
                }
            }

            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("New Content");
                    if (GUILayout.Button("Choose"))
                    {
                        SaveFile(AssetDatabase.GetAssetPath(instanceId), newContent);
                        selectingScript = false;
                    }
                }
                using (var s = new EditorGUILayout.ScrollViewScope(scroll1))
                {
                    scroll1 = s.scrollPosition;

                    EditorGUILayout.TextArea(newContent, EditorStyles.helpBox);
                }
            }
        }

        GUILayout.FlexibleSpace();
    }

    string ReadFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            StreamReader fileReader = new StreamReader(filePath, Encoding.GetEncoding("iso-8859-1"));
            string file = fileReader.ReadToEnd();

            fileReader.Close();

            return file;
        }
        else
        {
            Debug.LogError("File not found");
            return "File not found or is empty.";
        }
    }

    void SaveFile(string filePath, string content)
    {
        string path = filePath;

        Debug.Log("Script saved at " + path);

        FileStream stream = new FileStream(path, FileMode.Create);

        StreamWriter fileWriter = new StreamWriter(stream, Encoding.GetEncoding("iso-8859-1"));
        fileWriter.Write(content);
        fileWriter.Close();

        AssetDatabase.ImportAsset(filePath);
    }
}

[FilePath("UserSettings/OpenAISettings.asset", FilePathAttribute.Location.ProjectFolder)]
public sealed class OpenAISettings : ScriptableSingleton<OpenAISettings>
{
    public string apiURL = "https://api.openai.com/v1/chat/completions";
    public string apiKey = "";
    public string apiOrganization = "";
    public string apiModel = "gpt-3.5-turbo";

    public void Save() => Save(true);

    void OnDisable() => Save();
}

sealed class OpenAISettingsProvider : SettingsProvider
{
    public OpenAISettingsProvider() : base("Project/Chat GPT API", SettingsScope.Project) { }

    [SettingsProvider]
    public static SettingsProvider CreateCustomSettingsProvider() => new OpenAISettingsProvider();

    public override void OnGUI(string search)
    {
        OpenAISettings settings = OpenAISettings.instance;
        string url = settings.apiURL;
        string key = settings.apiKey;
        string organization = settings.apiOrganization;
        string model = settings.apiModel;

        EditorGUI.BeginChangeCheck();

        url = EditorGUILayout.TextField("Url", url);
        key = EditorGUILayout.TextField("Key", key);
        organization = EditorGUILayout.TextField("Organization", organization);
        model = EditorGUILayout.TextField("Model", model);
        if (EditorGUILayout.LinkButton("Open AI API"))
        {
            Application.OpenURL("https://platform.openai.com/docs/api-reference/chat#:~:text=POST-,https%3A//api.openai.com/v1/chat/completions,-Creates%20a%20completion");
        }

        if (EditorGUI.EndChangeCheck())
        {
            settings.apiURL = url;
            settings.apiKey = key;
            settings.apiOrganization = organization;
            settings.apiModel = model;
            settings.Save();
        }
    }
}