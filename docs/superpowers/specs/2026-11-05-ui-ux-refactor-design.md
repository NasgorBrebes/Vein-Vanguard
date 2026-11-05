# Rancangan refactor UI/UX Vein Vanguard

Status: APPROVED oleh pengguna; implementasi berjalan.
Tanggal: 5 November 2026.
Sumber: brief pengguna `Pasted text.txt` pada attachment percakapan ini.
Antislop: During, pilihan pengguna untuk sesi ini.

Catatan persetujuan: pengguna mengizinkan penyusunan rencana dan eksekusi langsung. Kedua baris teks tombol battle wajib kontras tinggi; enam kartu harus mempertahankan label utuh dengan padding ringkas; jalur baked/fresh, scene tersimpan, seluruh pemeriksaan 1–8, dan screenshot wajib diverifikasi.

## Tujuan dan batas perubahan

Selaraskan antarmuka dengan karakter kartun 2D ber-outline tebal. Pemain harus dapat membedakan informasi, kontrol, dan aksi utama tanpa membaca semua label dahulu. Dunia dan karakter mendapat lebih banyak ruang; isi diagnosis tetap mudah dibaca.

Pertahankan fungsi, state, teks, konten pembelajaran, audio, aset karakter, map, serta aturan permainan. Memindahkan teks ke elemen terpisah atau mengubah baris/tipografinya diperbolehkan; jangan menghapus atau menulis ulang isinya. Perubahan perilaku hanya feedback UI yang diminta: hover/tekan, fade petunjuk setelah bergerak, dan indikator soket/reaktor. Pertahankan keyboard, pointer, joystick, serta pilihan ikon-lalu-soket sebagai alternatif drag.

Tidak termasuk migrasi framework, penggantian karakter/ilustrasi/font, dependensi baru, fitur gameplay, balancing, save progress, atau perubahan materi nutrisi. File pengguna yang belum terlacak di `Assets/AsetBaru`, `Assets/Temp/SeparateActionsQA`, dan `Assets/_Recovery` tidak diubah.

## Pendekatan

Pilihan implementer: refactor bertahap pada Unity uGUI dan TextMeshPro yang sudah digunakan. Pisahkan perlakuan panel dan tombol pada helper yang ada; rapikan layout tiap layar; sambungkan kembali nilai visual ke state yang sudah ada. Ini menjangkau jalur UI runtime dan scene yang sudah dibangun tanpa mengganti model permainan.

Alternatif yang dipertimbangkan:

- Mengganti sprite HUD saja lebih kecil, tetapi menyisakan top/bottom bar, susunan kartu, dead space, dan ukuran modal yang menjadi masalah di brief.
- Membangun ulang dengan UI Toolkit atau Canvas baru menambah migrasi wiring, fokus, dan drag/drop tanpa kebutuhan produk yang mendukungnya.

## Temuan proyek dan jangkauan

Proyek aktif: `D:/Kuliah/Mixed Reality/Unity/Vein-Vanguard`, Unity 6000.3.9f1, scene `Assets/Scenes/VeinVanguard.unity`.

