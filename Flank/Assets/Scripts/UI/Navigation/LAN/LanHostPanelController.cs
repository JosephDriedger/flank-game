using System.Net.NetworkInformation;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LanHostPanelController : MonoBehaviour
{
    [SerializeField] private PanelManager panelManager;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button startHostingButton;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField ipAddressInput;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private TMP_InputField hostPlayerNameInput;

    [Header("Targets")]
    [SerializeField] private GameObject lanMenuPanel;
    [SerializeField] private GameObject lanLobbyPanel;

    private void Awake()
    {
        if (panelManager == null)
        {
            panelManager = FindFirstObjectByType<PanelManager>();
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(() =>
            {
                if (panelManager != null && lanMenuPanel != null)
                {
                    panelManager.ShowPanel(lanMenuPanel);
                }
            });
        }

        if (startHostingButton != null)
        {
            startHostingButton.onClick.AddListener(this.HandleStartHost);
        }
    }

    private void OnEnable()
    {
        string localIp = GetLocalLanIPv4OrLoopback();
        if (ipAddressInput != null)
        {
            ipAddressInput.text = localIp;
        }

        if (portInput != null && string.IsNullOrWhiteSpace(portInput.text))
        {
            portInput.text = LanSessionConfig.HostPort.ToString();
        }

        if (roomNameInput != null && string.IsNullOrWhiteSpace(roomNameInput.text))
        {
            roomNameInput.text = LanSessionConfig.RoomName;
        }

        if (hostPlayerNameInput != null && string.IsNullOrWhiteSpace(hostPlayerNameInput.text))
        {
            hostPlayerNameInput.text = LanSessionConfig.HostPlayerName;
        }
    }

    private void HandleStartHost()
    {
        LanSessionConfig.RoomName = this.GetTextOrDefault(roomNameInput, "Room");
        LanSessionConfig.HostPlayerName = this.GetTextOrDefault(hostPlayerNameInput, "Host");
        LanSessionConfig.HostIpAddress = this.GetTextOrDefault(ipAddressInput, GetLocalLanIPv4OrLoopback());
        LanSessionConfig.HostPort = this.ParsePortOrDefault(portInput, LanSessionConfig.DefaultPort);

        if (LanNetworkService.Instance == null)
        {
            Debug.LogError("LanNetworkService.Instance is null. Add LanNetworkService on a persistent NetworkManager object.");
            return;
        }

        bool started = LanNetworkService.Instance.StartHost(LanSessionConfig.HostPort);
        if (!started)
        {
            return;
        }

        // Ensure the UI reflects the actual host port we used.
        if (portInput != null)
        {
            portInput.text = LanSessionConfig.HostPort.ToString();
        }

        if (panelManager != null && lanLobbyPanel != null)
        {
            panelManager.ShowPanel(lanLobbyPanel);
        }
    }

    private string GetTextOrDefault(TMP_InputField field, string fallback)
    {
        if (field == null)
        {
            return fallback;
        }

        string text = field.text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        return text;
    }

    private ushort ParsePortOrDefault(TMP_InputField field, ushort fallback)
    {
        if (field == null)
        {
            return fallback;
        }

        if (ushort.TryParse(field.text, out ushort port))
        {
            return port;
        }

        return fallback;
    }

    private static string GetLocalLanIPv4OrLoopback()
    {
        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    string ip = addr.Address.ToString();
                    if (ip.StartsWith("169.254."))
                    {
                        continue;
                    }

                    return ip;
                }
            }
        }
        catch
        {
        }

        return "127.0.0.1";
    }
}
