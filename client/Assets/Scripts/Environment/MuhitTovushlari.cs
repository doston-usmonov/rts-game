using UnityEngine;
using System.Collections.Generic;

public class MuhitTovushlari : MonoBehaviour
{
    [System.Serializable]
    public class TovushManbasi
    {
        public AudioSource audioSource;
        public float maksimalOvoz = 1f;
        public float minimal3DOvoz = 0.1f;
        public float maksimal3DMasofa = 50f;
    }
    
    [Header("Ob-havo Tovushlari")]
    public TovushManbasi yomgirTovushi;
    public TovushManbasi qorTovushi;
    public TovushManbasi shamolTovushi;
    public TovushManbasi momaqaldiroqTovushi;
    
    [Header("Yer Tovushlari")]
    public List<AudioClip> namYerTovushlari;
    public List<AudioClip> qorliYerTovushlari;
    public List<AudioClip> muzliYerTovushlari;
    public List<AudioClip> loyqaYerTovushlari;
    
    private ObHavoTizimi obHavo;
    private YerMuhitTizimi yerMuhiti;
    private Dictionary<YerHolati, List<AudioClip>> yerTovushlari;
    
    private void Start()
    {
        obHavo = FindObjectOfType<ObHavoTizimi>();
        yerMuhiti = FindObjectOfType<YerMuhitTizimi>();
        
        TovushManbalariniSozlash();
        YerTovushlariniSozlash();
    }
    
    private void Update()
    {
        if (obHavo != null)
        {
            ObHavoTovushlariniYangilash();
        }
    }
    
    private void TovushManbalariniSozlash()
    {
        // Yomg'ir tovushi
        if (yomgirTovushi.audioSource != null)
        {
            yomgirTovushi.audioSource.loop = true;
            yomgirTovushi.audioSource.spatialBlend = 1f; // 3D tovush
            yomgirTovushi.audioSource.minDistance = 1f;
            yomgirTovushi.audioSource.maxDistance = yomgirTovushi.maksimal3DMasofa;
            yomgirTovushi.audioSource.volume = 0f;
        }
        
        // Qor tovushi
        if (qorTovushi.audioSource != null)
        {
            qorTovushi.audioSource.loop = true;
            qorTovushi.audioSource.spatialBlend = 1f;
            qorTovushi.audioSource.minDistance = 1f;
            qorTovushi.audioSource.maxDistance = qorTovushi.maksimal3DMasofa;
            qorTovushi.audioSource.volume = 0f;
        }
        
        // Shamol tovushi
        if (shamolTovushi.audioSource != null)
        {
            shamolTovushi.audioSource.loop = true;
            shamolTovushi.audioSource.spatialBlend = 1f;
            shamolTovushi.audioSource.minDistance = 1f;
            shamolTovushi.audioSource.maxDistance = shamolTovushi.maksimal3DMasofa;
            shamolTovushi.audioSource.volume = 0f;
        }
        
        // Momaqaldiroq tovushi
        if (momaqaldiroqTovushi.audioSource != null)
        {
            momaqaldiroqTovushi.audioSource.loop = false;
            momaqaldiroqTovushi.audioSource.spatialBlend = 0f; // 2D tovush
            momaqaldiroqTovushi.audioSource.volume = 0f;
        }
    }
    
    private void YerTovushlariniSozlash()
    {
        yerTovushlari = new Dictionary<YerHolati, List<AudioClip>>
        {
            { YerHolati.Nam, namYerTovushlari },
            { YerHolati.Qorli, qorliYerTovushlari },
            { YerHolati.Muzli, muzliYerTovushlari },
            { YerHolati.Loyqa, loyqaYerTovushlari }
        };
    }
    
    private void ObHavoTovushlariniYangilash()
    {
        // Yomg'ir tovushini yangilash
        if (yomgirTovushi.audioSource != null)
        {
            float yomgirIntensivligi = obHavo.YogingarchlikDarajasi;
            float yomgirOvozi = Mathf.Lerp(0f, yomgirTovushi.maksimalOvoz, yomgirIntensivligi);
            yomgirTovushi.audioSource.volume = yomgirOvozi;
            
            if (yomgirOvozi > 0 && !yomgirTovushi.audioSource.isPlaying)
            {
                yomgirTovushi.audioSource.Play();
            }
            else if (yomgirOvozi <= 0 && yomgirTovushi.audioSource.isPlaying)
            {
                yomgirTovushi.audioSource.Stop();
            }
        }
        
        // Shamol tovushini yangilash
        if (shamolTovushi.audioSource != null)
        {
            float shamolIntensivligi = obHavo.ShamolTezligi / 10f;
            float shamolOvozi = Mathf.Lerp(0f, shamolTovushi.maksimalOvoz, shamolIntensivligi);
            shamolTovushi.audioSource.volume = shamolOvozi;
            
            if (shamolOvozi > 0 && !shamolTovushi.audioSource.isPlaying)
            {
                shamolTovushi.audioSource.Play();
            }
            else if (shamolOvozi <= 0 && shamolTovushi.audioSource.isPlaying)
            {
                shamolTovushi.audioSource.Stop();
            }
        }
    }
    
    // Yer tovushini ijro etish
    public void YerTovushiniIjroEtish(Vector3 pozitsiya)
    {
        if (yerMuhiti == null || yerTovushlari == null) return;
        
        YerHolati joriyHolat = yerMuhiti.JoriyHolat;
        if (joriyHolat == YerHolati.Normal) return;
        
        if (yerTovushlari.TryGetValue(joriyHolat, out List<AudioClip> tovushlar) && 
            tovushlar != null && tovushlar.Count > 0)
        {
            // Tasodifiy tovushni tanlash
            AudioClip tovush = tovushlar[Random.Range(0, tovushlar.Count)];
            
            // Tovushni ijro etish
            AudioSource.PlayClipAtPoint(tovush, pozitsiya);
        }
    }
    
    // Momaqaldiroq tovushini ijro etish
    public void MomaqaldiroqniIjroEtish()
    {
        if (momaqaldiroqTovushi.audioSource != null && !momaqaldiroqTovushi.audioSource.isPlaying)
        {
            momaqaldiroqTovushi.audioSource.volume = momaqaldiroqTovushi.maksimalOvoz;
            momaqaldiroqTovushi.audioSource.Play();
        }
    }
}
