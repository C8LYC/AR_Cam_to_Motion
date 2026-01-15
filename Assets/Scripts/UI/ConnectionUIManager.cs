using UnityEngine;
using TMPro; // 引用 TextMesh Pro
using UnityEngine.UI;

public class ConnectionUIManager : MonoBehaviour
{
    [Header("References")]
    public ARSkeletonSender senderScript; // 拖曳你的 Sender 物件
    public TMP_InputField ipInputField;   // 拖曳 IP 輸入框
    public TMP_InputField portInputField; // 拖曳 Port 輸入框
    public Button applyButton;            // 拖曳確認按鈕

    [Header("Defaults")]
    private const string PREF_IP = "LastIP";
    private const string PREF_PORT = "LastPort";

    void Start()
    {
        // 1. 讀取上次儲存的設定，如果沒有則使用 Sender 目前的預設值
        string savedIp = PlayerPrefs.GetString(PREF_IP, senderScript.pcIpAddress);
        int savedPort = PlayerPrefs.GetInt(PREF_PORT, senderScript.pcPort);

        // 2. 更新 UI 顯示
        ipInputField.text = savedIp;
        portInputField.text = savedPort.ToString();

        // 3. 立即應用一次 (確保程式開始時是用儲存的 IP)
        ApplySettings();

        // 4. 綁定按鈕事件
        applyButton.onClick.AddListener(ApplySettings);
    }

    public void ApplySettings()
    {
        string newIp = ipInputField.text;
        int newPort;

        // 嘗試解析 Port，如果輸入非數字則設回預設
        if (!int.TryParse(portInputField.text, out newPort))
        {
            newPort = 8080;
            portInputField.text = "8080";
        }

        // 更新 Sender
        if (senderScript != null)
        {
            senderScript.SetConnectionInfo(newIp, newPort);
        }

        // 儲存設定到手機，下次重開會記住
        PlayerPrefs.SetString(PREF_IP, newIp);
        PlayerPrefs.SetInt(PREF_PORT, newPort);
        PlayerPrefs.Save();
        
        // 選擇性功能：收起小鍵盤 (隱藏 TouchScreenKeyboard)
        ipInputField.DeactivateInputField();
        portInputField.DeactivateInputField();
    }
} 