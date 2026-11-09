using System;

namespace VeinVanguard
{
    public static class EncounterCatalog
    {
        public const int Count = 6;
        public static int MaxHp(int id)
        {
            switch (id)
            {
                case 0: return 100;
                case 1: return 130;
                case 2: return 130;
                case 3: return 130;
                case 4: return 130;
                case 5: return 160;
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
        public static int IncomingDamage(int id)
        {
            switch (id)
            {
                case 0: return 14;
                case 1: return 18;
                case 2: return 18;
                case 3: return 18;
                case 4: return 18;
                case 5: return 18;
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
        public static string Title(int id)
        {
            switch (id)
            {
                case 0: return "LIPID GOLEM";
                case 1: return "GLUCO SLIME";
                case 2: return "ROKOK";
                case 3: return "BURGER DAN KENTANG";
                case 4: return "BOTOL ALKOHOL";
                case 5: return "BOSS";
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
    }

    public enum BattlePhase { Menu, Diagnose, Player, Trivia, Feedback, Enemy, Victory, Defeat, Complete }

    [Serializable]
    public class BattleModel
    {
        public int hp = 100, energy = 60, enemyHp, encounter, shield, correct, answered, turn;
        public int shieldHits;
        public int lastReflectedDamage;
        public bool diagnosed;
        int scannedSamples, classifiedSamples;
        public BattlePhase phase = BattlePhase.Menu;
        public int MaxEnemyHp => EncounterCatalog.MaxHp(encounter);
        public int ClassifiedCount => (IsClassified(0) ? 1 : 0) + (IsClassified(1) ? 1 : 0);
        public bool CanAttack => phase == BattlePhase.Player && diagnosed && classifiedSamples == 3 && scannedSamples == 3 && energy >= 20;
        public void Begin(int index)
        {
            if (index < 0 || index >= EncounterCatalog.Count) throw new ArgumentOutOfRangeException(nameof(index));
            encounter = index; enemyHp = MaxEnemyHp; diagnosed = false; turn = 1;
            scannedSamples = classifiedSamples = 0;
            shield = shieldHits = 0; lastReflectedDamage = 0;
            phase = BattlePhase.Diagnose;
        }
        public bool Diagnose()
        {
            if (phase != BattlePhase.Diagnose || scannedSamples != 3 || classifiedSamples != 3) return false;
            diagnosed = true; phase = BattlePhase.Player; return true;
        }
        public bool DiagnoseAll()
        {
            if (phase != BattlePhase.Diagnose) return false;
            scannedSamples = classifiedSamples = 3;
            return Diagnose();
        }
        public bool WasScanned(int target) => target >= 0 && target < 2 && (scannedSamples & (1 << target)) != 0;
        public bool IsClassified(int target) => target >= 0 && target < 2 && (classifiedSamples & (1 << target)) != 0;
        public bool ScanSample(int target)
        {
            if (phase != BattlePhase.Diagnose || target < 0 || target > 1 || WasScanned(target)) return false;
            scannedSamples |= 1 << target; return true;
        }
        public bool ClassifySample(int target, SampleClass choice)
        {
            if (phase != BattlePhase.Diagnose || !WasScanned(target) || IsClassified(target) ||
                !Enum.IsDefined(typeof(SampleClass), choice) || CombatSamples.Get(encounter, target).kind == SampleClass.Data || CombatSamples.Get(encounter, target).kind != choice) return false;
            classifiedSamples |= 1 << target; return true;
        }
        public bool CanSynthesize(int target, SynthesisModule module, SourceAdapter adapter)
        {
            if (!CanAttack || !IsClassified(target) || (int)module < 0 || (int)module > 2 ||
                CombatSamples.Get(encounter, target).kind == SampleClass.Data) return false;
            var kind = CombatSamples.Get(encounter, target).kind;
            return kind == SampleClass.SaturatedFat ? adapter == SourceAdapter.ReduceButter || adapter == SourceAdapter.UnsaturatedOil :
                kind == SampleClass.FreeSugar && (adapter == SourceAdapter.ReduceSweetener || adapter == SourceAdapter.WholeFruit);
        }
        public int Attack(int target, SynthesisModule module, SourceAdapter adapter)
        {
            if (!CanSynthesize(target, module, adapter)) return -1;
            var sample = CombatSamples.Get(encounter, target);
            int damage = module != sample.module ? 0 : adapter == sample.adapter ? 40 : 20;
            energy -= 20; enemyHp = Math.Max(0, enemyHp - damage);
            phase = enemyHp == 0 ? BattlePhase.Victory : BattlePhase.Enemy;
            return damage;
        }
        public bool CanSynthesizeFormula(FormulaIcon first, FormulaIcon second) => CanAttack && UniversalFormula.Complete(first, second) &&
            UniversalFormula.Contains(encounter, first) && UniversalFormula.Contains(encounter, second);
        public int AttackFormula(FormulaIcon first, FormulaIcon second)
        {
            if (!CanSynthesizeFormula(first, second)) return -1;
            int damage = UniversalFormula.Damage(encounter, first, second);
            energy -= 20; enemyHp = Math.Max(0, enemyHp - damage);
            phase = enemyHp == 0 ? BattlePhase.Victory : BattlePhase.Enemy;
            return damage;
        }
        public bool Restore()
        {
            if (phase != BattlePhase.Player || energy >= 60) return false;
            phase = BattlePhase.Trivia; return true;
        }
        public bool Answer(bool isCorrect)
        {
            if (phase != BattlePhase.Trivia) return false;
            answered++;
            if (isCorrect) { correct++; energy = Math.Min(60, energy + 40); }
            else energy = Math.Min(60, energy + 10);
            phase = BattlePhase.Feedback; return true;
        }
        public bool Homeostasis(int cost = 10, int reduction = 50, int hits = 2)
        {
            if (phase != BattlePhase.Player || !diagnosed || shieldHits > 0 || cost < 0 || energy < cost || reduction < 1 || reduction > 100 || hits < 1) return false;
            energy -= cost; shield = reduction; shieldHits = hits; phase = BattlePhase.Enemy;
            return true;
        }
        public int EnemyAttack()
        {
            if (phase != BattlePhase.Enemy) return -1;
            int incoming = EncounterCatalog.IncomingDamage(encounter);
            int damage = incoming;
            lastReflectedDamage = 0;
            if (shieldHits > 0)
            {
                // First protected hit is reflected; the second is simply parried.
                if (shieldHits == 2)
                {
                    enemyHp = Math.Max(0, enemyHp - incoming);
                    lastReflectedDamage = incoming;
                }
                damage = 0;
                if (--shieldHits == 0) shield = 0;
            }
            hp = Math.Max(0, hp - damage); turn++;
            phase = enemyHp == 0 ? BattlePhase.Victory : hp == 0 ? BattlePhase.Defeat : BattlePhase.Player;
            return damage;
        }
    }

    public class TriviaQuestion
    {
        public string prompt, explanation;
        public string[] options;
        public int answer;
        public TriviaQuestion(string p, string[] o, int a, string e) { prompt = p; options = o; answer = a; explanation = e; }
    }
    // Each encounter has its own shuffled deck. A question repeats only after
    // every question for that monster has been used.
    public sealed class TriviaDeck
    {
        readonly System.Random random;
        readonly System.Collections.Generic.List<int>[] remaining = {
            new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>(),
            new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>(),
            new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>()
        };
        readonly int[] last = { -1, -1, -1, -1, -1, -1 };
        public TriviaDeck() : this(new System.Random()) { }
        public TriviaDeck(System.Random random) { this.random = random; }
        public TriviaQuestion Next(int encounter)
        {
            var bank = TriviaBank.ForEncounter(encounter);
            var pool = remaining[encounter];
            if (pool.Count == 0)
            {
                for (int i = 0; i < bank.Length; i++) pool.Add(i);
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    int value = pool[i]; pool[i] = pool[j]; pool[j] = value;
                }
                if (pool.Count > 1 && pool[pool.Count - 1] == last[encounter])
                {
                    int value = pool[0]; pool[0] = pool[pool.Count - 1]; pool[pool.Count - 1] = value;
                }
            }
            int selected = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1);
            last[encounter] = selected;
            var source = bank[selected];
            var choices = (string[])source.options.Clone();
            int answer = source.answer;
            for (int i = choices.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string value = choices[i]; choices[i] = choices[j]; choices[j] = value;
                if (answer == i) answer = j;
                else if (answer == j) answer = i;
            }
            return new TriviaQuestion(source.prompt, choices, answer, source.explanation);
        }
    }

    public static class TriviaBank
    {
        public static TriviaQuestion[] ForEncounter(int encounter)
        {
            switch (encounter)
            {
                case 0: return LipidQuestions;
                case 1: return GlucoQuestions;
                case 2: return TobaccoQuestions;
                case 3: return BurgerQuestions;
                case 4: return AlcoholQuestions;
                case 5: return BossQuestions;
                default: throw new ArgumentOutOfRangeException(nameof(encounter));
            }
        }
        public static readonly TriviaQuestion[] TobaccoQuestions = {
            new TriviaQuestion("Zat tembakau yang menyebabkan ketergantungan adalah ...", new[]{"Nikotin", "Karbon monoksida", "Kadmium"}, 0, UniversalFormula.Fact(FormulaIcon.Nicotine)),
            new TriviaQuestion("Gas asap yang mengganggu pengangkutan oksigen adalah ...", new[]{"Nikotin", "Karbon monoksida", "Benzena"}, 1, UniversalFormula.Fact(FormulaIcon.CarbonMonoxide)),
            new TriviaQuestion("Tar dalam asap tembakau merupakan ...", new[]{"Satu enzim", "Campuran partikel", "Organ"}, 1, UniversalFormula.Fact(FormulaIcon.TobaccoTar)),
            new TriviaQuestion("Manakah logam pada paket data tembakau?", new[]{"Tar", "Kadmium", "Formaldehida"}, 1, UniversalFormula.Fact(FormulaIcon.Cadmium)),
            new TriviaQuestion("Rekaman meminta gas pengganggu pengangkutan oksigen. Apakah kartu Nikotin sesuai?", new[]{"Ya, semua zat asap sama", "Tidak, rekaman meminta karbon monoksida", "Ya, nikotin adalah enzim"}, 1, "Rekaman meminta gas yang mengganggu pengangkutan oksigen: karbon monoksida."),
            new TriviaQuestion("Apakah menyebut nikotin saja sudah menjelaskan seluruh zat berbahaya dalam asap?", new[]{"Tidak, asap mengandung banyak zat lain", "Ya, asap hanya nikotin", "Ya, tar adalah nama lain nikotin"}, 0, "Nikotin dan komponen asap lain memiliki peran yang berbeda.")
        };
        public static readonly TriviaQuestion[] BurgerQuestions = {
            new TriviaQuestion("Olesan yang disebut pada rekaman burger adalah ...", new[]{"Mentega", "Sirup", "Jus"}, 0, "Catatan resep menyebut olesan mentega."),
            new TriviaQuestion("Minyak yang disebut pada rekaman kentang adalah ...", new[]{"Kanola", "Minyak kelapa", "Tidak ada minyak"}, 1, "Catatan resep menyebut penggorengan dengan minyak kelapa."),
            new TriviaQuestion("Mengurangi olesan mentega mengubah ...", new[]{"Jenis lemak menjadi tak jenuh", "Jumlah olesan", "Mentega menjadi air"}, 1, "Mengurangi bahan yang ditambahkan langsung menyasar sumber pada rekaman. Jumlah bahan berkurang; jenis lemak atau gulanya tidak berubah."),
            new TriviaQuestion("Data penggantian kualitas lemak adalah ...", new[]{"Lemak tak jenuh", "Air putih", "Buah utuh"}, 0, "Penggantian menyasar kualitas lemak: gunakan pilihan kaya lemak tak jenuh, misalnya minyak kanola, kedelai, atau bunga matahari."),
            new TriviaQuestion("Mengukus kentang otomatis mengubah lemak jenuh menjadi tak jenuh?", new[]{"Ya", "Tidak", "Hanya jika ditambah mentega"}, 1, "Mengukus atau merebus dapat mengurangi minyak yang digunakan. Ini tidak otomatis mengubah lemak jenuh menjadi lemak tak jenuh, sehingga hasil formula masih sebagian."),
            new TriviaQuestion("Dasar menentukan jawaban pada battle Burger adalah ...", new[]{"Bentuk monster saja", "Bahan dan cara memasak pada rekaman", "Warna kartu"}, 1, "Gunakan catatan resep dalam rekaman, bukan menebak dari gambar musuh.")
        };
        public static readonly TriviaQuestion[] AlcoholQuestions = {
            new TriviaQuestion("Alkohol dalam minuman beralkohol disebut ...", new[]{"Etanol", "Asetat", "ALDH"}, 0, UniversalFormula.Fact(FormulaIcon.Ethanol)),
            new TriviaQuestion("Hasil penguraian etanol oleh ADH adalah ...", new[]{"Asetaldehida", "Nikotin", "Kadmium"}, 0, UniversalFormula.Fact(FormulaIcon.AlcoholDehydrogenase)),
            new TriviaQuestion("ADH termasuk ...", new[]{"Organ", "Enzim", "Hasil reaksi"}, 1, UniversalFormula.Fact(FormulaIcon.AlcoholDehydrogenase)),
            new TriviaQuestion("Enzim yang mengubah asetaldehida menjadi asetat adalah ...", new[]{"ADH", "ALDH", "Etanol"}, 1, UniversalFormula.Fact(FormulaIcon.AldehydeDehydrogenase)),
            new TriviaQuestion("Asetat merupakan ...", new[]{"Hasil penguraian lebih lanjut", "Nama lain nikotin", "Nama organ"}, 0, UniversalFormula.Fact(FormulaIcon.Acetate)),
            new TriviaQuestion("Sebagian besar penguraian alkohol berlangsung di ...", new[]{"Hati", "Tulang", "Kulit"}, 0, UniversalFormula.Fact(FormulaIcon.Liver))
        };
        public static readonly TriviaQuestion[] BossQuestions = CombineBossQuestions();

        static TriviaQuestion[] CombineBossQuestions()
        {
            var questions = new TriviaQuestion[TobaccoQuestions.Length + AlcoholQuestions.Length];
            TobaccoQuestions.CopyTo(questions, 0);
            AlcoholQuestions.CopyTo(questions, TobaccoQuestions.Length);
            return questions;
        }

        public static readonly TriviaQuestion[] LipidQuestions = {
            new TriviaQuestion("Rekaman mentega dan minyak kelapa pada Lipid Golem termasuk sumber apa?", new[]{"Lemak jenuh", "Gula bebas", "Pati"}, 0, "Mentega dan minyak kelapa merupakan sumber lemak jenuh. Rekaman ini menjelaskan sumber makanan dalam skenario game."),
            new TriviaQuestion("Ikon mana mengganti sumber lemak jenuh pada kedua rekaman Lipid Golem?", new[]{"Air putih", "Lemak tak jenuh", "Buah utuh"}, 1, "Ikon Lemak tak jenuh sesuai untuk mentega maupun minyak kelapa karena mengganti sumber lemak jenuh dengan pilihan kaya lemak tak jenuh."),
            new TriviaQuestion("Dalam game, berapa damage jika formula di kedua soket sesuai penuh dengan rekaman A/B?", new[]{"20 damage", "0 damage", "40 damage"}, 2, "Kedua soket yang sesuai penuh menghasilkan 40 damage saat proyektil tiba. Ini aturan game, bukan efek medis."),
            new TriviaQuestion("Dalam game, berapa damage tembakan jika kedua ikon tidak menyasar rekaman A/B?", new[]{"0 damage", "20 damage", "40 damage"}, 0, "Dua ikon valid yang sama-sama tidak menyasar rekaman menghasilkan 0 damage. Tembakan tetap memakai 20 MP saat proyektil tiba."),
            new TriviaQuestion("Label makanan mencantumkan 3 g lemak jenuh per sajian. Berapa dalam 2 sajian?", new[]{"3 gram", "6 gram", "9 gram"}, 1, "3 g x 2 sajian = 6 g lemak jenuh."),
            new TriviaQuestion("Pada porsi sama, camilan A mengandung 2 g lemak jenuh dan B 5 g. Mana lebih rendah?", new[]{"Camilan A", "Camilan B", "Sama"}, 0, "Pada porsi yang sama, 2 g lebih rendah daripada 5 g."),
            new TriviaQuestion("Satu kemasan berisi 4 sajian, masing-masing 5 g lemak total. Berapa lemak seluruh kemasan?", new[]{"5 gram", "10 gram", "20 gram"}, 2, "4 x 5 g = 20 g lemak total. Angka per sajian berbeda dari angka per kemasan."),
            new TriviaQuestion("Label menunjukkan 10 g lemak total, termasuk 3 g lemak jenuh. Berapa lemak selain lemak jenuh?", new[]{"13 gram", "7 gram", "3 gram"}, 1, "10 - 3 = 7 g. Lemak jenuh sudah termasuk dalam angka lemak total."),
            new TriviaQuestion("Produk A memiliki 2 g lemak jenuh per 20 g. Berapa lemak jenuh dalam porsi 40 g?", new[]{"4 gram", "2 gram", "1 gram"}, 0, "Porsi 40 g adalah dua kali 20 g, sehingga 2 x 2 = 4 g."),
            new TriviaQuestion("Minyak pada label mengandung 14 g lemak per sendok. Setengah sendok mengandung berapa?", new[]{"28 gram", "14 gram", "7 gram"}, 2, "14 g dibagi 2 = 7 g lemak untuk setengah ukuran saji."),
            new TriviaQuestion("Produk A: 2 g lemak jenuh per 20 g. B: 3 g per 30 g. Jika porsinya 60 g, bagaimana hasilnya?", new[]{"A lebih tinggi", "Sama: 6 gram", "B lebih tinggi"}, 1, "A: 2 x 3 = 6 g. B: 3 x 2 = 6 g. Samakan porsi sebelum membandingkan."),
            new TriviaQuestion("Lipid Golem memiliki 80 HP. Setiap synthesis lengkap yang sesuai menghasilkan 40 damage. Perlu berapa serangan?", new[]{"2 serangan", "4 serangan", "8 serangan"}, 0, "80 dibagi 40 = 2 serangan, selama damage tiap serangan tetap sama."),
        };
        public static readonly TriviaQuestion[] GlucoQuestions = {
            new TriviaQuestion("Gula pada rekaman sirup dan jus tanpa gula tambahan termasuk kategori apa?", new[]{"Lemak jenuh", "Gula bebas", "Pati"}, 1, "Gula dalam sirup dan jus buah termasuk gula bebas. Tanpa gula tambahan tidak berarti bebas gula."),
            new TriviaQuestion("Rekaman B Gluco Slime menunjukkan jus tanpa gula tambahan. Ikon mana menyasar bentuk pangan ini?", new[]{"Lemak tak jenuh", "Kukus/rebus", "Buah utuh"}, 2, "Buah utuh mengganti sajian jus sesuai bukti bentuk pangan. Air putih juga dapat dipilih untuk mengganti minuman sumber gula bebas. Tanpa gula tambahan tidak berarti bebas gula."),
            new TriviaQuestion("Gluco Slime tersisa 38 HP. Synthesis yang sesuai menghasilkan 40 damage. Berapa HP yang tersisa?", new[]{"0 HP", "2 HP", "18 HP"}, 0, "Damage melebihi sisa HP, sehingga HP menjadi 0 dan musuh dikalahkan dalam game."),
            new TriviaQuestion("Label minuman mencantumkan 12 g gula per sajian. Ada 2 sajian per botol. Berapa gula satu botol?", new[]{"12 gram", "24 gram", "6 gram"}, 1, "12 g x 2 sajian = 24 g gula. Perhatikan jumlah sajian per kemasan."),
            new TriviaQuestion("Pada ukuran sajian sama, produk A mengandung 8 g gula dan B 15 g. Mana lebih rendah gula?", new[]{"Produk A", "Produk B", "Keduanya sama"}, 0, "8 g lebih rendah daripada 15 g jika ukuran sajiannya sama."),
            new TriviaQuestion("Sereal mengandung 4 g serat per sajian. Dua sajian mengandung berapa serat?", new[]{"2 gram", "4 gram", "8 gram"}, 2, "4 g x 2 sajian = 8 g serat."),
            new TriviaQuestion("Minuman memiliki 10 g gula per 100 ml. Berapa gula dalam 250 ml?", new[]{"10 gram", "25 gram", "40 gram"}, 1, "250 ml adalah 2,5 kali 100 ml. Jadi 10 x 2,5 = 25 g gula."),
            new TriviaQuestion("Satu kemasan berisi 90 g sereal. Ukuran sajinya 30 g. Ada berapa sajian?", new[]{"1 sajian", "30 sajian", "3 sajian"}, 2, "90 dibagi 30 = 3 sajian. Kalikan angka gizi per sajian dengan tiga untuk seluruh kemasan."),
            new TriviaQuestion("Pada porsi sama, sereal A mengandung 5 g serat dan B 2 g. Mana lebih tinggi serat?", new[]{"Sereal A", "Sereal B", "Sama"}, 0, "5 g lebih tinggi daripada 2 g pada porsi yang sama."),
            new TriviaQuestion("Label mencatat 20 g karbohidrat total, termasuk 8 g gula. Berapa karbohidrat selain gula?", new[]{"28 gram", "12 gram", "8 gram"}, 1, "20 - 8 = 12 g. Gula sudah termasuk dalam karbohidrat total."),
            new TriviaQuestion("Minuman A: 6 g gula per 100 ml. B: 9 g per 150 ml. Berapa gula masing-masing dalam 300 ml?", new[]{"A lebih tinggi", "B lebih tinggi", "Sama: 18 gram"}, 2, "A: 6 x 3 = 18 g. B: 9 x 2 = 18 g. Bandingkan pada volume yang sama."),
            new TriviaQuestion("Satu botol mengandung 24 g gula. Jika diminum seperempatnya, berapa gula yang dikonsumsi?", new[]{"6 gram", "12 gram", "24 gram"}, 0, "24 dibagi 4 = 6 g gula untuk seperempat botol."),
        };
    }
}
