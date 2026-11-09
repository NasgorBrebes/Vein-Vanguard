# Vein Vanguard

**Game Design Document (GDD) · Revisi 9 November 2026**

**Tim:** Christian Gideon Valent (programmer), Hilmi Aminuddien (game designer), Syachrul Ramadhan (2D artist), Mohammad Ferry Irwansyah (sound designer dan project lead).

## 1. Konsep dan tujuan pembelajaran

Vein Vanguard adalah RPG edukasi 2D dengan eksplorasi peta dan pertarungan bergiliran dari samping. Pemain mengendalikan Nanobot, robot medis yang menyusuri pembuluh darah fiktif. Untuk menembakkan plasma dengan efektif, pemain membaca dua rekaman, memasangkan kartu yang menjawab masing-masing rekaman, lalu mempertimbangkan kapan perlu mengisi energi atau mengaktifkan perisai.

Tiga topik pembelajaran membentuk rangkaian misi:

- **Pangan.** Mengenali sumber lemak jenuh dan gula bebas, membaca label serta ukuran saji, dan memilih perubahan pangan yang sesuai dengan konteks rekaman.
- **Tembakau.** Membedakan peran nikotin, karbon monoksida, dan zat lain yang muncul dalam rekaman asap tembakau.
- **Alkohol.** Membedakan etanol, hasil penguraiannya, dan enzim yang berperan. Pertarungan terakhir menggabungkan rekaman tembakau dan alkohol.

Kartu berisi informasi atau pilihan perubahan pangan. Nanobot menggunakan data tersebut untuk mengkalibrasi senjata. Nanobot, monster, dan plasma adalah fiksi. HP, MP, dan damage adalah angka permainan. Pembahasan mengaitkan pilihan dengan bukti pada rekaman A dan B. Proyek ditujukan sebagai game edukasi gratis untuk Android landscape. Sasaran usia dan efektivitas pembelajaran masih perlu divalidasi.

## 2. Alur misi dan eksplorasi

**START → PROTOKOL DARURAT → MAP 01 → empat encounter → Map ke dua → MAP 02 → Ethanol Mimic → Toxifang → MISI SELESAI.**

1. Layar awal menawarkan START, SETTINGS, dan CREDITS. START memutar opening PROTOKOL DARURAT, lalu membuka sesi baru dengan 100 HP dan 60 MP.
2. Pemain berjalan di koridor pada ilustrasi peta. Kamera peta mengikuti Nanobot. Musuh berpatroli di sekitar posisinya, dan mendekati musuh yang masih aktif memulai pertarungan.
3. Empat musuh di MAP 01 dapat dihadapi sesuai rute yang dipilih pemain. Setelah keempatnya kalah, tombol **Map ke dua** membuka MAP 02. Editor dan development build memiliki pintasan pengujian **DEV: MAP 2**.
4. Di MAP 02, tangga bawah dan jembatan menuju Ethanol Mimic. Setelah menang, pemain kembali ke area awal dan mengikuti tangga atas menuju Toxifang. Toxifang terkunci sampai Ethanol Mimic kalah.
5. Kemenangan menyembunyikan penanda musuh dan memberi bonus **25 HP dan 20 MP**, masing-masing sampai batas maksimum. HP dan MP berlanjut antar-encounter dan antarpeta dalam satu sesi. Setelah seluruh musuh kalah, ringkasan menampilkan sisa HP dan jumlah jawaban kuis yang benar.
6. Saat HP mencapai 0, **MISI TERHENTI** menawarkan COBA LAGI atau MENU. COBA LAGI memulai sesi dari awal. Volume musik dan efek suara tersimpan lokal. Progres misi berlaku selama sesi berjalan.

**Kontrol.** Joystick virtual atau ketuk/klik jalur untuk bergerak. Di komputer, gunakan WASD atau tombol panah. Tab/Shift+Tab memindahkan fokus, Space atau Enter mengaktifkan kontrol, dan Escape menutup panel yang mengizinkan penutupan atau membuka pengaturan di luar battle. HUD peta menunjukkan nomor peta, HP, MP, dan objektif.

