using UnityEngine;
using TMPro; // 引用 TextMesh Pro 命名空間
using System.Text;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPerformanceHUD : MonoBehaviour
{
    [Header("Dependencies")]
    public ARSkeletonSender senderScript; // 請拖曳你的 Sender 腳本過來
    public ARSession arSession;

    [Header("UI Reference")]
    [Tooltip("請將場景中的 TextMeshPro UGUI 物件拖曳到這裡")]
    public TMP_Text infoText; // 使用 TMP_Text 可以同時支援 UI (Canvas) 和 3D World Text

    // FPS 計算相關
    private float deltaTime = 0.0f;
    private float currentFPS = 0.0f;

    // PPS (Packets Per Second) 計算相關
    private float ppsTimer = 0.0f;
    private int lastTotalPackets = 0;
    private float currentPPS = 0.0f;

    void Update()
    {
        // --- 1. 計算邏輯 (保持原本的數學運算) ---
        
        // 計算 FPS (平滑處理)
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        currentFPS = 1.0f / deltaTime;

        // 計算 PPS (每 0.5 秒更新一次數值)
        ppsTimer += Time.unscaledDeltaTime;
        if (ppsTimer >= 0.5f)
        {
            if (senderScript != null)
            {
                int currentTotal = senderScript.TotalPacketsSent;
                // (現在總數 - 上次總數) / 經過時間 = 每秒包數
                currentPPS = (currentTotal - lastTotalPackets) / ppsTimer;
                lastTotalPackets = currentTotal;
            }
            ppsTimer = 0f;
        }

        // --- 2. 顯示邏輯 (更新到 TMP) ---
        UpdateHUDText();
    }

    void UpdateHUDText()
    {
        if (infoText == null) return;

        StringBuilder sb = new StringBuilder();

        // A. 效能資訊 (FPS)
        sb.AppendLine($"Performance: {deltaTime * 1000.0f:0.0} ms ({currentFPS:0.} fps)");

        // B. 網路資訊 (PPS)
        if (senderScript != null)
        {
            string mode = senderScript.useReducedMode ? "Reduced (14pts)" : "Full (91pts)";
            sb.AppendLine($"UDP Mode: {mode}");
            sb.AppendLine($"UDP Rate: {currentPPS:0} pkts/sec");
            sb.AppendLine($"Total Sent: {senderScript.TotalPacketsSent}");
        }
        else
        {
            sb.AppendLine("<color=red>No Sender Linked!</color>");
        }

        sb.AppendLine("---------------------------");

        if (senderScript != null && senderScript.humanBodyManager != null)
        {
            var manager = senderScript.humanBodyManager;
            // 檢查 Manager 是否已啟用
            sb.AppendLine($"Body Manager: {(manager.enabled ? "Enabled" : "Disabled")}");
            
            // 檢查目前畫面上「追蹤中」的人體數量
            int trackedCount = manager.trackables.count;
            string countColor = trackedCount > 0 ? "green" : "red";
            sb.AppendLine($"Tracked Bodies: <color={countColor}>{trackedCount}</color>");

            // 檢查 Pose 3D 是否真的啟動 (讀取屬性)
            //sb.AppendLine($"Pose 3D Config: {manager.humanBodyPose3DEstimationEnabled}");
            //sb.AppendLine($"Depth mode open: {manager.humanSegmentationDepthMode}");
        }
        else
        {
            sb.AppendLine("<color=red>Sender or Manager Link Missing!</color>");
        }

        // D. AR Session 狀態
        sb.AppendLine($"AR State: {ARSession.state}");

        // 最後統一賦值給 TextMeshPro
        infoText.text = sb.ToString();
    }
}