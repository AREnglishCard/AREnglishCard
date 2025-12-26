using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Týklama olaylarýný yakalamak için kütüphane

// Bu scripti attýðýn objede otomatik olarak Image ve AudioSource arar, yoksa ekler.
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(AudioSource))]
public class CustomButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Görsel Ayarlarý")]
    public Sprite defaultSprite; // Butonun normal hali (Basýlmamýþ)
    public Sprite pressedSprite; // Butonun basýlmýþ hali (Koyu/Küçük olan)

    [Header("Ses Ayarlarý")]
    public AudioClip clickSound; // O bulduðun tatlý ses dosyasý
    [Range(0f, 1f)] public float volume = 1f; // Ses þiddeti

    private Image _buttonImage;
    private AudioSource _audioSource;

    void Awake()
    {
        // Bileþenleri otomatik tanýtalým
        _buttonImage = GetComponent<Image>();
        _audioSource = GetComponent<AudioSource>();

        // Ses ayarlarýný yapalým (Play On Awake kapalý olsun ki oyun açýlýnca çalmasýn)
        _audioSource.playOnAwake = false;

        // Baþlangýçta butonun kendi sprite'ýný default olarak ayarla
        if (defaultSprite == null)
            defaultSprite = _buttonImage.sprite;
    }

    // Kullanýcý butona bastýðý an (Mouse týklamasý veya parmak dokunuþu)
    public void OnPointerDown(PointerEventData eventData)
    {
        // Görseli deðiþtir
        if (pressedSprite != null)
            _buttonImage.sprite = pressedSprite;

        // Sesi çal
        if (clickSound != null)
            _audioSource.PlayOneShot(clickSound, volume);
    }

    // Kullanýcý butondan elini çektiði an
    public void OnPointerUp(PointerEventData eventData)
    {
        // Görseli eski haline getir
        if (defaultSprite != null)
            _buttonImage.sprite = defaultSprite;
    }
}