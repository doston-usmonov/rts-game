using UnityEngine;

public class ObHavoTizimi : MonoBehaviour
{
    // Ob-havo parametrlari
    public float Harorat { get; private set; }
    public float YogingarchlikDarajasi { get; private set; }
    public float ShamolTezligi { get; private set; }
    public Vector3 ShamolYonalishi { get; private set; }
    
    // Parametrlarni yangilash
    public void ObHavoniYangilash(FaslTizimi fasl)
    {
        // Haroratni hisoblash
        float bazaHarorat = fasl.BazaHaroratiniOlish();
        float kunlikOzgarish = Mathf.Sin(Time.time * 0.1f) * 5f; // Kunlik harorat o'zgarishi
        Harorat = bazaHarorat + kunlikOzgarish;
        
        // Yog'ingarchilik darajasini hisoblash
        YogingarchlikDarajasi = Mathf.PingPong(Time.time * 0.05f, 1f);
        
        // Shamol parametrlarini hisoblash
        ShamolTezligi = Mathf.PingPong(Time.time * 0.1f, 10f);
        float shamolBurchagi = Time.time * 0.1f;
        ShamolYonalishi = new Vector3(
            Mathf.Cos(shamolBurchagi),
            0f,
            Mathf.Sin(shamolBurchagi)
        ).normalized;
    }
    
    // Ob-havo ta'sirini qo'llash
    public void ObHavoTasiriniQollash(GameObject target)
    {
        // Shamol ta'siri
        if (target.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Vector3 shamolKuchi = ShamolYonalishi * ShamolTezligi;
            rb.AddForce(shamolKuchi, ForceMode.Force);
        }
        
        // Boshqa ob-havo ta'sirlari...
    }
    
    private MuhitTovushlari muhitTovushlari;
    
    private void Start()
    {
        muhitTovushlari = FindObjectOfType<MuhitTovushlari>();
    }
    
    private void Update()
    {
        // Haroratni yangilash
        float bazaHarorat = 20f;
        float kunlikOzgarish = Mathf.Sin(Time.time * 0.1f) * 5f; // Kunlik harorat o'zgarishi
        Harorat = bazaHarorat + kunlikOzgarish;
        
        // Yog'ingarchilik darajasini yangilash
        YogingarchlikDarajasi = Mathf.PingPong(Time.time * 0.05f, 1f);
        
        // Shamol parametrlarini yangilash
        ShamolTezligi = Mathf.PingPong(Time.time * 0.1f, 10f);
        float shamolBurchagi = Time.time * 0.1f;
        ShamolYonalishi = new Vector3(
            Mathf.Cos(shamolBurchagi),
            0f,
            Mathf.Sin(shamolBurchagi)
        ).normalized;
        
        // Momaqaldiroq effekti
        if (Random.value < 0.001f && YogingarchlikDarajasi > 0.7f)
        {
            if (muhitTovushlari != null)
            {
                muhitTovushlari.MomaqaldiroqniIjroEtish();
            }
        }
    }
}
