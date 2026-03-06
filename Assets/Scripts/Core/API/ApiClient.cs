using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;

public class ApiException : Exception
{
    public long StatusCode { get; }
    public string ResponseBody { get; }

    public ApiException(string message, long statusCode, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

public class ApiClient : MonoBehaviour
{
    public delegate void RequestCompleteHandler(bool success, string response, string error);

    public void Post(string url, string body, Dictionary<string, string> headers, RequestCompleteHandler callback)
    {
        StartCoroutine(PostCoroutine(url, body, headers, callback));
    }

    public IEnumerator PostCoroutine(string url, string body, Dictionary<string, string> headers, RequestCompleteHandler callback)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        foreach (var header in headers)
        {
            request.SetRequestHeader(header.Key, header.Value);
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error = $"API Error: {request.error}";
            Debug.LogError($"[ApiClient] {error}");
            callback?.Invoke(false, null, error);
        }
        else
        {
            string response = request.downloadHandler.text;
            callback?.Invoke(true, response, null);
        }
    }

    public void PostAsync(string url, string body, Dictionary<string, string> headers, System.Action<string> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(PostAsyncCoroutine(url, body, headers, onSuccess, onError));
    }

    private IEnumerator PostAsyncCoroutine(string url, string body, Dictionary<string, string> headers, System.Action<string> onSuccess, System.Action<string> onError)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        foreach (var header in headers)
        {
            request.SetRequestHeader(header.Key, header.Value);
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error = $"API Error: {request.error}";
            Debug.LogError($"[ApiClient] {error}");
            onError?.Invoke(error);
        }
        else
        {
            string response = request.downloadHandler.text;
            onSuccess?.Invoke(response);
        }
    }

    public void PostMultipart(string url, byte[] data, string fileName, string contentType, Dictionary<string, string> headers, RequestCompleteHandler callback)
    {
        StartCoroutine(PostMultipartCoroutine(url, data, fileName, contentType, headers, callback));
    }

    private IEnumerator PostMultipartCoroutine(string url, byte[] data, string fileName, string contentType, Dictionary<string, string> headers, RequestCompleteHandler callback)
    {
        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(data);
        request.downloadHandler = new DownloadHandlerBuffer();

        string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
        string contentTypeHeader = $"multipart/form-data; boundary={boundary}";
        request.SetRequestHeader("Content-Type", contentTypeHeader);

        foreach (var header in headers)
        {
            request.SetRequestHeader(header.Key, header.Value);
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error = $"API Error: {request.error}";
            Debug.LogError($"[ApiClient] {error}");
            callback?.Invoke(false, null, error);
        }
        else
        {
            string response = request.downloadHandler.text;
            callback?.Invoke(true, response, null);
        }
    }
}
