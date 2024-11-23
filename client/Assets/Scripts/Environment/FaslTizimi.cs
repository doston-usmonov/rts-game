using UnityEngine;

public enum Fasl
{
    Bahor,
    Yoz,
    Kuz,
    Qish
}

public class FaslTizimi : MonoBehaviour
{
    // Fasl parametrlari
    public Fasl JoriyFasl { get; private set; }
    private float faslVaqti;
    
    // Fasl davomiyligi (sekundlarda)
    public float FaslDavomiyligi = 300f; // 5 daqiqa
    
    private void Start()
    {
        JoriyFasl = Fasl.Bahor;
        faslVaqti = 0f;
    }
    
    private void Update()
    {
        // Fasl vaqtini yangilash
        faslVaqti += Time.deltaTime;
        
        // Fasl almashinuvi
        if (faslVaqti >= FaslDavomiyligi)
        {
            faslVaqti = 0f;
            JoriyFasl = (Fasl)(((int)JoriyFasl + 1) % 4);
        }
    }
    
    // Faslga qarab baza haroratini qaytarish
    public float BazaHaroratiniOlish()
    {
        switch (JoriyFasl)
        {
            case Fasl.Bahor:
                return 15f;
            case Fasl.Yoz:
                return 25f;
            case Fasl.Kuz:
                return 10f;
            case Fasl.Qish:
                return -5f;
            default:
                return 15f;
        }
    }
    
    // Fasl o'zgarishi hodisasi
    public delegate void FaslOzgarishiHandler(Fasl yangiFasl);
    public event FaslOzgarishiHandler FaslOzgarishi;
    
    // Fasl o'zgarishini e'lon qilish
    private void FaslOzgarishiniElonQilish()
    {
        FaslOzgarishi?.Invoke(JoriyFasl);
    }
}
