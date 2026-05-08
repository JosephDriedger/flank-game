using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public sealed class OnlineMatchmakingService : MonoBehaviour
{
    public static OnlineMatchmakingService Instance { get; private set; }

    private Coroutine _heartbeatRoutine;
    private Coroutine _monitorRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _monitorRoutine = StartCoroutine(MonitorHostState());
    }

    private void OnDestroy()
    {
        // Best-effort cleanup if the service is torn down while hosting.
        if (!string.IsNullOrEmpty(OnlineSessionConfig.RoomId))
        {
            string id = OnlineSessionConfig.RoomId;
            OnlineSessionConfig.ClearRoom();
            StartCoroutine(DeleteRoomRoutine(id, null, null));
        }
    }

    // ================================================================
    // PUBLIC API
    // ================================================================

    public void FetchPublicIp(Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchPublicIpRoutine(onSuccess, onError));
    }

    public void CreateRoom(CreateRoomRequest request, Action<MatchmakingRoom> onSuccess, Action<string> onError)
    {
        StartCoroutine(CreateRoomRoutine(request, onSuccess, onError));
    }

    public void FetchRooms(Action<MatchmakingRoom[]> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchRoomsRoutine(onSuccess, onError));
    }

    public void GetRoom(string roomId, Action<MatchmakingRoom> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRoomRoutine(roomId, onSuccess, onError));
    }

    public void StartHeartbeat(string roomId)
    {
        StopHeartbeat();
        _heartbeatRoutine = StartCoroutine(HeartbeatRoutine(roomId));
    }

    public void StopHeartbeat()
    {
        if (_heartbeatRoutine == null) return;
        StopCoroutine(_heartbeatRoutine);
        _heartbeatRoutine = null;
    }

    public void DeleteCurrentRoom()
    {
        string id = OnlineSessionConfig.RoomId;
        if (string.IsNullOrEmpty(id)) return;

        StopHeartbeat();
        OnlineSessionConfig.ClearRoom();
        StartCoroutine(DeleteRoomRoutine(id, null, null));
    }

    // ================================================================
    // HOST STATE MONITOR — auto-deletes room when NGO stops hosting
    // ================================================================

    private IEnumerator MonitorHostState()
    {
        bool wasServer = false;
        WaitForSeconds wait = new WaitForSeconds(2f);

        while (true)
        {
            yield return wait;

            NetworkManager nm = NetworkManager.Singleton;
            bool isServer = nm != null && !nm.ShutdownInProgress && nm.IsServer;

            if (wasServer && !isServer && !string.IsNullOrEmpty(OnlineSessionConfig.RoomId))
            {
                string id = OnlineSessionConfig.RoomId;
                StopHeartbeat();
                OnlineSessionConfig.ClearRoom();
                yield return DeleteRoomRoutine(id, null, null);
            }

            wasServer = isServer;
        }
    }

    // ================================================================
    // COROUTINES
    // ================================================================

    private IEnumerator FetchPublicIpRoutine(Action<string> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest req = UnityWebRequest.Get("https://api.ipify.org"))
        {
            req.timeout = 6;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(req.downloadHandler.text.Trim());
            else
                onError?.Invoke(req.error);
        }
    }

    private IEnumerator CreateRoomRoutine(CreateRoomRequest body, Action<MatchmakingRoom> onSuccess, Action<string> onError)
    {
        string url   = BaseUrl() + "/rooms";
        byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 10;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                MatchmakingRoom room = JsonUtility.FromJson<MatchmakingRoom>(req.downloadHandler.text);
                OnlineSessionConfig.RoomId = room.id;
                onSuccess?.Invoke(room);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }

    private IEnumerator FetchRoomsRoutine(Action<MatchmakingRoom[]> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(BaseUrl() + "/rooms"))
        {
            req.timeout = 6;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                // JsonUtility cannot parse a top-level array; wrap it.
                string wrapped = "{\"rooms\":" + req.downloadHandler.text + "}";
                MatchmakingRoomList list = JsonUtility.FromJson<MatchmakingRoomList>(wrapped);
                onSuccess?.Invoke(list?.rooms ?? new MatchmakingRoom[0]);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }

    private IEnumerator GetRoomRoutine(string roomId, Action<MatchmakingRoom> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(BaseUrl() + "/rooms/" + roomId.ToUpper()))
        {
            req.timeout = 6;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(JsonUtility.FromJson<MatchmakingRoom>(req.downloadHandler.text));
            else
                onError?.Invoke(req.error);
        }
    }

    private IEnumerator DeleteRoomRoutine(string roomId, Action onSuccess, Action<string> onError)
    {
        using (UnityWebRequest req = UnityWebRequest.Delete(BaseUrl() + "/rooms/" + roomId))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 5;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke();
            else
                onError?.Invoke(req.error);
        }
    }

    private IEnumerator HeartbeatRoutine(string roomId)
    {
        WaitForSeconds interval = new WaitForSeconds(30f);

        while (true)
        {
            yield return interval;

            string url = BaseUrl() + "/rooms/" + roomId + "/heartbeat";
            using (UnityWebRequest req = new UnityWebRequest(url, "PUT"))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = 5;
                yield return req.SendWebRequest();
            }
        }
    }

    private static string BaseUrl() =>
        OnlineSessionConfig.MatchmakingServerUrl.TrimEnd('/');
}
