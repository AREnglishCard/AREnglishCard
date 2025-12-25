using UnityEngine;

public class SpeakOnTarget : MonoBehaviour
{
    public string kelime;
    public string cumle;

    public void SpeakNow()
    {
        if (TTSManager.Instance != null)
        {
            TTSManager.Instance.KartTanimlaVeOku(kelime, cumle);
        }

        if (SpeechManager.Instance != null)
        {
            SpeechManager.Instance.HedefGuncelle(kelime, cumle);
        }
    }
}