- `WorldFlow.cs`: title, map, HUD eksplorasi, joystick skin, settings/credits, slider, serta helper `MedicalButton`, `ButtonFrame`, dan `Skin`. Saat ini helper bingkai tombol juga menata panel dan joystick.
- `BattleManager.cs`: pembentukan HUD, empat aksi, layout dialog, label status, HP/MP, narasi, serta refresh. Semua caller helper panel/tombol diperiksa agar penataan ulang tidak memberi teks gelap di panel gelap.
- `DiagnosticWorkbench.cs`: diagnosis, enam item formula, dua soket, indikator reaktor, dan feedback. `RefreshLearningWorkbench` saat ini mengatur ulang warna sehingga harus mengikuti skin baru.
- `ExplorationJoystick.cs`: input analog yang sudah bekerja; pertahankan clamp, pointer ownership, reset saat fokus hilang/menu terbuka, dan geometri sentuh.
- `FormulaIconDrag.cs` / `FormulaSocketDrop.cs`: pertahankan kontrak event dan pemetaan item/soket; ubah hanya jika kebutuhan visual tidak dapat dipenuhi di workbench.
- Scene mempunyai `hierarchyBaked: 1`. Perubahan pada `BuildUI`/`BuildFlow` saja tidak cukup; jalur `WireBakedHierarchy`/`BakeHierarchyForEditor` harus menghasilkan tampilan dan wiring yang sama, tanpa komponen duplikat saat dipanggil ulang.
- `Editor/PrototypeBuilder.cs`: pemeriksaan eksplorasi saat ini mewajibkan sprite putih dan tanda plus yang justru diminta dihapus. Ganti hanya assertion visual tersebut; pertahankan uji gerak, raycast, clamp, dan reset.
- `Editor/PlasmaWorkbenchChecks.cs` serta `DiagnosticWorkbenchChecks.cs`: pertahankan pemeriksaan aturan, hit target, teks, pemisahan drawer, dan karakter tidak tertutup.
- `Assets/Game/README-Menu-Exploration.md` dan `README-Diagnose-Synthesize.md`: sesuaikan petunjuk visual/pemeriksaan yang berubah; dokumentasi aturan gameplay tetap.

Model di `NutrientSystem.cs`, `UniversalFormula.cs`, dan `CombatSamples.cs` tidak memerlukan perubahan. Sprite HUD aksi, ikon sampel, enam formula, senjata, font, dan artwork yang sudah ada dipakai ulang. Bentuk rounded, cincin, dan bingkai putus-putus menggunakan kemampuan/geometry uGUI; tidak membuat gambar AI atau paket ilustrasi baru.

## Arah visual

Pembacaan desain: game edukasi petualangan medis dengan karakter kartun ber-outline tebal; ENERGY 3 / RHYTHM 2 / MOTION 2. Energi berasal dari bentuk tegas dan aksi berwarna; gerak hanya membantu input serta status rakitan.

| Elemen | Aturan | Alasan |
| --- | --- | --- |
| Panel informasi, narasi, drawer, modal | Navy `#0F1C2E`, opacity 88–92%, border `#2D6A8E` 2 unit, radius 12–16, shadow solid `#08101A` offset 4 | Konten terbaca di atas map dan selaras dengan outline karakter |
| Teks panel | `#F8FAFC`; label penting bold; font proyek tetap | Menjaga keterbacaan dan konten lama |
| Tombol | Radius sekitar 14, outline gelap 3, bevel bawah 4; hover naik sedikit, pressed menekan bevel | Bentuk dan respons membedakan kontrol dari panel |
| Status | HP `#22C55E`, MP `#06B6D4`; angka dan nama tetap terlihat | Warna membantu membaca nilai tanpa menjadi satu-satunya petunjuk |
| Aksi | Diagnose cyan; Synthesize oranye plasma; Restore hijau; Homeostasis amber | Empat warna semantik mengikuti brief dan fungsi, bukan dekorasi umum |
| Fokus dan disabled | Fokus ber-outline jelas; disabled abu-abu, label terbaca, input terkunci | Kontrol dapat dipakai dengan keyboard serta terbaca saat belum tersedia |
| Gerak | Hover/press singkat; hint fade; pulse hanya soket kosong; reaktor mengikuti charge | Feedback menunjukkan tindakan berikutnya dan kesiapan nyata |

Warna aksi yang beragam, bentuk kapsul pada badge/kontrol, dan pulse soket kosong adalah permintaan eksplisit pengguna. Terapkan terbatas pada fungsi itu. Panel menggunakan rounded rectangle; tidak semua komponen dijadikan pil. Shadow solid menandai elevasi panel dan bevel kontrol, tanpa blur kaca/glow latar. Glow terfokus pada soket yang meminta isi atau aksi tembak yang tersedia.

Ukuran di dokumen menggunakan unit Canvas referensi 1600 × 900; konversi melalui CanvasScaler dan safe area proyek. Periksa kontras teks terhadap warna panel yang sudah dikompositkan di atas latar paling terang, bukan warna navy opaque saja. Angka bar ditempatkan di samping kanan bila teks di atas fill tidak cukup kontras.

