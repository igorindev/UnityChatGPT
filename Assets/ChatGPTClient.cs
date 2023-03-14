using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public sealed class ChatGPTChatChoice
{
    public int Index { get; set; }
    public ChatGPTChatMessage Message { get; set; }
    public string FinishReason { get; set; }
}

[Serializable]
public sealed class ChatGPTChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
}

[Serializable]
public sealed class ChatGPTChatUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

[Serializable]
public sealed class ChatGPTMessage
{
    public string role;
    public string content;
}

public sealed class ChatGPTRequest
{
    [JsonProperty(PropertyName = "model")]
    public string Model { get; set; }

    [JsonProperty(PropertyName = "messages")]
    public ChatGPTMessage[] Messages { get; set; }
}

[Serializable]
public sealed class ChatGPTResponse
{
    public string Id { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public List<ChatGPTChatChoice> Choices { get; set; }
    public ChatGPTChatUsage Usage { get; set; }
    public double ResponseTotalTime { get; set; }
}

public class ChatGPTClient
{
    public void SendPrompt(string prompt, Action<ChatGPTResponse, bool> callBack)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(SendPromptCoroutine(prompt, callBack));
    }

    IEnumerator SendPromptCoroutine(string prompt, Action<ChatGPTResponse, bool> callBack)
    {
        string url = OpenAISettings.instance.apiURL;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            string requestParams = JsonConvert.SerializeObject(new ChatGPTRequest
            {
                Model = OpenAISettings.instance.apiModel,
                Messages = new ChatGPTMessage[]
                {
                    new ChatGPTMessage { role = "user", content = prompt }
                }
            });

            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestParams);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.disposeCertificateHandlerOnDispose = true;

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {OpenAISettings.instance.apiKey}");
            request.SetRequestHeader("OpenAI-Organization", OpenAISettings.instance.apiOrganization);

            DateTime requestStartDateTime = DateTime.Now;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.Log(request.error);

                callBack?.Invoke(null, true);
            }
            else
            {
                string responseInfo = request.downloadHandler.text;
                ChatGPTResponse response = JsonConvert.DeserializeObject<ChatGPTResponse>(responseInfo);

                response.ResponseTotalTime = (DateTime.Now - requestStartDateTime).TotalMilliseconds;

                callBack?.Invoke(response, false);
            }
        }
    }
}