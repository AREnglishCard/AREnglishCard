using UnityEngine;
using UnityEngine.UI;
using Photon.Pun; // Photon kütüphanesi

public class RoomItem : MonoBehaviour
{
    public Text roomNameText; // Prefabýn içindeki Oda Adý yazýsý
    private string currentRoomName; // Odanýn ismini hafýzada tutacaðýz

    // NetworkManager bu fonksiyonu çaðýrýp odanýn adýný buraya yollayacak
    public void SetRoomName(string _roomName)
    {
        currentRoomName = _roomName;
        roomNameText.text = _roomName; // Ekranda odanýn adýný yazar
    }

    // "Katýl" butonuna bu fonksiyonu baðlayacaðýz
    public void JoinRoom()
    {
        Debug.Log(currentRoomName + " odasýna katýlýnýyor...");
        PhotonNetwork.JoinRoom(currentRoomName);
    }
}