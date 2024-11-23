using UnityEngine;

public enum YerHolati
{
    Normal,
    Nam,
    Qorli,
    Muzli,
    Loyqa
}

public class YerMuhitTizimi : MonoBehaviour
{
    // Yer holati parametrlari
    public YerHolati JoriyHolat { get; private set; }
    public float HolatIntensivligi { get; private set; }
    
    // Yer namligi (0-1)
    public float YerNamligi { get; private set; }
    
    // Qor qalinligi (0-1)
    public float QorQalinligi { get; private set; }
    
    // Muz qalinligi (0-1)
    public float MuzQalinligi { get; private set; }
    
    // Parametrlarni yangilash
    public void YerHolatiniYangilash(ObHavoTizimi obHavo, FaslTizimi fasl)
    {
        // Namlikni hisoblash
        YerNamligi = Mathf.Clamp01(obHavo.YogingarchlikDarajasi);
        
        // Qor qalinligini hisoblash
        if (fasl.JoriyFasl == Fasl.Qish && obHavo.Harorat < 0)
        {
            QorQalinligi = Mathf.Lerp(QorQalinligi, 1f, Time.deltaTime);
        }
        else
        {
            QorQalinligi = Mathf.Lerp(QorQalinligi, 0f, Time.deltaTime);
        }
        
        // Muz qalinligini hisoblash
        if (obHavo.Harorat < -5f && YerNamligi > 0.3f)
        {
            MuzQalinligi = Mathf.Lerp(MuzQalinligi, 1f, Time.deltaTime);
        }
        else
        {
            MuzQalinligi = Mathf.Lerp(MuzQalinligi, 0f, Time.deltaTime);
        }
        
        // Yer holatini aniqlash
        YerHolatiniAniqlash(obHavo, fasl);
    }
    
    private void YerHolatiniAniqlash(ObHavoTizimi obHavo, FaslTizimi fasl)
    {
        if (MuzQalinligi > 0.5f)
        {
            JoriyHolat = YerHolati.Muzli;
            HolatIntensivligi = MuzQalinligi;
        }
        else if (QorQalinligi > 0.5f)
        {
            JoriyHolat = YerHolati.Qorli;
            HolatIntensivligi = QorQalinligi;
        }
        else if (YerNamligi > 0.7f)
        {
            JoriyHolat = YerHolati.Loyqa;
            HolatIntensivligi = YerNamligi;
        }
        else if (YerNamligi > 0.3f)
        {
            JoriyHolat = YerHolati.Nam;
            HolatIntensivligi = YerNamligi;
        }
        else
        {
            JoriyHolat = YerHolati.Normal;
            HolatIntensivligi = 0f;
        }
    }
}
