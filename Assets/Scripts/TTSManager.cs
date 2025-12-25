using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance;
    public AudioSource audioSource;

    // HAFIZA (Karttaki bilgileri burada tutuyoruz)
    private string hafizaKelime = "";
    private string hafizaCumle = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // GÜNCELLEME: Artık bu fonksiyon sadece bilgileri kaydediyor, SES ÇIKARMIYOR.
    public void KartTanimlaVeOku(string kelime, string cumle)
    {
        hafizaKelime = kelime;
        hafizaCumle = cumle;
        
        // Buradaki "Speak(...)" komutunu sildik. 
        // Artık kartı görünce otomatik konuşmayacak.
    }

    public void SadeceKelimeyiOku()
    {
        // Butona basılınca hafızadaki kelimeyi okur
        if (!string.IsNullOrEmpty(hafizaKelime)) Speak(hafizaKelime);
    }

    public void SadeceCumleyiOku()
    {
        // Butona basılınca hafızadaki cümleyi okur
        if (!string.IsNullOrEmpty(hafizaCumle)) Speak(hafizaCumle);
    }

    public void Speak(string text)
    {
        StartCoroutine(SesIndirVeCal(text));
    }

    IEnumerator SesIndirVeCal(string text)
    {
        string url = "https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&q=" + text + "&tl=en";
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
    }
}