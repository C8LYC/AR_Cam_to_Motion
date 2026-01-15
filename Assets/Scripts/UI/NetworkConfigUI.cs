using UnityEngine;
using UnityEngine.UI; // 引用標準 UI
// 如果你使用 TextMeshPro，請改用 TMPro 命名空間，並將 InputField 改為 TMP_InputField

public class NetworkConfigUI : MonoBehaviour
{
    [Header("References")]
    public ARSkeletonSender sender; // 連結到你的 Sender 腳本
    public GameObject configPanel;  // 設定視窗的 Panel (包含輸入框)
    public Button settingsButton;   // 畫面角落的設定按鈕

    [Header("UI Inputs")]
    public InputField ipInputField;   // 輸入 IP
    public InputField portInputField; // 輸入 Port
    public Text statusText;           // 顯示當前狀態 (Optional)

    void Start()
    {
        // 初始化：先隱藏設定選單
        configPanel.SetActive(false);
        
        // 預填當前的設定值
        if (sender != null)
        {
            ipInputField.text = sender.pcIpAddress;
            portInputField.text = sender.pcPort.ToString();
        }

        // 設定 Port 輸入框只能輸入數字 (整數)
        portInputField.contentType = InputField.ContentType.IntegerNumber;
    }

    void Update()
    {
        // 簡單更新狀態文字 (如果有拉這個 UI)
        if (statusText != null && sender != null)
        {
            string state = sender.isSending ? $"Sending ({sender.TotalPacketsSent})" : "Paused";
            statusText.text = $"Status: {state}\nTarget: {sender.pcIpAddress}:{sender.pcPort}";
        }
    }

    // 按下「設定按鈕」時呼叫
    public void OnOpenSettings()
    {
        // 1. 暫停傳送
        sender.SetSendingState(false);
        
        // 2. 顯示輸入面板
        configPanel.SetActive(true);
        settingsButton.gameObject.SetActive(false); // 隱藏設定按鈕避免重複點擊

        // 3. 更新輸入框顯示當前數值
        ipInputField.text = sender.pcIpAddress;
        portInputField.text = sender.pcPort.ToString();
    }

    // 按下設定面板中的「確認/連線」按鈕時呼叫
    public void OnConfirmSettings()
    {
        string newIp = ipInputField.text.Trim();
        string newPortStr = portInputField.text.Trim();

        // 簡單驗證
        if (string.IsNullOrEmpty(newIp)) 
        {
            Debug.LogWarning("IP Cannot be empty");
            return;
        }

        if (int.TryParse(newPortStr, out int newPort))
        {
            // 1. 更新 Sender 資訊
            sender.SetConnectionInfo(newIp, newPort);

            // 2. 恢復傳送
            sender.SetSendingState(true);

            // 3. 關閉面板
            ClosePanel();
        }
        else
        {
            Debug.LogError("Invalid Port Number");
            // 你可以在這裡加入跳出錯誤視窗的邏輯
        }
    }

    // 按下「取消」按鈕時呼叫
    public void OnCancel()
    {
        // 不更新數值，直接恢復傳送並關閉面板
        sender.SetSendingState(true);
        ClosePanel();
    }

    private void ClosePanel()
    {
        configPanel.SetActive(false);
        settingsButton.gameObject.SetActive(true);
    }
}