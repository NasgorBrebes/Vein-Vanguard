# Bukti refactor UI/UX Vein Vanguard

Tanggal: 5 November 2026. Scene: `Assets/Scenes/VeinVanguard.unity`.
Unity 6000.3.9f1, Windows Editor, branch `ui/cartoon-hud-refactor`.

Hasil suite terakhir: **42 PASS, 0 FAIL, 25 screenshot**. Log lengkap tersimpan di [ui-ux-refactor-results.txt](ui-ux-refactor-results.txt). Screenshot berasal dari Game View yang berjalan, bukan mockup.

## Penerimaan rancangan, poin 1–8

| Poin | Hasil dan bukti |
| --- | --- |
| 1. Compile, console, aturan model | PASS: Assembly-CSharp dan Editor berhasil dikompilasi; `VerifyRules()` mencakup kedua encounter, input invalid, diagnosis, formula 0/20/40, biaya sekali, fase/giliran/reset, recharge, dan parry. Pipeline `console_status` melaporkan `compilationFailed=false`, `compiling=false`, `consoleErrors=0`. |
| 2. Baked dan fresh | PASS: bake dua kali tidak menambah node. Fixture fresh menyalin referensi aset pada instance terisolasi, menjalankan Awake/build, START, scan, synthesis, serta hit-test; penataan ulang tidak menduplikasi node dan cleanup mengembalikan satu EventSystem. Scene utama disimpan lewat Editor sebelum Play Mode. |
| 3. Eksplorasi | PASS: delapan arah, 72 frame, pivot/flip/idle, clamp diagonal, deadzone, gerak parsial, pointer ownership dan release. Hint tampil di sesi baru, memudar setelah perpindahan berhasil, tetap hilang setelah kembali dari battle, lalu pulih saat reset sesi. |
| 4. Diagnosis dan plasma | PASS: entry, pemisahan drawer, scan tersimpan, tap/drag/drop, ikon berulang, input null/asing, charge batal/ganti/tutup/buka, hit target, teks feedback, impact, lethal, dan Homeostasis. Verifikasi aset meloloskan 27 sprite dengan konfigurasi impor tetap. |
| 5. Landscape | PASS: 1600×900, 1920×1080, 1280×720, 2000×900 (20:9). Bounds teks title/map/battle/drawer diperiksa; HP/MP dan reaktor minimal 16 unit Canvas. Kedua drawer tetap kurang dari 36% tinggi layar, berada di bawah, dan tidak menutupi actor. Keenam label item tetap lengkap. |
| 6. Interaksi | PASS: click-through melalui EventSystem/GraphicRaycaster nyata untuk menu, modal, aksi, kuis, hasil, menang/kalah, dan retry. Keyboard diuji dengan state event Input System, melalui pembacaan input produksi. Klik mouse OS pada START juga diamati membuka map. Rincian di tabel berikut. |
| 7. Screenshot dan antislop | PASS: 25 PNG, termasuk title, map, battle, diagnosis, synthesis kosong/ready pada empat ukuran, hasil pada empat ukuran, settings, credits, Restore benar/salah, parry, kemenangan, misi lengkap, map selesai, dan defeat. Gambar telah diperiksa; perbaikan label soket, margin reaktor, serta thumb slider diulang lewat regression checks. |
| 8. Batas bukti | PASS untuk pelaporan yang terukur: suite berjalan di Windows Editor. APK, Android fisik, safe area perangkat nyata, dan kenyamanan sentuh di hardware belum diuji. Tidak ada klaim performa/FPS atau kepatuhan aksesibilitas menyeluruh. |

Pipeline `wait_for` membuktikan player loop bergerak: `Time.frameCount` berubah dari 10236 ke 10286, `met=true`, `framesObserved=2`. Bukti ini memastikan capture bukan sekadar frame Editor yang membeku.

Console memiliki satu jenis warning dari paket Unity AI Assistant: `Account API did not become accessible within 30 seconds` (dua entri). Stack berasal dari `Unity.AI.Toolkit.Accounts`, bukan script game. Tidak ada exception/error game pada run terakhir. Konfigurasi akun/paket tersebut tidak diubah.

## Click-through yang dijalankan

