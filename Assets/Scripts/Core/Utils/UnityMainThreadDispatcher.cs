using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<System.Action> _executionQueue = new Queue<System.Action>();
    private readonly object _lock = new object();
    
    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    public void Update()
    {
        lock (_lock)
        {
            while (_executionQueue.Count > 0)
            {
                var action = _executionQueue.Dequeue();
                action?.Invoke();
            }
        }
    }
    
    public void Enqueue(System.Action action)
    {
        if (action == null) return;
        
        lock (_lock)
        {
            _executionQueue.Enqueue(action);
        }
    }
    
    private void OnDestroy()
    {
        _executionQueue.Clear();
    }
}