**Opening PROTOKOL DARURAT.** Cinematic 60 detik memperkenalkan sinyal darurat (0–10 detik), tiga kelompok ancaman (10–22), aktivasi Nanobot (22–35), dua rekaman dan empat kemampuan (35–47), lalu keberangkatan misi (47–60). Pemain membaca subtitle SISTEM dan NANOBOT sambil lingkungan, siluet, partikel, serta hologram bergerak. SKIP melewati cerita dan melanjutkan ke portal kemunculan MAP 01. Memulai sesi baru dari title memutar opening kembali. COBA LAGI memakai restart gameplay melalui layar loading. Opening memakai preferensi kurang gerak, safe area, dan volume yang sama dengan game. Revisi V2 menampilkan ancaman bergantian dengan animasi asli dan scanner, pose kebangkitan Nanobot, empat demonstrasi kemampuan selama 12 detik, serta kamera berlapis yang mengikuti keberangkatan. Peta mulai disiapkan saat layar menghitam, lalu muncul melalui fade sebelum kontrol pemain aktif. SFX mengikuti event aksi, dan volume musik meningkat menuju misi. Polish V3 mengaitkan langkah Nanobot dengan jarak tempuh dunia dan tracking kamera. Sambungan background, foreground yang melintas, serta gerbang simulasi berisi artwork MAP 01 menghubungkan perjalanan akhir dengan gameplay. Aktivasi memakai crop rig kepala/tubuh, glint mata, dan energi yang naik dari kaki. Masing-masing kategori ancaman memiliki entry serta reaksi scanner yang berbeda. Durasi tetap 60 detik.

**Portal masuk area.** Setelah opening atau SKIP, portal cyan muncul di spawn asli MAP 01. Tombol Map ke dua memuat area lalu menampilkan portal violet dengan aksen cyan di spawn asli MAP 02. Dalam sekitar 3,2 detik, partikel berkumpul, cincin energi membuka, Nanobot terungkap dan maju sedikit secara visual, kamera melebar dari zoom 2,2 ke 1, lalu portal menutup. HUD dan kontrol diaktifkan setelah selesai. Mode kurang gerak memakai fade sekitar 0,65 detik tanpa zoom atau orbit. Input, encounter, dan tujuan tap diblokir selama kemunculan; tombol gerak yang tertahan harus dilepas dahulu. Kembali dari battle tidak mengulang portal. Retry memulai ulang sesi dan portal MAP 01. Animator tidak mengubah posisi logis, rute, statistik, atau progres; efek dipakai ulang dalam keadaan nonaktif setelah selesai.

## 3. Roster dan rekaman pembelajaran

| Peta | Musuh | HP | Damage musuh | Rekaman A | Rekaman B | Soal Restore |
| --- | --- | ---: | ---: | --- | --- | ---: |
| 01 | Lipid Golem | 100 | 14 | Mentega sebagai olesan | Minyak kelapa untuk menggoreng | 12 |
| 01 | Gluco Slime | 130 | 18 | Sirup sebagai pemanis | Jus tanpa gula tambahan | 12 |
| 01 | Smoke Imp | 130 | 18 | Zat tembakau penyebab ketergantungan | Gas asap yang mengganggu pengangkutan oksigen | 6 |
| 01 | Frystack Duo | 130 | 18 | Olesan mentega pada roti burger | Kentang yang digoreng dengan minyak kelapa | 6 |
| 02 | Ethanol Mimic | 130 | 18 | Jenis alkohol pada minuman | Hasil penguraian etanol oleh ADH | 6 |
| 02 | Toxifang | 160 | 18 | Gas asap yang mengganggu pengangkutan oksigen | Hasil penguraian etanol oleh ADH | 12 |

Frystack Duo tampil sebagai satu encounter. Narasi pembuka boss memakai gelar **Toxifang, the Vein Serpent**. Toxifang menggunakan kembali 12 soal Smoke Imp dan Ethanol Mimic. Totalnya **42 soal unik**.

## 4. Siklus pertarungan

| Aksi | Syarat dan dampak |
| --- | --- |
| **Diagnose** | Satu pemindaian membuka rekaman A dan B dan mengaktifkan fase pemain. Bebas biaya dan bebas giliran. Membuka panel lagi menampilkan hasil tersimpan. |
| **Synthesize** | Tersedia setelah scan jika MP minimal 20. Buka drawer perakitan, pasang kartu A/B, tunggu charge, lalu tembak. Satu tembakan memakai 20 MP dan satu giliran. |
| **Restore** | Tersedia setelah scan ketika MP di bawah 60. Jawab satu soal dengan tiga opsi. Jawaban benar memberi 40 MP, jawaban salah 10 MP, dengan batas 60 MP. Baca pembahasan lalu lanjut ke giliran musuh. |
| **Homeostasis** | Tersedia setelah scan jika MP minimal 10 dan perisai sebelumnya sudah habis. Aktivasi memakai 10 MP dan langsung memberi giliran kepada musuh. Dua serangan berikutnya memberi 0 damage ke pemain. Hit pertama memantulkan damage dasar musuh, hit kedua hanya diblokir. |

