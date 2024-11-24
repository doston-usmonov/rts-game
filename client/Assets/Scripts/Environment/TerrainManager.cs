using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    private YerTeksturaTizimi yerTekstura;
    private YerMuhitTizimi yerMuhiti;
    
    private void Start()
    {
        yerTekstura = new YerTeksturaTizimi();
        yerMuhiti = GetComponent<YerMuhitTizimi>();
        
        if (yerMuhiti == null)
        {
            yerMuhiti = gameObject.AddComponent<YerMuhitTizimi>();
        }
        
        // Yer tekstura tizimini sozlash
        YerTeksturaTiziminiSozlash();
    }
    
    private void Update()
    {
        // Yer teksturalarini yangilash
        YerTeksturalariniYangilash();
    }
    
    [System.Serializable]
    public class YerTeksturaTizimi
    {
        // Asosiy teksturalar
        public Texture2D AsosiyTekstura;
        public Texture2D NamlikTeksturasi;
        public Texture2D QorTeksturasi;
        public Texture2D MuzTeksturasi;
        public Texture2D LoyTeksturasi;
        
        // Normal xaritalar
        public Texture2D AsosiyNormalXarita;
        public Texture2D NamNormalXarita;
        public Texture2D QorNormalXarita;
        public Texture2D MuzNormalXarita;
        public Texture2D LoyNormalXarita;
        
        // Shader parametrlari
        public static readonly int AsosiyTeksturaID = Shader.PropertyToID("_MainTex");
        public static readonly int NormalXaritaID = Shader.PropertyToID("_BumpMap");
        public static readonly int NamlikXaritaID = Shader.PropertyToID("_NamlikMap");
        public static readonly int QorXaritaID = Shader.PropertyToID("_QorMap");
        public static readonly int MuzXaritaID = Shader.PropertyToID("_MuzMap");
        public static readonly int LoyXaritaID = Shader.PropertyToID("_LoyMap");
        
        // Tekstura o'tish parametrlari
        public float TeksturaOtishTezligi = 1f;
        public float joriyNamlikKoef;
        public float joriyQorKoef;
        public float joriyMuzKoef;
        public float joriyLoyKoef;
    }
    
    private void YerTeksturalariniYangilash()
    {
        // Yer holatiga qarab tekstura koeffitsientlarini yangilash
        YerHolatiKoeffitsientlariniYangilash();
        
        // Teksturalarni aralashtirish
        TeksturalarniAralashtirish();
        
        // Normal xaritalarni yangilash
        NormalXaritalarniYangilash();
    }
    
    private void YerHolatiKoeffitsientlariniYangilash()
    {
        // Namlik koeffitsienti
        float maqsadNamlik = yerMuhiti.YerNamligi;
        yerTekstura.joriyNamlikKoef = Mathf.Lerp(
            yerTekstura.joriyNamlikKoef,
            maqsadNamlik,
            Time.deltaTime * yerTekstura.TeksturaOtishTezligi
        );
        
        // Qor koeffitsienti
        float maqsadQor = yerMuhiti.QorQalinligi;
        yerTekstura.joriyQorKoef = Mathf.Lerp(
            yerTekstura.joriyQorKoef,
            maqsadQor,
            Time.deltaTime * yerTekstura.TeksturaOtishTezligi
        );
        
        // Muz koeffitsienti
        float maqsadMuz = yerMuhiti.MuzQalinligi;
        yerTekstura.joriyMuzKoef = Mathf.Lerp(
            yerTekstura.joriyMuzKoef,
            maqsadMuz,
            Time.deltaTime * yerTekstura.TeksturaOtishTezligi
        );
        
        // Loy koeffitsienti
        float maqsadLoy = yerMuhiti.JoriyHolat == YerHolati.Loyqa ? yerMuhiti.HolatIntensivligi : 0f;
        yerTekstura.joriyLoyKoef = Mathf.Lerp(
            yerTekstura.joriyLoyKoef,
            maqsadLoy,
            Time.deltaTime * yerTekstura.TeksturaOtishTezligi
        );
    }
    
    private void TeksturalarniAralashtirish()
    {
        if (yerTekstura.AsosiyTekstura != null)
        {
            // Asosiy teksturani o'rnatish
            Shader.SetGlobalTexture(YerTeksturaTizimi.AsosiyTeksturaID, yerTekstura.AsosiyTekstura);
            
            // Qo'shimcha tekstura xaritalarini o'rnatish
            if (yerTekstura.NamlikTeksturasi != null)
            {
                Shader.SetGlobalTexture(YerTeksturaTizimi.NamlikXaritaID, yerTekstura.NamlikTeksturasi);
                Shader.SetGlobalFloat("_NamlikKoef", yerTekstura.joriyNamlikKoef);
            }
            
            if (yerTekstura.QorTeksturasi != null)
            {
                Shader.SetGlobalTexture(YerTeksturaTizimi.QorXaritaID, yerTekstura.QorTeksturasi);
                Shader.SetGlobalFloat("_QorKoef", yerTekstura.joriyQorKoef);
            }
            
            if (yerTekstura.MuzTeksturasi != null)
            {
                Shader.SetGlobalTexture(YerTeksturaTizimi.MuzXaritaID, yerTekstura.MuzTeksturasi);
                Shader.SetGlobalFloat("_MuzKoef", yerTekstura.joriyMuzKoef);
            }
            
            if (yerTekstura.LoyTeksturasi != null)
            {
                Shader.SetGlobalTexture(YerTeksturaTizimi.LoyXaritaID, yerTekstura.LoyTeksturasi);
                Shader.SetGlobalFloat("_LoyKoef", yerTekstura.joriyLoyKoef);
            }
        }
    }
    
    private void NormalXaritalarniYangilash()
    {
        // Asosiy normal xaritani o'rnatish
        if (yerTekstura.AsosiyNormalXarita != null)
        {
            Shader.SetGlobalTexture(YerTeksturaTizimi.NormalXaritaID, yerTekstura.AsosiyNormalXarita);
        }
        
        // Qo'shimcha normal xaritalarni aralashtirish
        if (yerTekstura.NamNormalXarita != null && yerTekstura.joriyNamlikKoef > 0)
        {
            AralashtirishNormalXarita(yerTekstura.NamNormalXarita, yerTekstura.joriyNamlikKoef);
        }
        
        if (yerTekstura.QorNormalXarita != null && yerTekstura.joriyQorKoef > 0)
        {
            AralashtirishNormalXarita(yerTekstura.QorNormalXarita, yerTekstura.joriyQorKoef);
        }
        
        if (yerTekstura.MuzNormalXarita != null && yerTekstura.joriyMuzKoef > 0)
        {
            AralashtirishNormalXarita(yerTekstura.MuzNormalXarita, yerTekstura.joriyMuzKoef);
        }
        
        if (yerTekstura.LoyNormalXarita != null && yerTekstura.joriyLoyKoef > 0)
        {
            AralashtirishNormalXarita(yerTekstura.LoyNormalXarita, yerTekstura.joriyLoyKoef);
        }
    }
    
    private void AralashtirishNormalXarita(Texture2D normalXarita, float koeffitsient)
    {
        // Normal xaritani aralashtirish uchun shader parametrlarini o'rnatish
        Shader.SetGlobalTexture("_QoshimchaNormalXarita", normalXarita);
        Shader.SetGlobalFloat("_NormalAralashKoef", koeffitsient);
    }
    
    // Yer tekstura tizimini sozlash
    private void YerTeksturaTiziminiSozlash()
    {
        // Teksturalarni yuklash
        if (yerTekstura.AsosiyTekstura == null)
        {
            yerTekstura.AsosiyTekstura = Resources.Load<Texture2D>("Textures/Terrain/Base");
            yerTekstura.NamlikTeksturasi = Resources.Load<Texture2D>("Textures/Terrain/Wet");
            yerTekstura.QorTeksturasi = Resources.Load<Texture2D>("Textures/Terrain/Snow");
            yerTekstura.MuzTeksturasi = Resources.Load<Texture2D>("Textures/Terrain/Ice");
            yerTekstura.LoyTeksturasi = Resources.Load<Texture2D>("Textures/Terrain/Mud");
        }
        
        // Normal xaritalarni yuklash
        if (yerTekstura.AsosiyNormalXarita == null)
        {
            yerTekstura.AsosiyNormalXarita = Resources.Load<Texture2D>("Textures/Terrain/Base_Normal");
            yerTekstura.NamNormalXarita = Resources.Load<Texture2D>("Textures/Terrain/Wet_Normal");
            yerTekstura.QorNormalXarita = Resources.Load<Texture2D>("Textures/Terrain/Snow_Normal");
            yerTekstura.MuzNormalXarita = Resources.Load<Texture2D>("Textures/Terrain/Ice_Normal");
            yerTekstura.LoyNormalXarita = Resources.Load<Texture2D>("Textures/Terrain/Mud_Normal");
        }
    }
}