| Kontrol/alur | Hasil yang diamati |
| --- | --- |
| START | Membuka World; juga berhasil lewat klik mouse OS. |
| SETTINGS pada title/map | Membuka modal; Musik/Efek suara mengubah AudioSource dan tersimpan ketika dibuka kembali; nilai semula dipulihkan setelah uji. |
| KEMBALI / CREDITS | Modal menutup; semua nama credits tetap muat. |
| MENU pada map | Kembali ke title. |
| Analog | Clamp, deadzone, kecepatan parsial, delapan arah, pelepasan dan pointer lain diperiksa melalui event pointer. |
| Keyboard | Event Input System: Tab memfokuskan START; Space memulai; D bergerak; Escape membuka settings; Tab menuju slider SFX; Escape menutup. Binding Enter bawaan dan submit handler dipertahankan. |
| DIAGNOSE / TUTUP / buka ulang | Scan tunggal membuka rekaman A/B; kategori mengikuti hasil; tidak membuka synthesis atau memakai energi/giliran. |
| SYNTHESIZE | Membuka dock sendiri; enam item, dua soket, tap dan drag/drop berfungsi; hit target diperiksa setelah frame render. |
| Soket/charge | Isi/lepas/ganti, ikon sama di kedua soket, tutup/buka, invalid/foreign drag; charge tetap tidak menunjukkan benar/salah formula. |
| Fire | Disabled saat belum siap, enabled setelah charge dan energi cukup; biaya/damage hanya pada impact, tembakan ganda terkunci. |
| Restore / jawaban / LANJUT | Jawaban benar +40 MP, salah +10 MP, lalu giliran musuh. Kedua feedback dicapture. |
| Homeostasis | Tombol memicu parry dan pantulan pertama; satu hit pertahanan tersisa setelah serangan. |
| Kemenangan / KEMBALI KE PETA | Kedua encounter diuji dengan fixture musuh 1 HP agar transisi cepat; flag defeated dan misi lengkap tetap benar. |
| Kekalahan / COBA LAGI | Fixture pemain 1 HP menghasilkan defeat; retry memulihkan sesi ke 100 HP dan 60 MP. |

Click-through otomatis memeriksa routing pointer sebelum memanggil event, bukan hanya memanggil fungsi BattleManager. Keyboard memakai event yang diantrekan ke Input System; ini tidak membuktikan keyboard fisik pada perangkat pengguna. Input keyboard OS melalui helper desktop tidak memberikan perubahan yang dapat diverifikasi, sehingga tidak diklaim sebagai uji keyboard fisik yang lulus.

## Kontras dan regresi visual

Empat tombol battle memakai teks `#F8FAFC` pada surface gelap. Assertion memeriksa kedua baris melalui warna label yang sama, tanpa override warna rich-text: rasio minimal 7:1 pada normal, hover/fokus, dan disabled. Perhitungan dengan alpha di atas putih memberikan sekitar 14,25:1 normal, 11,59:1 hover, dan 11,51:1 disabled untuk surface aksi yang dipakai.

Teks panel `#F8FAFC` di atas `#0F1C2E` menghasilkan 11,48:1 pada opacity 88%, 13,09:1 pada 92%, dan 7,25:1 pada nameplate 75%, dengan putih sebagai latar terburuk. Angka ini perhitungan pasangan warna sRGB, bukan hasil pengukuran semua piksel screenshot.

Regresi yang sempat gagal lalu diperbaiki:

- Label A/B pada soket melebihi area baca: margin dibuang dan tinggi area label ditambah.
- Label reaktor overflow pada 20:9: margin vertikal dikurangi tanpa mengubah teks.
- Keyboard submit tidak menekan bevel: `ISubmitHandler` menyalakan feedback singkat dengan waktu unscaled, tanpa memanggil aksi kedua kali. Regression check meloloskan keadaan pressed.
- Narasi terlihat di belakang drawer transparan: panel narasi disembunyikan selama dialog/drawer dan dipulihkan saat ditutup.
- Tombol dialog tunggal terlalu lebar: ukuran dibatasi 330×56 unit.
- Slider meregangkan thumb menjadi 30×75,1: travel area dibuat ber-height nol sehingga thumb tetap bulat, sementara area sentuh slider tetap sekitar 45 unit. Assertion menguji dimensi hasil layout, bukan hanya sizeDelta.

## Delivery Gate antislop

Laporan ini menilai UI Unity yang diubah. Pemeriksaan DOM, ARIA, CSS, browser zoom dan Core Web Vitals tidak berlaku pada Canvas native dan tidak diklaim lulus sebagai fitur web.

