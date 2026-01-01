using System;

namespace BelgeYonetimSistemi
{
    public class BelgeKayit
    {
        public int FotoNo { get; set; }
        public string TC { get; set; } = string.Empty;
        public string AdSoyad { get; set; } = string.Empty;
        public string BelgeNo { get; set; } = string.Empty;
        public DateTime VerilisTarihi { get; set; }
        public DateTime GecerlilikTarihi { get; set; }
        public string EgiticiAdi { get; set; } = string.Empty;
        public string MesulMudur { get; set; } = string.Empty;
        public string DosyaUzantisi { get; set; } = string.Empty; // .jpg veya .pdf

        // ... rest of the class remains unchanged ...
    }
}