using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine; // Unity'nin kütüphanesi
using UnityEngine.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elemanlarý")]
    public GameObject roomPanel;      // Odanýn paneli
    public TextMeshProUGUI playerListText; // Oyuncu isimlerini yazan text
    public GameObject startButton;    // Oyunu baþlatma butonu

    [Header("Ayarlar")]
    public string gameSceneName = "GameScene"; // Oyunun asýl sahnesinin adý

    void Start()
    {
        // Baþlangýç ayarlarý
    }

    // Odaya biz girdiðimizde
    public override void OnJoinedRoom()
    {
        Debug.Log(">>> SEN ODAYA GÝRDÝN! <<<");
        roomPanel.SetActive(true); // Paneli aç

        UpdatePlayerList(); // Listeyi güncelle

        // Sadece odayý kuran (Master Client) baþlat butonunu görsün
        startButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    // Odaya BAÞKA BÝRÝ girdiðinde çalýþýr
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // ÝSTEDÝÐÝN GÜNCELLEME BURADA:
        Debug.Log("!!! ODAYA BÝRÝ GÝRDÝ: " + newPlayer.NickName);

        UpdatePlayerList();
    }

    // Odadan BAÞKA BÝRÝ çýktýðýnda çalýþýr
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // ÝSTEDÝÐÝN GÜNCELLEME BURADA:
        Debug.Log("!!! ODADAN BÝRÝ ÇIKTI: " + otherPlayer.NickName);

        UpdatePlayerList();
    }

    // Oyuncu listesini ekrana yazdýran fonksiyon
    void UpdatePlayerList()
    {
        // Eðer UI çalýþmýyorsa bile konsola tam listeyi basalým ki gör
        string list = "";
        Debug.Log("--- GÜNCEL ODA LÝSTESÝ ---");
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            list += player.NickName + "\n";
            Debug.Log("- " + player.NickName); // Konsola da yazsýn
        }
        Debug.Log("--------------------------");

        if (playerListText != null)
        {
            playerListText.text = list;
        }
    }

    // Butona basýnca çalýþacak fonksiyon
    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Oyun Baþlatýlýyor...");
            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startButton.SetActive(PhotonNetwork.IsMasterClient);
    }
}