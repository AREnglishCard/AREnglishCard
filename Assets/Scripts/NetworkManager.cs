using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine; // Unity'nin kütüphanesi
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("--- UI Elemanlarý ---")]
    public TMP_InputField roomNameInput;
    public Button createButton;
    public Text statusText;

    [Header("--- Oda Listesi Ayarlarý ---")]
    public Transform contentObject;
    public GameObject roomItemPrefab;
    public GameObject lobbyPanel; // Lobi paneli (Odaya girince kapanacak)

    void Start()
    {
        // ---> ÝÞTE EKSÝK OLAN SÝHÝRLÝ SATIR BU <---
        // Bu true olmazsa, LoadLevel yapýnca sadece host gider, diðerleri kalýr.
        PhotonNetwork.AutomaticallySyncScene = true;

        PhotonNetwork.ConnectUsingSettings();
        statusText.text = "Sunucuya baðlanýlýyor...";

        // Baþlangýçta butonu kapatalým, baðlanýnca açarýz
        if (createButton != null) createButton.interactable = false;
    }

    public override void OnConnectedToMaster()
    {
        UnityEngine.Debug.Log("Server'a gelindi, Lobiye giriliyor..."); // DÜZELTÝLDÝ
        PhotonNetwork.JoinLobby();
        statusText.text = "Lobiye giriliyor...";
    }

    public override void OnJoinedLobby()
    {
        UnityEngine.Debug.Log("Lobiye Girildi."); // DÜZELTÝLDÝ
        statusText.text = "Lobiye Hoþgeldin!";
        if (createButton != null) createButton.interactable = true;
    }

    // --- BUTONA BASINCA BU ÇALIÞACAK ---
    public void CreateRoom()
    {
        string odaAdi = roomNameInput.text;

        // Ýsim boþsa rastgele sayý ver
        if (string.IsNullOrEmpty(odaAdi))
        {
            // BURAYI DÜZELTTÝM: UnityEngine.Random kullandýk
            odaAdi = "Oda " + UnityEngine.Random.Range(1000, 9999);
        }

        RoomOptions options = new RoomOptions { MaxPlayers = 2, IsOpen = true, IsVisible = true };
        PhotonNetwork.CreateRoom(odaAdi, options);

        statusText.text = "Oda kuruluyor: " + odaAdi;
        UnityEngine.Debug.Log(odaAdi + " kuruluyor..."); // DÜZELTÝLDÝ
    }

    public override void OnJoinedRoom()
    {
        statusText.text = "Odaya Girildi: " + PhotonNetwork.CurrentRoom.Name;

        // Odaya girince Lobi ekranýný kapat ki diðer ekran (RoomManager) gözüksün
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform child in contentObject)
        {
            Destroy(child.gameObject);
        }

        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList || !room.IsOpen || !room.IsVisible) continue;

            GameObject newRow = Instantiate(roomItemPrefab, contentObject);
            newRow.GetComponent<RoomItem>().SetRoomName(room.Name);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Hata: " + message;
        UnityEngine.Debug.LogError("Oda kurulamadý: " + message); // DÜZELTÝLDÝ
    }
}