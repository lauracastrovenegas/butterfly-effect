using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;  // Added this for Task support

/// <summary>
/// Handles executing actions on Unity's main thread, which is necessary for many Unity operations
/// like creating AudioClips or modifying UI elements.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly object Lock = new object();
    private readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = UnityEngine.Object.FindFirstObjectByType<UnityMainThreadDispatcher>();

                        if (_instance == null)
                        {
                            var go = new GameObject("UnityMainThreadDispatcher");
                            _instance = go.AddComponent<UnityMainThreadDispatcher>();
                            DontDestroyOnLoad(go);
                            Debug.Log("Created new UnityMainThreadDispatcher instance");
                        }
                    }
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("UnityMainThreadDispatcher initialized");
        }
        else if (_instance != this)
        {
            UnityEngine.Object.Destroy(gameObject);
            Debug.Log("Destroyed duplicate UnityMainThreadDispatcher");
        }
    }

    private void Update()
    {
        lock (Lock)
        {
            while (_executionQueue.Count > 0)
            {
                try
                {
                    _executionQueue.Dequeue().Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error executing queued action: {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null)
        {
            Debug.LogWarning("Attempted to enqueue null action");
            return;
        }

        lock (Lock)
        {
            _executionQueue.Enqueue(action);
        }
    }

    public Task EnqueueAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();

        Enqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        return tcs.Task;
    }

    public async Task EnqueueAsync(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<bool>();

        Enqueue(async () =>
        {
            try
            {
                await action();
                tcs.SetResult(true);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        await tcs.Task;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            lock (Lock)
            {
                _executionQueue.Clear();
            }
            _instance = null;
            Debug.Log("UnityMainThreadDispatcher destroyed");
        }
    }
}