Restore mengacak soal dan opsi jawaban. Soal baru dapat berulang setelah seluruh dek encounter habis, dengan pencegahan pengulangan langsung saat dek berganti. Jawaban salah tetap memberi energi agar pemain bisa kembali mencoba formula sambil belajar dari penjelasan.

Saat proyektil mencapai musuh, game menerapkan damage dan biaya MP sekali. Tombol tembak terkunci selama aksi dan pembahasan. **HASIL SYNTHESIS** menjelaskan kedua rekaman pada setiap tembakan, termasuk damage 0, pasangan yang diulang, dan tembakan lethal. Pemain memilih **LANJUT · GILIRAN MUSUH** bila musuh bertahan atau **LANJUT · KEMENANGAN** bila musuh kalah. Musuh yang kalah akibat tembakan tidak menyerang balik. Pantulan Homeostasis juga dapat mengakhiri pertarungan.

## 5. Synthesize dan senjata plasma

Drawer Diagnose dan Synthesize terbuka secara terpisah. Synthesize menampilkan **enam kartu dalam grid 3 × 2**, panel fakta, pengingat singkat A/B, dan senjata plasma yang menghadap musuh. Port A dan B menempel pada badan senjata. Saat terisi, ikon menggantikan huruf di tengah port. Reaktor menunjukkan charge, dan tombol tembak mencantumkan biaya 20 MP. Drawer menempati sekitar 35% tinggi area aman.

- Seret kartu ke port, atau pilih kartu lalu pilih port. Kartu yang sama boleh mengisi kedua port. Pilih port terisi tanpa kartu aktif untuk mengosongkannya.
- Menyentuh kartu menampilkan fakta umum tanpa membocorkan kecocokannya terhadap rekaman. Tampilan sempit menyediakan scroll untuk mempertahankan keterbacaan dan ukuran sasaran.
- Port A menjawab rekaman A. Port B menjawab rekaman B. Susunan bertahan saat drawer ditutup atau saat pemain membuka kembali Synthesize dalam encounter yang sama. Encounter baru mengosongkan kedua port.
- Dua port terisi memulai charge otomatis **0,65 detik**. Mengubah rakitan mengulang atau membatalkan charge. Membuka kembali drawer dengan rakitan lengkap memulai charge baru. Charge, animasi, dan respons arena hanya menandakan kesiapan senjata.

### Palet kartu

| Palet | Kartu | Encounter |
| --- | --- | --- |
| Pangan | Batasi tambahan, Air putih, Buah utuh, Lemak tak jenuh, Kukus/rebus, Variasi olesan | Lipid Golem, Gluco Slime, Frystack Duo |
| Tembakau | Nikotin, Karbon monoksida, Tar, Benzena, Formaldehida, Kadmium | Smoke Imp |
| Alkohol | Etanol, Asetaldehida, Asetat, Enzim ADH, Enzim ALDH, Hati | Ethanol Mimic |
| Gabungan | Nikotin, Karbon monoksida, Tar, Etanol, Asetaldehida, Enzim ALDH | Toxifang |

Katalog memuat 18 kartu, sementara pemain melihat enam kartu per encounter. Kartu pangan memakai ikon, sedangkan kartu data memakai label dan singkatan. Palet dapat dipakai ulang. Toxifang menggunakan enam kartu dari dua palet yang sudah dikenalkan.

### Skor dan damage

| Hasil pasangan | Damage |
| --- | ---: |
| Kedua rekaman cocok penuh (2/2) | 40 |
| Minimal satu rekaman mendapat skor positif, selain pasangan 2/2 | 20 |
| Kedua rekaman tidak cocok (0/0) | 0 |

Satu kartu memperoleh skor **2** jika cocok penuh, **1** jika cocok sebagian, atau **0** jika meleset. Kartu pangan mengikuti sifat sumber. Contohnya, Kukus/rebus memperoleh skor sebagian untuk rekaman penggorengan. Kartu data menguji identifikasi dan memakai skor 2 atau 0. Pasangan 40 damage pada tiga encounter data adalah **Nikotin + Karbon monoksida** untuk Smoke Imp, **Etanol + Asetaldehida** untuk Ethanol Mimic, dan **Karbon monoksida + Asetaldehida** untuk Toxifang. Urutan mengikuti rekaman A lalu B.

