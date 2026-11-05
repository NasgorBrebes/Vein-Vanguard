# Vein Vanguard UI/UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Terapkan rancangan UI kartun yang disetujui tanpa mengubah aturan, konten, atau state game.

**Architecture:** Pertahankan BattleManager partial, Unity uGUI, dan TMP. Satu jalur penataan bersama diterapkan pada fresh build dan scene baked; kontrol memakai geometry rounded native uGUI, dengan komponen kecil khusus feedback tombol/soket. Pemeriksaan Editor memakai model dan EventSystem nyata serta screenshot Game View.

**Tech Stack:** Unity 6000.3.9f1, UnityEngine.UI, TextMeshPro, Input System, Editor checks yang ada. Tanpa dependensi baru.

**Spec:** `docs/superpowers/specs/2026-11-05-ui-ux-refactor-design.md`, disetujui pengguna 5 November 2026, ditambah tiga catatan pengawalan pada percakapan.

## Global Constraints

- Seluruh teks, fungsi, konten pembelajaran, dan aturan gameplay dipertahankan.
- Panel `#0F1C2E` opacity 88–92%, border `#2D6A8E` 2 unit, radius 12–16, shadow solid `#08101A` offset 4.
- Label utama dan subteks empat aksi memakai surface gelap dengan teks `#F8FAFC`; kontras pasangan tersebut diverifikasi bersama keadaan disabled.
- Drawer kurang dari 36% tinggi layar; enam item 3 × 2 dengan padding ringkas dan label lengkap.
- Scene baked dan fresh UI harus setara; tidak menyentuh file pengguna yang belum terlacak.
- Pengguna telah mengizinkan rencana dan eksekusi dalam satu giliran: lanjutkan tanpa gerbang persetujuan tambahan.

## Review Focus

- Refresh atau wire ulang tidak boleh mengembalikan skin lama atau menggandakan dekorasi/listener: uji idempotensi dan jalur baked/fresh pada Task 1/4.
- Label panjang dalam dock pada rasio sempit tetap muat: text bounds semua TMP pada Task 3/4.
- Geometri dekoratif tidak boleh menghalangi input: hit-test EventSystem nyata pada Task 2/3/4.
- Mouse/keyboard/touch tetap setara; fade hanya setelah perpindahan berhasil, reset hanya sesi baru: uji gerak/fade pada Task 2.
- Tampilan siap plasma tidak boleh membocorkan jawaban atau mengubah biaya/impact: jalankan checks charge/impact/0–20–40 pada Task 3/4.

### Task 1: Dasar skin, title, dan battle

**Files:** Create `Assets/Game/OutlinedUiGraphic.cs`, `Assets/Game/UiControlFeedback.cs`, `Assets/Game/UiPresentation.cs`, `Assets/Game/Editor/UiRefactorChecks.cs`. Modify `BattleManager.cs`, `WorldFlow.cs`.

**Interfaces:** `OutlinedUiGraphic : BaseMeshEffect` menghasilkan rounded fill/border/dashed border lewat VertexHelper sambil mempertahankan referensi Image terserialisasi; `UiControlFeedback : MonoBehaviour` menangani feedback Selectable tanpa memindahkan hit target. `BattleManager.ApplyPresentation()` menata ulang node yang ada secara idempotent; `UiRefactorChecks` menyediakan menu QA untuk Editor/Play Mode.

- [x] Tambahkan pemeriksaan Editor dengan assertion panel gelap, tombol kontras, bar >=16 unit, title width <=280, dan tanpa duplikasi setelah bake dua kali; jalankan pada scene lama dan catat kegagalan yang sesuai.
- [x] Implementasikan geometry dan skin panel/button bersama, title proporsional, battle padat, badge lokasi/shield, dan narasi gelap. Terapkan dari jalur fresh serta baked.
- [x] Jalankan pemeriksaan Task 1 dan compile; periksa hasil gambar sebelum meneruskan.

### Task 2: Eksplorasi dan settings

**Files:** Modify `UiPresentation.cs`, `WorldFlow.cs`, `Editor/PrototypeBuilder.cs`, `Editor/UiRefactorChecks.cs`.

**Interfaces:** `ApplyPresentation()` menata HUD map/joystick/modal yang sama; `UpdateMovementHint()` mengubah alpha berdasar gerak yang berhasil, dengan reset di StartExploration. Setter volume dan ExplorationJoystick tidak berubah.