## Susunan per layar

### Title

Artwork tetap. Menu berada di bawah area judul seperti sekarang, dengan START sekitar 280 unit dan dua tombol sekunder sekitar 248 unit; batas lebar menyesuaikan safe area. START memakai hijau medis dan sedikit lebih tinggi; SETTINGS/CREDITS putih kebiruan dengan teks gelap. Nama tombol tetap agar wiring scene lama berfungsi.

### Eksplorasi / MAP 02

```text
[MAP 02 | HP bar + angka | MP bar + angka]          [SETTINGS] [MENU]
[Objektif lengkap sesuai progres]

                 Dunia, pemain, musuh + nameplate

 (joystick bulat)          [petunjuk kontrol lengkap, lalu memudar]
```

Hapus panel lebar atas/bawah. HUD kiri atas mengelompokkan badge map dan HP/MP, dengan objektif di bawahnya. Gunakan kapsul kecil bertuliskan SETTINGS dan MENU di kanan atas agar seluruh teks lama tetap tersedia; brief memperbolehkan alternatif ini terhadap ikon saja.

Joystick mempertahankan area sentuh 180 × 180, posisi 40/40 dari sudut safe area, dan knob 66 × 66. Hilangkan sprite bingkai tombol dan tanda plus; tampilkan lingkaran luar transparan bercincin dan knob bulat. Tidak mengubah kecepatan atau jalan yang dapat dilalui.

Nameplate GLUCO SLIME dan LIPID GOLEM mengikuti actor masing-masing: kapsul navy opacity 75%, teks putih bold. Dekorasi tidak mencegat klik map.

Petunjuk kontrol tetap lengkap dalam pil kecil yang menyesuaikan lebar, lalu memudar setelah perpindahan posisi pemain yang berhasil melalui analog, keyboard, atau klik. Input yang tertahan tembok tidak memicu fade. Tampilkan lagi pada sesi baru; kembali dari battle tidak mengulang onboarding. Hormati pause/settings dan safe area.

### Battle

```text
[NANOBOT + shield]          [LOKASI / GILIRAN]           [NAMA MUSUH]
[HP bar + angka]                                      [HP bar + angka]
[MP bar + angka]

                   Nanobot           Musuh

[ikon kecil | narasi pertarungan lengkap]
[DIAGNOSE]       [SYNTHESIZE]       [RESTORE]       [HOMEOSTASIS]
```

Status kiri/kanan dipadatkan; ruang arena tetap cukup untuk animasi dan VFX. Bar HP/MP minimal 16 unit tinggi. Angka HP/MP tetap terikat ke model dan tidak diduplikasi tanpa kebutuhan. SHIELD berikut persen dan hit berada di badge kecil yang tetap terbaca saat nol. Badge tengah mempertahankan semua variasi teks lokasi/giliran/trivia.

Narasi menggunakan panel gelap dengan indikator kecil dari bentuk/ikon yang sudah tersedia, terpisah dari empat tombol. Keempat aksi memiliki bobot setara, label serta subteks lama, dan aksen fungsi masing-masing. Perubahan state interactable tetap mengikuti `Refresh`; styling tidak membocorkan kebenaran formula atau mengaktifkan tindakan yang belum tersedia.

### Drawer DIAGNOSE

Tetap drawer bawah yang terpisah dari Synthesize. Dua profil A/B memiliki ikon lebih besar di bidang kontras, judul bold, badge kategori dari data klasifikasi yang sudah ada, lalu rekaman serta teks deskripsi lama. Informasi kategori hanya muncul setelah scan selesai, mengikuti gerbang diagnosis yang ada. Header, status scan, dan TUTUP tetap tersedia. Kedua musuh menggunakan susunan yang sama dan konten masing-masing.

### Drawer SYNTHESIZE

```text
SYNTHESIZE / FUSI PLASMA                                  [TUTUP]
[6 item: 3 kolom × 2 baris]  [senjata + Soket A / B]  [REAKTOR + bar]
                                                    [TEMBAKKAN PLASMA]
[Pengingat sumber A dan B lengkap]                   [feedback lengkap]
```