## 6. Art direction dan antarmuka

Vein Vanguard memakai ilustrasi organik 2D dengan karakter ber-outline tebal. Nanobot berwarna putih dan sian, dengan aksen merah. Sprite musuh memakai animasi saat bertarung dan berkeliaran di peta. HUD navy memisahkan informasi dari kontrol dan mempertahankan kontras dengan arena.

| Arena | Identitas visual dan staging |
| --- | --- |
| Stage 1 | Artwork asli kuning, oranye, dan merah dengan struktur organik hijau kekuningan, siluet merah muda/ungu, serta tanah olive dengan bercak cairan hijau. |
| Stage 2 | Artwork asli lavender dengan lengkung jaringan dan tanah magenta. Posisi kedua aktor diturunkan mengikuti permukaan depan arena, dengan penyesuaian saat drawer dibuka. |

- **Kamera battle.** Stage Root uGUI melakukan pan dan zoom, sementara HUD tetap di tempat. Framing menyorot Nanobot saat idle, membuka ruang saat drawer tampil, bergeser ke musuh ketika proyektil meluncur, dan memusatkan arena saat musuh menyerang.
- **Lingkungan.** Artwork latar menjaga proporsi aslinya dan menutupi viewport. Shader menambah gerakan halus pada bagian belakang. Layer sel darah dan arus plasma memakai animasi 8 FPS. Charge penuh dan tembakan meningkatkan respons visual partikel. Bayangan kontak bergerak bersama aktor dan membantu menunjukkan titik pijak.
- **Menu radial.** Empat tombol muncul bergiliran di sekitar Nanobot. Diagnose memakai sian, Synthesize oranye, Restore hijau, dan Homeostasis amber. Aksi yang belum tersedia tetap terlihat dalam keadaan redup. Centang hijau menandai scan selesai. Fokus keyboard dan target sentuh 48 dp menjadi acuan desain.
- **Kurang gerak.** Preferensi internal `VV.Motion.Reduced` membekukan frame partikel, menonaktifkan pan/zoom battle, dan mengurangi gerak tombol. Menu SETTINGS saat ini menyediakan volume musik dan efek suara.

## 7. Implementasi, pengujian, dan status desain

| Bagian | Acuan proyek |
| --- | --- |
| Teknologi | Unity 6000.3.9f1, C#, URP, uGUI, TextMeshPro, dan Input System. Canvas referensi 1600 × 900 dengan safe area. |
| Scene aktif | `Assets/Scenes/VeinVanguard.unity` memakai hierarki baked. Sinkronisasi UI melalui `WireBakedHierarchy` dan `BakeHierarchyForEditor`. |
| Gameplay dan materi | `NutrientSystem.cs` untuk stats, giliran, dan Restore. `CombatSamples.cs` dan `UniversalFormula.cs` untuk rekaman, palet, skor, serta pembahasan. |
| Alur dan presentasi | `BattleManager.cs`, `WorldFlow.cs`, `DiagnosticWorkbench.cs`, dan `UiPresentation.cs`. |
| Aset aktif | Peta `Assets/Resources/map_2.png` dan `Assets/AsetBaru/map_3.png`. Arena `Assets/battle_background.png` dan `Assets/AsetBaru/battle_background_2.png`. Efek arena di `Assets/Game/Resources/BattleArena/`. |

Pemeriksaan penerimaan terbaru mencatat **42 PASS di Unity Editor**, dengan pengujian antarmuka pada 1280 × 720, 1600 × 900, 1920 × 1080, dan 2000 × 900. Pemeriksaan materi mencakup 216 pasangan kartu dari enam encounter. APK, Android fisik, dan pemahaman pemain umum belum diverifikasi pada revisi ini.

**Ruang lingkup berikutnya.** Desain Action Command dengan timing serang/parry, opsi otomatis, dan log singkat untuk formula berulang pernah disetujui sebagai rancangan. Build saat ini memakai damage 0/20/40 langsung dari formula, pantulan Homeostasis otomatis pada hit pertama, dan pembahasan penuh setiap tembakan. Usulan pengganti mekanik dua port ditunda. Jadwal rilis belum ditetapkan dalam revisi ini.
