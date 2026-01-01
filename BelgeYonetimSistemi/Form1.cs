using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OfficeOpenXml; // EPPlus
using PdfiumViewer;  // PDF Görüntüleme
using System.Collections.Generic;

namespace BelgeYonetimSistemi
{
    public partial class MainForm : Form
    {
        // --- DEĞİŞKENLER ---
        private string _calismaYolu = "";
        private string _fotoKlasoru = "";
        private string _dataFile = "";
        private string _secilenDosyaTemp = ""; // Kaydetmeden önceki geçici dosya

        // Remove the duplicate declaration at the end of the file:
        // DELETE THIS DUPLICATE BLOCK
        /*
            // Change this:
            private TabControl tabControl;

            // To this:
            private TabControl? tabControl;
        */

        // The correct declaration should be only once, at the top of the class, matching your intended nullability:
        private TabControl tabControl; // or private TabControl? tabControl; if you want it nullable
        private TextBox txtTC, txtAdSoyad, txtBelgeNo, txtFotoNo;
        private DateTimePicker dtpVerilis, dtpGecerlilik;
        private ComboBox cmbEgitici, cmbMesul;
        private PictureBox pbOnizleme;
        private DataGridView dgvListe;

        public MainForm()
        {
            // EPPlus Lisansı
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            this.Text = "Belge Yönetim Sistemi v1.0";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            ArayuzuOlustur(); // UI Tasarımını Yükle
            IlkAyarlar();     // Klasörleri Kontrol Et
            ListeyiYenile();  // Grid'i Doldur
        }

