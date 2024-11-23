using UnityEngine;
using System.Collections.Generic;

public class SpecializedFormation : MonoBehaviour
{
    // Formatsiya parametrlari
    public float FormatsiyaKengligi = 10f;
    public float FormatsiyaChuqurligi = 5f;
    public float BirlikOraligʻi = 2f;
    
    // Yer holati ta'siri
    private YerMuhitTizimi yerMuhiti;
    private float harakatTezligiKoeffitsienti = 1f;
    private float burilishTezligiKoeffitsienti = 1f;
    
    // Formatsiya tarkibidagi birliklar
    private List<GameObject> formatsiyaBirliklari = new List<GameObject>();
    
    private void Start()
    {
        // Yer muhiti tizimini olish
        yerMuhiti = FindObjectOfType<YerMuhitTizimi>();
        
        if (yerMuhiti == null)
        {
            Debug.LogWarning("YerMuhitTizimi topilmadi!");
        }
    }
    
    private void Update()
    {
        if (yerMuhiti != null)
        {
            // Yer holatiga qarab harakat parametrlarini yangilash
            YerHolatiTasiriniYangilash();
        }
        
        // Formatsiya harakatini yangilash
        FormatsiyaniYangilash();
    }
    
    private void YerHolatiTasiriniYangilash()
    {
        switch (yerMuhiti.JoriyHolat)
        {
            case YerHolati.Nam:
                harakatTezligiKoeffitsienti = Mathf.Lerp(1f, 0.8f, yerMuhiti.HolatIntensivligi);
                burilishTezligiKoeffitsienti = Mathf.Lerp(1f, 0.9f, yerMuhiti.HolatIntensivligi);
                break;
                
            case YerHolati.Qorli:
                harakatTezligiKoeffitsienti = Mathf.Lerp(1f, 0.7f, yerMuhiti.HolatIntensivligi);
                burilishTezligiKoeffitsienti = Mathf.Lerp(1f, 0.8f, yerMuhiti.HolatIntensivligi);
                break;
                
            case YerHolati.Muzli:
                harakatTezligiKoeffitsienti = Mathf.Lerp(1f, 0.6f, yerMuhiti.HolatIntensivligi);
                burilishTezligiKoeffitsienti = Mathf.Lerp(1f, 0.5f, yerMuhiti.HolatIntensivligi);
                break;
                
            case YerHolati.Loyqa:
                harakatTezligiKoeffitsienti = Mathf.Lerp(1f, 0.5f, yerMuhiti.HolatIntensivligi);
                burilishTezligiKoeffitsienti = Mathf.Lerp(1f, 0.7f, yerMuhiti.HolatIntensivligi);
                break;
                
            default:
                harakatTezligiKoeffitsienti = 1f;
                burilishTezligiKoeffitsienti = 1f;
                break;
        }
    }
    
    private void FormatsiyaniYangilash()
    {
        // Har bir birlik uchun
        for (int i = 0; i < formatsiyaBirliklari.Count; i++)
        {
            GameObject birlik = formatsiyaBirliklari[i];
            if (birlik != null)
            {
                // Birlik harakatini yer holatiga moslash
                if (birlik.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    // Tezlikni moslash
                    rb.velocity *= harakatTezligiKoeffitsienti;
                    
                    // Burilish tezligini moslash
                    float burilishKuchi = rb.angularVelocity.magnitude;
                    rb.angularVelocity = rb.angularVelocity.normalized * 
                        (burilishKuchi * burilishTezligiKoeffitsienti);
                }
            }
        }
    }
    
    // Formatsiyaga birlik qo'shish
    public void BirlikQoshish(GameObject birlik)
    {
        if (!formatsiyaBirliklari.Contains(birlik))
        {
            formatsiyaBirliklari.Add(birlik);
            FormatsiyaniQaytaTuzish();
        }
    }
    
    // Formatsiyadan birlik olib tashlash
    public void BirlikOlibTashlash(GameObject birlik)
    {
        if (formatsiyaBirliklari.Remove(birlik))
        {
            FormatsiyaniQaytaTuzish();
        }
    }
    
    // Formatsiyani qayta tuzish
    private void FormatsiyaniQaytaTuzish()
    {
        int birliklarSoni = formatsiyaBirliklari.Count;
        if (birliklarSoni == 0) return;
        
        // Formatsiya o'lchamlarini hisoblash
        int qatorlarSoni = Mathf.CeilToInt(Mathf.Sqrt(birliklarSoni));
        int ustunlarSoni = Mathf.CeilToInt((float)birliklarSoni / qatorlarSoni);
        
        // Har bir birlik uchun pozitsiyani hisoblash
        Vector3 formatsiyaMarkazi = transform.position;
        
        for (int i = 0; i < birliklarSoni; i++)
        {
            int qator = i / ustunlarSoni;
            int ustun = i % ustunlarSoni;
            
            // Birlik pozitsiyasini hisoblash
            Vector3 birlikPozitsiyasi = formatsiyaMarkazi + new Vector3(
                (ustun - (ustunlarSoni - 1) * 0.5f) * BirlikOraligʻi,
                0f,
                (qator - (qatorlarSoni - 1) * 0.5f) * BirlikOraligʻi
            );
            
            // Birlikni yangi pozitsiyaga ko'chirish
            GameObject birlik = formatsiyaBirliklari[i];
            if (birlik != null)
            {
                birlik.transform.position = birlikPozitsiyasi;
            }
        }
    }
}
