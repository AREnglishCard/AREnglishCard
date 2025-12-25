using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // Eðer TextMeshPro kullanýyorsan bunu ekle, yoksa sil.

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elemanlarý")]
    public TMP_InputField odaIsmiInput; // Input Field'ý buraya sürükleyeceksin
    public Button odaKurButonu;
    public Text durumYazisi; // Ekrana "Baðlanýyor..." yazdýrmak istersen

    void Start()
    {
        // Oyun açýlýnca sunucuya baðlan
        PhotonNetwork.ConnectUsingSettings();
        odaKurButonu.interactable = false; // Baðlanmadan butona basamasýnlar
        if (durumYazisi) durumYazisi.text = "Sunucuya baðlanýlýyor...";
    }

    // Sunucuya (Master Server) baðlanýnca çalýþýr
    public override void OnConnectedToMaster()
    {
        Debug.Log("Server'a gelindi, Lobiye giriliyor...");
        PhotonNetwork.JoinLobby();
    }

    // Lobiye girince çalýþýr
    public override void OnJoinedLobby()
    {
        Debug.Log("Lobiye girildi.");
        odaKurButonu.interactable = true; // Artýk oda kurabilir
        if (durumYazisi) durumYazisi.text = "Lobiye Hoþgeldin!";
    }

    // BUTONA ATAYACAÐIN FONKSÝYON BU
    public void OdaKur()
    {
        // Eðer input boþsa rastgele bir isim verelim ki hata vermesin
        string odaAdi = odaIsmiInput.text;
        if (string.IsNullOrEmpty(odaAdi))
        {
            odaAdi = "Oda " + Random.Range(1000, 9999);
        }

        // Oda ayarlarý (Örn: Maksimum 4 kiþilik)
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom(odaAdi, roomOptions);
    }

    // Oda baþarýyla kurulup içine girilince çalýþýr
    public override void OnJoinedRoom()
    {
        Debug.Log("Odaya girildi! Þimdi oyun sahnesini yükleyebilirsin.");
        // PhotonNetwork.LoadLevel("OyunSahnesi"); // Sahne adýn neyse buraya yaz
    }

    // Oda kurma baþarýsýz olursa (Ayný isimde oda varsa vb.)
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Oda kurulamadý: " + message);
    }
}