- [x] Tambahkan uji fade (tidak bergerak/bergerak/kembali dari battle/reset), floating HUD, nameplates, settings <=460, slider thumb bulat; sesuaikan assertion joystick lama yang bertentangan dengan brief.
- [x] Terapkan floating HUD, bar/angka real-time, nameplate, joystick bulat, hint fade, settings/credits gelap, track rounded dan thumb bulat. Pertahankan teks SETTINGS/MENU.
- [x] Jalankan VerifyExploration dan checks baru; verifikasi hit-test, menu, focus/reset dan volume persistensi.

### Task 3: Diagnosis dan plasma

**Files:** Modify `DiagnosticWorkbench.cs`, `UiPresentation.cs`, `UiControlFeedback.cs`, `Editor/UiRefactorChecks.cs`; gunakan `PlasmaWorkbenchChecks.cs` yang ada.

**Interfaces:** `StyleLearningWorkbench()` dipanggil setelah konstruksi; refresh hanya mengubah state yang diperlukan. Badge kategori bersumber dari CombatSamples, terlihat setelah diagnosis. Empty socket pulse dibatasi pada state configure, charge mengikuti nilai lama.

- [x] Tambahkan assertion ukuran ikon/label, kategori belum muncul saat scan, reaktor >=16, serta empty/filled/disabled/ready; jalankan kegagalan visual pada implementasi lama.
- [x] Tata profil A/B, inventori 3 × 2, socket dashed/pulse dan filled, bar charge, fire kontras. Jangan mengubah coroutine atau aturan formula.
- [x] Jalankan entry, separation, interactions, visible hit targets, feedback layout, asset checks, serta impact dan Homeostasis.

### Task 4: Sinkronisasi scene dan bukti penerimaan

**Files:** Modify `Assets/Scenes/VeinVanguard.unity` melalui Editor save; modify README menu/diagnosis; create laporan `docs/superpowers/verification/2026-11-05-ui-ux-refactor.md` dan PNG pada folder sibling `ui-ux-refactor/`.

**Interfaces:** Menu Editor QA melakukan bake/save, Play Mode checks, dan capture Game View pada ukuran yang ditetapkan. Catatan bukti membedakan event callback/raycast otomatis dari input OS manual.

- [x] Bake dua kali dan simpan scene; pastikan tidak ada missing reference, komponen duplikat, atau perubahan aset/model tak terkait. Periksa jalur fresh pada instance test terisolasi, lalu pulihkan scene.
- [x] Jalankan pemeriksaan penerimaan 1–8 pada spec, termasuk ukuran 1600×900, 1920×1080, 1280×720, dan 20:9. Catat seluruh hasil, bukan hanya hasil yang lolos.
- [x] Capture title, map, battle, diagnosis, synthesis kosong/ready, settings, credits, hasil, serta screenshot rasio lain; baca kembali screenshot dan koreksi layout jika diperlukan.
- [x] Reviewer konteks baru memeriksa diff terhadap spec; perbaiki temuan penting dan jalankan pemeriksaan yang terkait.
- [x] Perbarui dokumentasi, laporan antislop, dan ledger. Penyerahan menyebutkan keterbatasan Android fisik/APK yang belum diuji dan lokasi bukti.

## Ledger

- Setup: branch `ui/cartoon-hud-refactor`; kerjakan checkout Unity yang sedang terbuka agar scene yang diminta pengguna benar-benar tersinkron. Tidak membuat worktree tambahan atau memindahkan file pengguna.
- Pre-flight: Task 2/3 memakai skin dan ApplyPresentation Task 1; Task 4 memverifikasi semua jalur. Tidak ada konflik interface.
- Ruling: eksekusi langsung sudah diminta eksplisit pengguna; tidak meminta review plan lagi. Rencana dapat dibaca sambil implementasi berjalan.
- Ruling: gunakan laporan/ledger ini sebagai catatan persisten di Windows; tidak menjalankan helper Bash workflow yang tidak tersedia pada shell proyek.
- Task 1: complete — skin RED→GREEN, high-contrast actions, title/battle, native rounded geometry.
- Task 2: complete — exploration/volume/joystick/fade checks; circle-thumb regression RED→GREEN.
- Task 3: complete — diagnosis/plasma/impact/charge/input and layout checks passed on four sizes.
- Task 4: complete — scene saved via Editor, fresh/baked verified, 42 PASS / 0 FAIL, 25 screenshots and persistent report/log.
- Final review: one important keyboard-submit feedback issue fixed RED→GREEN; full suite green; no deferred minors.
- Ruling: keep the local feature branch for user review; no merge, push, or extra integration approval flow is needed for the requested local implementation.