Ikon item diperbesar dan label tetap lengkap. Drag ghost tidak memblokir raycast; state selected membedakan ikon yang akan dipasang lewat tap/keyboard. Soket mempertahankan pemetaan A ke sumber A dan B ke sumber B; bingkai putus-putus berpulse saat kosong, ikon jelas saat terisi. Ikon yang sama boleh dipasang dua kali.

Pulse berfungsi sebagai ajakan mengisi soket kosong dan berhenti saat terisi, panel ditutup, atau tidak dapat dikonfigurasi. Jangan menambah pulse pada kartu lain. Jangan memakai warna/charge untuk menandai jawaban benar: semua rakitan lengkap tetap charge 0,65 detik.

Track reaktor minimal 16 unit; warna meningkat mengikuti charge dan status STANDBY/FUSION/CHARGED yang sudah ada. Fire abu-abu saat belum tersedia; terang saat dua soket terisi, charge selesai, energi cukup, dan tidak ada drag/aksi berjalan. Teks serta aturan biaya 20 MP tetap. MP/damage hanya berubah saat impact sesuai logika lama.

Drawer diagnosis dan synthesis tetap kurang dari 36% tinggi layar dan berada pada area bawah 40%; karakter bergeser seperti sistem dock lama dan kembali tepat saat ditutup. Periksa teks penuh dan hit target pada landscape sempit sebelum memperbesar ikon; jangan menutup karakter atau menghapus teks untuk memaksa layout muat.

### SETTINGS dan modal lain

SETTINGS di tengah, maksimal lebar 460 unit dan tinggi sesuai konten sekitar 380–420 unit. Track volume kapsul, thumb lingkaran ber-outline, area klik/sentuh tetap memadai. Label Musik, Efek suara, persentase, catatan penyimpanan, serta KEMBALI tetap. Slider masih memanggil setter dan PlayerPrefs yang sama. Jangan mengubah kebijakan mute/volume lama dalam refactor visual ini.

CREDITS, kuis, hasil, menang/kalah, dan dialog lain memakai panel informasi gelap, bukan bingkai tombol. Sesuaikan warna teks pada jalur dialog dinamis supaya tidak kembali ke navy-on-navy. Credits mendapat tinggi yang cukup untuk seluruh nama yang ada; batas SETTINGS tidak boleh memotong credits atau hasil pembelajaran.

## Interaksi dan ketahanan layout

- Pertahankan CanvasScaler, safe area, skala/krop/pivot sprite, serta kualitas impor aset yang ada. Tidak mengubah kamera global untuk memperbaiki satu panel.
- Pertahankan objek/referensi terserialisasi dan callback publik yang masih dipakai scene, Editor checks, atau script lain. Jalur UI baru dan baked harus setara tanpa membangun ulang scene secara destruktif.
- Semua tombol/slider yang diubah memiliki feedback normal, hover/fokus, pressed, dan disabled. Urutan navigasi mengikuti susunan visual; aktivasi keyboard memakai EventSystem/InputSystem yang ada.
- Escape menutup overlay/drawer/dialog yang dapat dibatalkan sesuai aturan yang ada. Jangan membuat Escape membatalkan giliran, scan, charge, atau feedback wajib yang sebelumnya tidak boleh dilewati. Fokus kembali ke pemicu bila masih aktif.
- Empty/loading/error tidak memerlukan layar rekaan: pertahankan soket kosong, scan berjalan, charge berjalan, energi tidak cukup, hasil salah/sebagian/benar, disabled actions, dan feedback yang sudah dimiliki game.
- Tanpa opsi baru untuk tema, animasi, atau input. Gunakan fasilitas platform/proyek yang sudah tersedia bila ada; jangan mengklaim dukungan reduced-motion atau pembaca layar yang belum diuji.

## Pemeriksaan penerimaan

