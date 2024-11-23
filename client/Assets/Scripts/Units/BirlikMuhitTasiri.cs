using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BirlikMuhitTasiri : MonoBehaviour
{
    [Header("Muhit Ta'siri")]
    public float shamolTasiriKoeffitsienti = 1f;
    public float yerTasiriKoeffitsienti = 1f;
    
    [Header("Tovush Parametrlari")]
    public float tovushMasofa = 1f;
    public float tovushKutishVaqti = 0.5f;
    
    private Rigidbody rb;
    private ObHavoTizimi obHavo;
    private YerMuhitTizimi yerMuhiti;
    private MuhitTovushlari muhitTovushlari;
    
    private Vector3 asosiyTezlik;
    private float asosiyBurilishTezligi;
    private float oxirgiTovushVaqti;
    private Vector3 oxirgiPozitsiya;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        obHavo = FindObjectOfType<ObHavoTizimi>();
        yerMuhiti = FindObjectOfType<YerMuhitTizimi>();
        muhitTovushlari = FindObjectOfType<MuhitTovushlari>();
        
        // Asosiy tezliklarni saqlash
        asosiyTezlik = rb.velocity;
        asosiyBurilishTezligi = rb.angularVelocity.magnitude;
        oxirgiPozitsiya = transform.position;
    }
    
    private void FixedUpdate()
    {
        if (obHavo != null)
        {
            ShamolTasiriniQollash();
        }
        
        if (yerMuhiti != null)
        {
            YerTasiriniQollash();
        }
        
        // Harakat tovushini tekshirish
        HarakatTovushiniTekshirish();
    }
    
    private void ShamolTasiriniQollash()
    {
        // Shamol kuchini hisoblash
        Vector3 shamolKuchi = obHavo.ShamolYonalishi * obHavo.ShamolTezligi * shamolTasiriKoeffitsienti;
        
        // Birlik og'irligiga qarab shamol ta'sirini moslash
        shamolKuchi *= Mathf.Clamp01(10f / rb.mass);
        
        // Shamol kuchini qo'llash
        rb.AddForce(shamolKuchi, ForceMode.Force);
    }
    
    private void YerTasiriniQollash()
    {
        float tezlikKoeffitsienti = 1f;
        float burilishKoeffitsienti = 1f;
        
        // Yer holatiga qarab koeffitsientlarni aniqlash
        switch (yerMuhiti.JoriyHolat)
        {
            case YerHolati.Nam:
                tezlikKoeffitsienti = 0.8f;
                burilishKoeffitsienti = 0.9f;
                break;
                
            case YerHolati.Qorli:
                tezlikKoeffitsienti = 0.7f;
                burilishKoeffitsienti = 0.8f;
                break;
                
            case YerHolati.Muzli:
                tezlikKoeffitsienti = 0.6f;
                burilishKoeffitsienti = 0.5f;
                
                // Muzda sirpanish effekti
                if (rb.velocity.magnitude > 0.1f)
                {
                    Vector3 sirpanishYonalishi = rb.velocity.normalized;
                    rb.AddForce(sirpanishYonalishi * 2f, ForceMode.Force);
                }
                break;
                
            case YerHolati.Loyqa:
                tezlikKoeffitsienti = 0.5f;
                burilishKoeffitsienti = 0.7f;
                
                // Loyda botish effekti
                if (!rb.useGravity)
                {
                    rb.AddForce(Vector3.down * yerMuhiti.HolatIntensivligi, ForceMode.Force);
                }
                break;
        }
        
        // Koeffitsientlarni birlik xususiyatlariga moslash
        tezlikKoeffitsienti = Mathf.Lerp(1f, tezlikKoeffitsienti, yerTasiriKoeffitsienti);
        burilishKoeffitsienti = Mathf.Lerp(1f, burilishKoeffitsienti, yerTasiriKoeffitsienti);
        
        // Tezlik va burilishni yangilash
        rb.velocity = asosiyTezlik * tezlikKoeffitsienti;
        rb.angularVelocity = rb.angularVelocity.normalized * (asosiyBurilishTezligi * burilishKoeffitsienti);
    }
    
    private void HarakatTovushiniTekshirish()
    {
        if (muhitTovushlari == null || yerMuhiti == null) return;
        
        // Harakat masofasini hisoblash
        float harakatMasofa = Vector3.Distance(transform.position, oxirgiPozitsiya);
        
        // Agar yetarli masofa bosib o'tilgan bo'lsa va kutish vaqti o'tgan bo'lsa
        if (harakatMasofa >= tovushMasofa && Time.time - oxirgiTovushVaqti >= tovushKutishVaqti)
        {
            // Yer tovushini ijro etish
            muhitTovushlari.YerTovushiniIjroEtish(transform.position);
            
            // Vaqt va pozitsiyani yangilash
            oxirgiTovushVaqti = Time.time;
            oxirgiPozitsiya = transform.position;
        }
    }
    
    // Asosiy tezliklarni yangilash
    public void AsosiyTezliklarniYangilash(Vector3 yangiTezlik, float yangiBurilishTezligi)
    {
        asosiyTezlik = yangiTezlik;
        asosiyBurilishTezligi = yangiBurilishTezligi;
    }
}