| Gate | Status dan alasan |
| --- | --- |
| R-02, R-16: copy | PASS: teks pengguna/konten game dipertahankan; tidak menambahkan copy promosi. Em dash yang sudah ada pada objektif selesai dipertahankan sesuai instruksi pengguna untuk menjaga teks. |
| R-03, R-25: layout/kontras | PASS dalam empat ukuran Editor yang diuji: bounds teks dan hit target lolos; warna aksi dihitung dan di-assert. Android fisik tetap di luar bukti. |
| R-17, R-18, R-23, R-36, R-38: konten/aset | PASS: angka berasal dari model atau fixture uji yang dijelaskan; karakter, ikon, map, font, dan materi lama dipakai ulang. Bentuk UI dibuat melalui mesh uGUI sesuai brief. Tidak ada statistik/ilustrasi/klaim rekaan. |
| R-24, R-26: kontrol | PASS: menu dan tindakan yang diubah memiliki callback nyata; raycast memeriksa routing sebelum klik; disabled tetap mengikuti model. |
| R-27: state | PASS: scan, hasil tersimpan, soket kosong/terisi, charge, energi kurang, drag, hasil benar/sebagian/salah, menang/kalah dan retry tetap tersedia. Tidak membuat loading/error state rekaan pada data lokal. |
| R-28: FAQ | PASS: tidak menambahkan FAQ atau halaman yang tidak diminta. |
| R-32, R-39: interaksi | PASS dalam event pipeline Editor yang diuji: fokus terlihat, Tab dibatasi root aktif, Space/submit dan Escape bekerja, slider berlabel, tap menjadi alternatif drag. Tidak mengklaim pembaca layar atau keyboard/touch fisik. |
| R-33: implementasi sumber | PASS: script C# diubah langsung; scene disimpan lewat Editor; tidak ada script pengganti string CSS/UI yang ditinggalkan. |
| R-34, R-37: arah/tema | PASS: menggunakan brief yang disetujui dan During; fixed dark surface merupakan permintaan pengguna. Tidak menambahkan toggle tema. |
| R-35: bukti eksekusi | PASS: 42 hasil PASS, nol FAIL, player loop bergerak, console nol error, 25 screenshot, dan tabel click-through tersedia. |
| R-40: klaim performa | PASS: tidak mengklaim angka FPS, profiling atau metrik web; asset import dan CanvasScaler lama dipertahankan. |
| R-01, R-04, R-06, R-07, R-08, R-09 | PASS: warna mengikuti fungsi medis, ikon berasal dari aset game, font dipertahankan, badge menampilkan state nyata. Tidak menambahkan grid latar atau panah dekoratif. |
| R-10, R-12, R-13, R-14, R-19, R-22 | PASS: panel navy tanpa blur; shadow solid mengikuti outline karakter; pulse hanya soket kosong; reaktor mengikuti charge; ilustrasi tetap terkait game. Bentuk/pulse/warna semantik mengikuti instruksi eksplisit pengguna. |
| Liveliness | PASS: ENERGY 3 / RHYTHM 2 / MOTION 2; START, arena dan rakitan plasma mempunyai fokus berbeda. Border tegas, ikon medis, dan surface navy menjaga identitas kartun medis. |
| C-1–C-5, R-05, R-11, R-15, R-20, R-21, R-29–R-31 | PASS dalam cakupan verifikasi: alasan desain ada di spec; panel dan tombol mempunyai bentuk/peran berbeda; fitur dan teks tetap; aksen beragam punya fungsi, sesuai brief. |

## Review dan keputusan eksekusi

Reviewer terpisah memeriksa kode dan screenshot, menemukan satu isu penting pada feedback keyboard submit. Perbaikan tersebut telah melewati RED→GREEN dan suite lengkap. Tidak ada temuan minor yang ditangguhkan dari review.

Keputusan yang diambil: memakai checkout Unity yang sudah diminta pengguna pada branch lokal; melanjutkan langsung setelah persetujuan rencana+eksekusi; menyimpan ledger di rencana untuk workflow Windows. Tidak membuat worktree baru, mengubah materi/model, memasang dependensi, mengunggah, atau menggabungkan ke main. Biayanya: isolasi berada pada branch dan disiplin scope, bukan direktori terpisah.

CLI yang terpasang dipakai untuk verifikasi lokal; tidak memperbarui instalasi CLI global karena pembaruan bukan bagian refactor. Bukti final dapat direproduksi dari menu `Tools > Vein Vanguard > UI Refactor > Run Acceptance and Capture` di Edit Mode. Runner menyimpan scene dan memulai sesi uji baru.

## Screenshot pilihan

- [Battle](ui-ux-refactor/battle-1600x900.png)
- [Map](ui-ux-refactor/map-1600x900.png)
- [Diagnosis](ui-ux-refactor/diagnosis-1600x900.png)
- [Synthesis siap, 1280×720](ui-ux-refactor/synthesis-ready-1280x720.png)
- [Synthesis siap, 20:9](ui-ux-refactor/synthesis-ready-2000x900.png)
- [Settings](ui-ux-refactor/settings-1600x900.png)
- [Hasil](ui-ux-refactor/result-1600x900.png)

APK dan perangkat Android fisik belum diuji; skala sentuh, notch, serta pembacaan di layar hardware tetap membutuhkan pengujian perangkat.
