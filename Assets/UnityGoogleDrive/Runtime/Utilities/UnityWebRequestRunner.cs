﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

#if !UNITY_2017_2_OR_NEWER
/// <summary>
/// Wrapper to use <see cref="UnityEngine.Networking.UnityWebRequest"/> in event-driven manner.
/// In Unity 2017.2 'completed' event was introduced, which should be used instead. 
/// </summary>
public class UnityWebRequestRunner : AsyncOperation
{
    class CoroutineContainer : MonoBehaviour { }

    #pragma warning disable IDE1006 
    public event Action<AsyncOperation> completed;
    #pragma warning restore IDE1006 

    public UnityWebRequest UnityWebRequest { get; private set; }
    public AsyncOperation RequestYield { get; private set; }

    private MonoBehaviour coroutineContainer;
    private GameObject containerObject;
    private Coroutine coroutine;

    public UnityWebRequestRunner (UnityWebRequest unityWebRequest, MonoBehaviour coroutineContainer = null)
    {
        UnityWebRequest = unityWebRequest;
        if (coroutineContainer) this.coroutineContainer = coroutineContainer;
        else this.coroutineContainer = CreateContainer();
    }

    public void Run ()
    {
        if (UnityWebRequest == null)
        {
            Debug.LogWarning("UnityGoogleDrive: Attempted to start UnityWebRequestRunner with non-valid UnityWebRequest.");
            OnComplete();
            return;
        }

        if (!coroutineContainer || !coroutineContainer.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("UnityGoogleDrive: Attempted to start UnityWebRequestRunner with non-valid container.");
            OnComplete();
            return;
        }

        coroutine = coroutineContainer.StartCoroutine(RequestRoutine());
        RequestYield = UnityWebRequest.Send();
    }

    public void Abort ()
    {
        if (coroutine != null)
        {
            if (coroutineContainer)
                coroutineContainer.StopCoroutine(coroutine);
            coroutine = null;
        }

        UnityWebRequest.Abort();

        OnComplete();
    }

    private IEnumerator RequestRoutine ()
    {
        while (UnityWebRequest != null && !UnityWebRequest.isDone)
            yield return null;
        OnComplete();
    }

    private void OnComplete ()
    {
        if (completed != null)
            completed.Invoke(RequestYield);
        if (containerObject)
            UnityEngine.Object.Destroy(containerObject);
    }

    private MonoBehaviour CreateContainer ()
    {
        containerObject = new GameObject("UnityWebRequest");
        containerObject.hideFlags = HideFlags.DontSave;
        UnityEngine.Object.DontDestroyOnLoad(containerObject);
        return containerObject.AddComponent<CoroutineContainer>();
    }
}
#endif

public static class UnityWebRequestExtensions
{
    #if UNITY_2017_2_OR_NEWER
    public static UnityWebRequestAsyncOperation RunWebRequest (this UnityWebRequest unityWebRequest)
    {
        var webAsync = unityWebRequest.SendWebRequest();
        return webAsync;
    }
    public static UnityWebRequestAsyncOperation RunWebRequest (this UnityWebRequest unityWebRequest, ref AsyncOperation outAsyncOperation)
    {
        var webAsync = unityWebRequest.SendWebRequest();
        outAsyncOperation = webAsync;
        return webAsync;
    }
    #else
    public static UnityWebRequestRunner RunWebRequest (this UnityWebRequest unityWebRequest)
    {
        var runner = new UnityWebRequestRunner(unityWebRequest);
        runner.Run();
        return runner;
    }
    public static UnityWebRequestRunner RunWebRequest (this UnityWebRequest unityWebRequest, ref AsyncOperation outAsyncOperation)
    {
        var runner = new UnityWebRequestRunner(unityWebRequest);
        runner.Run();
        outAsyncOperation = runner.RequestYield;
        return runner;
    }
    #endif
}
