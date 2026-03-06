using System.Collections.Generic;
using UnityEngine;

public class SaveTaskQueue
{
    private readonly Queue<SaveTask> _queue = new Queue<SaveTask>();
    private readonly object _lock = new object();
    private readonly int _maxQueueSize;

    public SaveTaskQueue(int maxQueueSize = 100)
    {
        _maxQueueSize = maxQueueSize;
    }

    public bool TryEnqueue(SaveTask task)
    {
        if (task == null || task.Snapshot == null)
        {
            Debug.LogWarning("[SaveTaskQueue] Cannot enqueue null task or task with null snapshot");
            return false;
        }

        lock (_lock)
        {
            if (_queue.Count >= _maxQueueSize)
            {
                Debug.LogWarning($"[SaveTaskQueue] Queue is full ({_maxQueueSize}), dropping oldest task");
                _queue.Dequeue();
            }
            _queue.Enqueue(task);
            return true;
        }
    }

    public bool TryDequeue(out SaveTask task)
    {
        lock (_lock)
        {
            if (_queue.Count > 0)
            {
                task = _queue.Dequeue();
                return true;
            }
            task = null;
            return false;
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count == 0;
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
        }
    }
}