        // --- TASARIM MOTORU (Designer.cs yerine) ---
        private void ArayuzuOlustur()
        {
            tabControl = new TabControl { Dock = DockStyle.Fill, Padding = new Point(10, 6) };

            // SEKME 1: Veri Girişi
            TabPage page1 = new TabPage("Veri Girişi");
            page1.Padding = new Padding(20);

            // Sol Panel (Form)
            Panel pnlForm = new Panel { Dock = DockStyle.Left, Width = 400, BackColor = Color.WhiteSmoke };

            // UI Yardımcısı ile kontrollleri ekle
            int y = 20;
            pnlForm.Controls.Add(new Label { Text = "Fotoğraf No:", Top = y, Left = 10 });
            txtFotoNo = new TextBox { Top = y, Left = 120, Width = 100, ReadOnly = true };
            pnlForm.Controls.Add(txtFotoNo);

            y += 40;
            Button btnDosyaSec = new Button { Text = "Dosya Seç (Resim/PDF)", Top = y, Left = 10, Width = 210, Height = 30, BackColor = Color.LightBlue };
            btnDosyaSec.Click += BtnDosyaSec_Click;
            pnlForm.Controls.Add(btnDosyaSec);

            y += 40;
            pbOnizleme = new PictureBox { Top = y, Left = 10, Width = 210, Height = 150, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
            pnlForm.Controls.Add(pbOnizleme);

            y += 160;
            pnlForm.Controls.Add(new Label { Text = "TC Kimlik:", Top = y, Left = 10 });
            txtTC = new TextBox { Top = y, Left = 120, Width = 200, MaxLength = 11 };
            pnlForm.Controls.Add(txtTC);

            y += 35;
            pnlForm.Controls.Add(new Label { Text = "Adı Soyadı:", Top = y, Left = 10 });
            txtAdSoyad = new TextBox { Top = y, Left = 120, Width = 200 };
            pnlForm.Controls.Add(txtAdSoyad);

            y += 35;
            pnlForm.Controls.Add(new Label { Text = "Belge No:", Top = y, Left = 10 });
            txtBelgeNo = new TextBox { Top = y, Left = 120, Width = 200 };
            pnlForm.Controls.Add(txtBelgeNo);

            y += 35;
            pnlForm.Controls.Add(new Label { Text = "Veriliş Tarihi:", Top = y, Left = 10 });
            dtpVerilis = new DateTimePicker { Top = y, Left = 120, Width = 200, Format = DateTimePickerFormat.Short };
            pnlForm.Controls.Add(dtpVerilis);

            y += 35;
            pnlForm.Controls.Add(new Label { Text = "Geçerlilik Tar:", Top = y, Left = 10 });
            dtpGecerlilik = new DateTimePicker { Top = y, Left = 120, Width = 200, Format = DateTimePickerFormat.Short };
            pnlForm.Controls.Add(dtpGecerlilik);

            y += 35;
            pnlForm.Controls.Add(new Label { Text = "Eğitici:", Top = y, Left = 10 });
            cmbEgitici = new ComboBox { Top = y, Left = 120, Width = 200 };
            cmbEgitici.Items.AddRange(new object[] { "Ahmet Yılmaz", "Mehmet Demir" });
            pnlForm.Controls.Add(cmbEgitici);

            y += 35;
            pnlForm.Controls.Add(new Label { Text = "Mesul Müdür:", Top = y, Left = 10 });
            cmbMesul = new ComboBox { Top = y, Left = 120, Width = 200 };
            cmbMesul.Items.AddRange(new object[] { "Ayşe Kara", "Fatma Çelik" });
            pnlForm.Controls.Add(cmbMesul);

            y += 50;
            Button btnKaydet = new Button { Text = "KAYDET", Top = y, Left = 10, Width = 310, Height = 40, BackColor = Color.SeaGreen, ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold) };
            btnKaydet.Click += BtnKaydet_Click;
            pnlForm.Controls.Add(btnKaydet);

            page1.Controls.Add(pnlForm);

            // SEKME 2: Liste ve Grid
            TabPage page2 = new TabPage("Kayıt Listesi");
            dgvListe = new DataGridView { Dock = DockStyle.Fill, RowTemplate = { Height = 80 }, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

            // Grid Sütunları
            DataGridViewImageColumn imgCol = new DataGridViewImageColumn { Name = "Img", HeaderText = "Önizleme", Width = 80, ImageLayout = DataGridViewImageCellLayout.Zoom };
            dgvListe.Columns.Add(imgCol);
            dgvListe.Columns.Add("No", "Foto No");
            dgvListe.Columns.Add("TC", "TC Kimlik");
            dgvListe.Columns.Add("Ad", "Ad Soyad");
            dgvListe.Columns.Add("Belge", "Belge No");
            dgvListe.Columns.Add("Tarih", "Bitiş Tarihi");

            // Panel (Alt Butonlar)
            Panel pnlGridAlt = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            Button btnExcel = new Button { Text = "Excel Oluştur ve ZIP'le", Top = 10, Left = 10, Width = 200, Height = 40 };
            btnExcel.Click += BtnExcel_Click;
            pnlGridAlt.Controls.Add(btnExcel);

            page2.Controls.Add(dgvListe);
            page2.Controls.Add(pnlGridAlt);

            tabControl.TabPages.Add(page1);
            tabControl.TabPages.Add(page2);
            this.Controls.Add(tabControl);
        }

        // --- İŞ MANTIĞI ---

        private void IlkAyarlar()
        {
            // Masaüstünde klasör oluştur (Test için)
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _calismaYolu = Path.Combine(desktop, "BelgeYonetim_Data");

            if (!Directory.Exists(_calismaYolu))
            {
                DialogResult dr = MessageBox.Show("Masaüstünde çalışma klasörü oluşturulsun mu?", "İlk Kurulum", MessageBoxButtons.YesNo);
                if (dr == DialogResult.Yes) Directory.CreateDirectory(_calismaYolu);
                else return;
            }

            _fotoKlasoru = Path.Combine(_calismaYolu, "foto");
            _dataFile = Path.Combine(_calismaYolu, "data.txt");

            if (!Directory.Exists(_fotoKlasoru)) Directory.CreateDirectory(_fotoKlasoru);
            if (!File.Exists(_dataFile)) File.Create(_dataFile).Close();

            SiradakiNumarayiBul();
        }

        private void SiradakiNumarayiBul()
        {
            // Basit numara takibi: data.txt'deki en son numarayı bul + 1
            int max = 0;
            if (File.Exists(_dataFile))
            {
                var lines = File.ReadAllLines(_dataFile);
                foreach (var line in lines)
                {
                    var kayit = BelgeKayit.FromLine(line);
                    if (kayit != null && kayit.FotoNo > max) max = kayit.FotoNo;
                }
            }
            txtFotoNo.Text = (max + 1).ToString();
        }

        private void BtnDosyaSec_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Dosyalar|*.jpg;*.jpeg;*.png;*.pdf";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _secilenDosyaTemp = ofd.FileName;
                    string ext = Path.GetExtension(_secilenDosyaTemp).ToLower();

                    // Önizleme Göster
                    if (ext == ".pdf")
                    {
                        try
                        {
                            using (var doc = PdfDocument.Load(_secilenDosyaTemp))
                            {
                                pbOnizleme.Image = doc.Render(0, 300, 300, true);
                            }
                        }
                        catch { MessageBox.Show("PDF Önizleme hatası (NuGet paketlerini kontrol et)."); }
                    }
                    else
                    {
                        pbOnizleme.Image = Image.FromFile(_secilenDosyaTemp);
                    }
                }
            }
        }

        private void BtnKaydet_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_secilenDosyaTemp)) { MessageBox.Show("Lütfen dosya seçin!"); return; }
            if (txtTC.Text.Length != 11) { MessageBox.Show("TC 11 hane olmalı."); return; }

            int fotoNo = int.Parse(txtFotoNo.Text);
            string kaynakUzanti = Path.GetExtension(_secilenDosyaTemp).ToLower();

            // Eğer resimse JPG yap, PDF ise PDF kalsın
            string hedefUzanti = (kaynakUzanti == ".pdf") ? ".pdf" : ".jpg";
            string hedefYol = Path.Combine(_fotoKlasoru, fotoNo + hedefUzanti);

            // Dosyayı Kaydet
            if (kaynakUzanti == ".pdf")
            {
                File.Copy(_secilenDosyaTemp, hedefYol, true);
            }
            else
            {
                using (Image img = Image.FromFile(_secilenDosyaTemp))
                {
                    img.Save(hedefYol, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
            }

            // Text Veriyi Kaydet
            BelgeKayit kayit = new BelgeKayit
            {
                FotoNo = fotoNo,
                TC = txtTC.Text,
                AdSoyad = txtAdSoyad.Text,
                BelgeNo = txtBelgeNo.Text,
                VerilisTarihi = dtpVerilis.Value,
                GecerlilikTarihi = dtpGecerlilik.Value,
                EgiticiAdi = cmbEgitici.Text,
                MesulMudur = cmbMesul.Text,
                DosyaUzantisi = hedefUzanti
            };

            File.AppendAllText(_dataFile, kayit.ToString() + Environment.NewLine);

            MessageBox.Show("Kayıt Başarılı!");

            // Temizlik
            SiradakiNumarayiBul();
            txtTC.Clear(); txtAdSoyad.Clear(); pbOnizleme.Image = null; _secilenDosyaTemp = "";
            ListeyiYenile();
        }

        private void ListeyiYenile()
        {
            dgvListe.Rows.Clear();
            if (!File.Exists(_dataFile)) return;

            var lines = File.ReadAllLines(_dataFile);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var k = BelgeKayit.FromLine(line);
                if (k == null) continue;

                Image thumb = null;
                string dosyaYolu = Path.Combine(_fotoKlasoru, k.FotoNo + k.DosyaUzantisi);

                if (File.Exists(dosyaYolu))
                {
                    try
                    {
                        if (k.DosyaUzantisi == ".pdf")
                        {
                            // PDF Thumbnail (İlk sayfa)
                            using (var doc = PdfDocument.Load(dosyaYolu))
                            {
                                thumb = doc.Render(0, 100, 100, 96, 96, false);
                            }
                        }
                        else
                        {
                            // Resim Thumbnail
                            using (var img = Image.FromFile(dosyaYolu))
                            {
                                thumb = new Bitmap(img, new Size(100, 100));
                            }
                        }
                    }
                    catch { /* Hata olursa boş geç */ }
                }

                dgvListe.Rows.Add(thumb, k.FotoNo, k.TC, k.AdSoyad, k.BelgeNo, k.GecerlilikTarihi.ToShortDateString());
            }
        }

        private void BtnExcel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Bu demo sürümde veriler Excel'e aktarıldı simülasyonu yapılmıştır.\nDosya Yolu: " + _calismaYolu);
            // Gerçek implementasyon için EPPlus kodlarını buraya ekleyebilirsin.
        }
        public static BelgeKayit FromLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // Adjust the delimiter and parsing logic as per your data.txt format
            // Example: FotoNo|TC|AdSoyad|BelgeNo|VerilisTarihi|GecerlilikTarihi|EgiticiAdi|MesulMudur|DosyaUzantisi
            var parts = line.Split('|');
            if (parts.Length < 9)
                return null;

            return new BelgeKayit
            {
                FotoNo = int.TryParse(parts[0], out int fotoNo) ? fotoNo : 0,
                TC = parts[1],
                AdSoyad = parts[2],
                BelgeNo = parts[3],
                VerilisTarihi = DateTime.TryParse(parts[4], out DateTime verilis) ? verilis : DateTime.MinValue,
                GecerlilikTarihi = DateTime.TryParse(parts[5], out DateTime gecerlilik) ? gecerlilik : DateTime.MinValue,
                EgiticiAdi = parts[6],
                MesulMudur = parts[7],
                DosyaUzantisi = parts[8]
            };
        }
    }
        // Change this:
        private TabControl tabControl;

        // To this:
        private TabControl? tabControl;
        
}