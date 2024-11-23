using UnityEngine;

public class ObHavoEffektlari : MonoBehaviour
{
    [Header("Yomg'ir Effekti")]
    public ParticleSystem yomgirEffekti;
    public float yomgirIntensivligi = 1000f;
    public float yomgirTomchiOlchami = 0.1f;
    
    [Header("Qor Effekti")]
    public ParticleSystem qorEffekti;
    public float qorIntensivligi = 500f;
    public float qorZarraOlchami = 0.2f;
    
    [Header("Shamol Effekti")]
    public ParticleSystem shamolEffekti;
    public float shamolIntensivligi = 100f;
    public float shamolZarraOlchami = 0.5f;
    
    private ObHavoTizimi obHavo;
    private FaslTizimi fasl;
    
    private void Start()
    {
        obHavo = FindObjectOfType<ObHavoTizimi>();
        fasl = FindObjectOfType<FaslTizimi>();
        
        if (obHavo == null || fasl == null)
        {
            Debug.LogWarning("ObHavoTizimi yoki FaslTizimi topilmadi!");
            return;
        }
        
        // Effektlarni boshlang'ich holatda o'chirish
        EffektlarniOchirish();
    }
    
    private void Update()
    {
        if (obHavo == null || fasl == null) return;
        
        // Ob-havo holatiga qarab effektlarni yangilash
        ObHavoEffektlariniYangilash();
    }
    
    private void ObHavoEffektlariniYangilash()
    {
        // Yomg'ir effekti
        if (obHavo.YogingarchlikDarajasi > 0.3f && fasl.JoriyFasl != Fasl.Qish)
        {
            YomgirEffektiniYangilash();
        }
        else
        {
            yomgirEffekti?.Stop();
        }
        
        // Qor effekti
        if (obHavo.YogingarchlikDarajasi > 0.3f && fasl.JoriyFasl == Fasl.Qish)
        {
            QorEffektiniYangilash();
        }
        else
        {
            qorEffekti?.Stop();
        }
        
        // Shamol effekti
        if (obHavo.ShamolTezligi > 2f)
        {
            ShamolEffektiniYangilash();
        }
        else
        {
            shamolEffekti?.Stop();
        }
    }
    
    private void YomgirEffektiniYangilash()
    {
        if (yomgirEffekti != null)
        {
            var emission = yomgirEffekti.emission;
            emission.rateOverTime = yomgirIntensivligi * obHavo.YogingarchlikDarajasi;
            
            var main = yomgirEffekti.main;
            main.startSize = yomgirTomchiOlchami;
            
            if (!yomgirEffekti.isPlaying)
            {
                yomgirEffekti.Play();
            }
        }
    }
    
    private void QorEffektiniYangilash()
    {
        if (qorEffekti != null)
        {
            var emission = qorEffekti.emission;
            emission.rateOverTime = qorIntensivligi * obHavo.YogingarchlikDarajasi;
            
            var main = qorEffekti.main;
            main.startSize = qorZarraOlchami;
            
            // Shamol ta'sirini qo'shish
            var velocityOverLifetime = qorEffekti.velocityOverLifetime;
            velocityOverLifetime.x = obHavo.ShamolYonalishi.x * obHavo.ShamolTezligi;
            velocityOverLifetime.z = obHavo.ShamolYonalishi.z * obHavo.ShamolTezligi;
            
            if (!qorEffekti.isPlaying)
            {
                qorEffekti.Play();
            }
        }
    }
    
    private void ShamolEffektiniYangilash()
    {
        if (shamolEffekti != null)
        {
            var emission = shamolEffekti.emission;
            emission.rateOverTime = shamolIntensivligi * (obHavo.ShamolTezligi / 10f);
            
            var main = shamolEffekti.main;
            main.startSize = shamolZarraOlchami;
            
            var velocityOverLifetime = shamolEffekti.velocityOverLifetime;
            velocityOverLifetime.x = obHavo.ShamolYonalishi.x * obHavo.ShamolTezligi * 2f;
            velocityOverLifetime.z = obHavo.ShamolYonalishi.z * obHavo.ShamolTezligi * 2f;
            
            if (!shamolEffekti.isPlaying)
            {
                shamolEffekti.Play();
            }
        }
    }
    
    private void EffektlarniOchirish()
    {
        yomgirEffekti?.Stop();
        qorEffekti?.Stop();
        shamolEffekti?.Stop();
    }
    
    private void OnDisable()
    {
        EffektlarniOchirish();
    }
}
