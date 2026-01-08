using UnityEngine;

public class SpeakOnTarget : MonoBehaviour
{
    public string kelime; // Inspector'da buraya "cat" yazmalýsýn
    public string cumle;

    // Vuforia bu fonksiyonu ImageTarget bulunduðunda tetikler (Event Trigger ile)
    public void SpeakNow()
    {
        // 1. Sesli okuma sistemini hazýrla (Eski kodun)
        if (TTSManager.Instance != null)
        {
            TTSManager.Instance.KartTanimlaVeOku(kelime, cumle);
        }

        if (SpeechManager.Instance != null)
        {
            SpeechManager.Instance.HedefGuncelle(kelime, cumle);
        }

        // 2. GameManager'a "Kartý Buldum" de ve puaný kap (YENÝ KISIM)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCardDetected(kelime);
        }
    }
}