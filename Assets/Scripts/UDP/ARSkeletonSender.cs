using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Net;
using System.Net.Sockets;
using System;

public class ARSkeletonSender : MonoBehaviour
{
    [Header("Network Settings")]
    public string pcIpAddress = "192.168.1.100";
    public int pcPort = 8080;

    [Header("Transmission Settings")]
    [Tooltip("勾選則只傳送辨識用的 14 個關鍵點，不勾則傳送全部 91 個點")]
    public bool useReducedMode = true;

    [Header("AR References")]
    public ARHumanBodyManager humanBodyManager;

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint; 

    public int TotalPacketsSent { get; private set; } = 0;
    public bool isSending { get; private set; } = true;

    void Start()
    {
        udpClient = new UdpClient();
        UpdateRemoteEndPoint();
    }

    // 更新連線資訊 (可從 UI 呼叫)
    public void SetConnectionInfo(string newIp, int newPort)
    {
        pcIpAddress = newIp;
        pcPort = newPort;
        UpdateRemoteEndPoint();
        Debug.Log($"[ARSkeletonSender] 目標地址已更新: {pcIpAddress}:{pcPort}");
    }

    private void UpdateRemoteEndPoint()
    {
        try
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(pcIpAddress), pcPort);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ARSkeletonSender] 無效的 IP 格式: {pcIpAddress} - {e.Message}");
        }
    }

    public void SetSendingState(bool state)
    {
        isSending = state;
    }

    void OnEnable()
    {
        if (humanBodyManager != null)
            humanBodyManager.humanBodiesChanged += OnHumanBodiesChanged;
    }

    void OnDisable()
    {
        if (humanBodyManager != null)
            humanBodyManager.humanBodiesChanged -= OnHumanBodiesChanged;
    }

    void OnHumanBodiesChanged(ARHumanBodiesChangedEventArgs eventArgs)
    {
        if (!isSending || remoteEndPoint == null) return;

        // 優先處理更新中的人體數據
        foreach (var humanBody in eventArgs.updated)
        {
            if (humanBody.trackingState == TrackingState.Tracking)
            {
                SendBodyData(humanBody);
                return; // 通常一次只處理一個主要的人體
            }
        }
        
        foreach (var humanBody in eventArgs.added)
        {
            if (humanBody.trackingState == TrackingState.Tracking)
            {
                SendBodyData(humanBody);
                return;
            }
        }
    }

    void SendBodyData(ARHumanBody body)
    {
        if (!body.joints.IsCreated) return;

        byte[] packet;

        // 根據設定選擇打包模式
        if (useReducedMode)
        {
            // 使用您指定的 14 個關鍵點進行辨識 (含 Position 與 Rotation)
            packet = SkeletonProtocol.PackReduced(body);
        }
        else
        {
            // 使用完整的 91 個關節進行骨架同步
            packet = SkeletonProtocol.PackFull(body);
        }

        if (packet == null || packet.Length == 0) return;

        try
        {
            // 使用非同步發送，確保不影響 AR 渲染效能 (避免掉幀)
            udpClient.BeginSend(packet, packet.Length, remoteEndPoint, null, null);
            TotalPacketsSent++;
        }
        catch (Exception e)
        {
            // 減少 Log 頻率，避免卡頓
            if (TotalPacketsSent % 100 == 0)
                Debug.LogWarning($"[ARSkeletonSender] UDP 發送失敗: {e.Message}");
        }
    }

    public void OpenReducedPackMode()
    {
        useReducedMode = true;
    }

    public void CloseReducedPackMode()
    {
        useReducedMode = false;
    }

    void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
}