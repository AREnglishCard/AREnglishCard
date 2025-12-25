using UnityEngine;
using System.Collections; // Zıplama zamanlaması için gerekli

public class ARObjectController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float donusHizi = 100.0f;
    public float buyutmeHizi = 2.0f;
    public float minBoyut = 0.1f;    
    public float maxBoyut = 5.0f;    

    [Header("Tap (Tıklama) Ayarları")]
    public float ziplamaGucu = 0.5f; // Ne kadar yükseğe zıplasın

    // Değişkenler
    private Vector3 sonMousePozisyonu;
    private bool surukleniyorMu = false;
    
    // Zıplama için gerekenler
    private Vector3 baslangicPozisyonu;
    private bool efektOynuyorMu = false;

    void Start()
    {
        // Objenin ilk konumunu hafızaya al (Zıplayıp buraya geri dönecek)
        baslangicPozisyonu = transform.localPosition;
    }

    void Update()
    {
        // --- 1. FREE ROTATE (HER YÖNE DÖNDÜRME) ---
        if (Input.GetMouseButtonDown(0))
        {
            sonMousePozisyonu = Input.mousePosition;
            surukleniyorMu = false;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - sonMousePozisyonu;

            if (delta.magnitude > 2f) 
            {
                surukleniyorMu = true;
                
                float rotY = -delta.x * donusHizi * Time.deltaTime;
                float rotX = delta.y * donusHizi * Time.deltaTime;

                transform.Rotate(rotX, rotY, 0, Space.World);
            }
            
            sonMousePozisyonu = Input.mousePosition;
        }

        // --- 2. SCALE (BÜYÜTME) ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 yeniBoyut = transform.localScale + Vector3.one * scroll * buyutmeHizi;

            yeniBoyut.x = Mathf.Clamp(yeniBoyut.x, minBoyut, maxBoyut);
            yeniBoyut.y = Mathf.Clamp(yeniBoyut.y, minBoyut, maxBoyut);
            yeniBoyut.z = Mathf.Clamp(yeniBoyut.z, minBoyut, maxBoyut);

            transform.localScale = yeniBoyut;
        }

        // --- 3. TAP (TIKLAMA & ZIPLAMA) ---
        if (Input.GetMouseButtonUp(0) && !surukleniyorMu)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Tıklanan obje bu scriptin bağlı olduğu obje mi?
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    // Eğer şu an zaten zıplamıyorsa -> Zıplat
                    if (!efektOynuyorMu) StartCoroutine(ZiplamaEfekti());
                    
                    // Sesi Oku
                    TetikleSes();
                }
            }
        }
    }

    // --- ZIPLAMA ANİMASYONU (KOD İLE) ---
    IEnumerator ZiplamaEfekti()
    {
        efektOynuyorMu = true;
        
        float sure = 0.2f; // Çıkış süresi
        float gecenSure = 0;
        
        // Mevcut pozisyonu referans al (kullanıcı hareket ettirmiş olabilir diye güncellemiyoruz, local kullanıyoruz)
        // Ancak zıplama her zaman 'yukarı' olsun diye localPosition.y'yi artırıyoruz.
        Vector3 hedefYukseklik = baslangicPozisyonu + Vector3.up * ziplamaGucu;

        // Yukarı Çık
        while (gecenSure < sure)
        {
            transform.localPosition = Vector3.Lerp(baslangicPozisyonu, hedefYukseklik, (gecenSure / sure));
            gecenSure += Time.deltaTime;
            yield return null;
        }

        // Aşağı İn
        gecenSure = 0;
        while (gecenSure < sure)
        {
            transform.localPosition = Vector3.Lerp(hedefYukseklik, baslangicPozisyonu, (gecenSure / sure));
            gecenSure += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = baslangicPozisyonu; // Tam yerine oturt
        efektOynuyorMu = false;
    }

    void TetikleSes()
    {
        if (TTSManager.Instance != null)
        {
            var speakScript = GetComponentInParent<SpeakOnTarget>();
            if (speakScript != null)
            {
                TTSManager.Instance.Speak(speakScript.kelime);
            }
        }
    }
}