1. Compile proyek dan periksa console sebelum mengklaim implementasi selesai. Jalankan pemeriksaan model `PrototypeBuilder.VerifyRules()` tanpa mengubah aturan.
2. Verifikasi jalur baked yang digunakan scene dan jalur fresh UI memberikan styling/wiring setara; pemanggilan penataan ulang tidak menambah panel, listener, atau sprite tanpa batas.
3. Jalankan pemeriksaan eksplorasi yang disesuaikan: delapan arah, pointer clamp/release, reset saat fokus/menu, jalan map, serta HUD dari nilai model. Tambahkan pemeriksaan kecil fade hanya sesudah gerak berhasil.
4. Jalankan pemeriksaan diagnosis/plasma yang ada: entry, separation, interactions, visible hit targets setelah satu frame, feedback layout, impact, serta Homeostasis. Perbarui assertion tampilan yang memang berubah, tanpa melemahkan batas state/biaya/ukuran dock.
5. Periksa layout landscape 1600 × 900, 1920 × 1080, 1280 × 720, dan satu rasio lebar seperti 20:9. Pastikan judul, angka, objektif, deskripsi A/B, enam label, dan feedback tidak terpotong; kontrol sentuh tidak bertabrakan safe area.
6. Catat click-through: START ke map; SETTINGS buka/slider/simpan/tutup; CREDITS buka/tutup; MENU; analog; keyboard/klik map; kedua encounter; scan/reopen; palette tap; drag/drop; lepas/ganti soket; charge batal/restart; fire disabled/ready; Restore; Homeostasis; hasil; kembali ke map; menang/kalah dan retry.
7. Ambil screenshot aktual title, map, battle, diagnosis, synthesis kosong/ready, settings, dan dialog hasil. Bandingkan dengan brief, periksa kontras komposit, dan jalankan Delivery Gate antislop sebelum penyerahan implementasi.
8. Laporkan bukti yang benar-benar dijalankan. APK/perangkat Android fisik hanya diklaim teruji bila benar-benar diuji; ukuran/kontras desktop tidak membuktikan kenyamanan sentuh di hardware.

## Keputusan implementer dan risiko

Keputusan implementer: mempertahankan uGUI, font dan aset yang ada; memilih START hijau medis dan Synthesize oranye; mempertahankan teks SETTINGS/MENU; mempertahankan batas drawer dan geometri sentuh analog; menampilkan ulang petunjuk hanya pada sesi baru. Semua keputusan ini proposal yang dapat dikoreksi pengguna.

Risiko utama: skin lama diterapkan kembali oleh refresh atau jalur baked; teks diagnosis/feedback yang panjang terpotong ketika ikon diperbesar; dekorasi mencegat raycast; hover/pressed/fokus menggeser hit target; dan perubahan input tidak sengaja mengubah kapan charge/biaya/aksi tersedia. Pemeriksaan di atas harus membuktikan batas-batas tersebut tetap.

Detail yang diserahkan kepada implementer: nilai spacing akhir, implementasi geometry rounded/dashed paling kecil yang bekerja, penempatan angka di samping bar, serta penyesuaian anchors pada rasio layar. Tidak ada keputusan gameplay yang diserahkan untuk diubah.

## Pemeriksaan rancangan sebelum persetujuan

Reviewer terpisah membaca dokumen dan kode terkait, lalu melaporkan `nothing important left`: batas visual, perilaku, jalur baked/runtime, serta kriteria penerimaan cukup untuk menyusun rencana. Ini pemeriksaan rancangan, bukan hasil uji Unity.

Perhitungan kontras sRGB mengikuti panduan antislop-human: teks `#F8FAFC` pada panel `#0F1C2E` di atas putih menghasilkan 11,48:1 pada opacity 88%, 13,09:1 pada 92%, dan 7,25:1 pada nameplate 75%. Teks `#0F1C2E` pada hijau `#22C55E` menghasilkan 7,52:1; pada cyan `#06B6D4`, 7,06:1. Angka ini hanya memverifikasi pasangan warna proposal tersebut. Pasangan lain, fokus, disabled, dan hasil render aktual masih harus diperiksa saat implementasi.
