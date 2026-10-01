using System;
using UnityEngine;

namespace VeinVanguard
{
    public enum Nutrient { Omega3, Fiber, Water }
    public enum BattlePhase { Menu, Diagnose, Player, Trivia, Feedback, Enemy, Victory, Defeat, Complete }

    [Serializable]
    public class BattleModel
    {
        public int hp = 100, energy = 60, enemyHp, encounter, shield, correct, answered, turn;
        public int shieldHits;
        public bool diagnosed;
        public BattlePhase phase = BattlePhase.Menu;
        public int MaxEnemyHp => encounter == 0 ? 100 : 130;
        public Nutrient Weakness => encounter == 0 ? Nutrient.Omega3 : Nutrient.Fiber;
        public bool CanAttack => phase == BattlePhase.Player && diagnosed && energy >= 20;
        public void Begin(int index)
        {
            encounter = index; enemyHp = MaxEnemyHp; diagnosed = false; turn = 1;
            shield = shieldHits = 0;
            phase = BattlePhase.Diagnose;
        }
        public bool Diagnose()
        {
            if (phase != BattlePhase.Diagnose && phase != BattlePhase.Player) return false;
            diagnosed = true; phase = BattlePhase.Player; return true;
        }
        public static float Multiplier(Nutrient chosen, Nutrient weakness)
            => chosen == weakness ? 2f : chosen == Nutrient.Water ? 1f : .1f;
        public int Attack(Nutrient nutrient)
        {
            if (!CanAttack) return -1;
            energy -= 20;
            int damage = Mathf.RoundToInt(20 * Multiplier(nutrient, Weakness));
            enemyHp = Mathf.Max(0, enemyHp - damage);
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
            if (isCorrect) { correct++; energy = Mathf.Min(60, energy + 40); }
            else energy = Mathf.Min(60, energy + 10);
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
            int damage = Mathf.RoundToInt((encounter == 0 ? 14 : 18) * (1 - shield / 100f));
            hp = Mathf.Max(0, hp - damage); turn++;
            if (shieldHits > 0 && --shieldHits == 0) shield = 0;
            phase = hp == 0 ? BattlePhase.Defeat : BattlePhase.Player;
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
            new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>()
        };
        readonly int[] last = { -1, -1 };
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
            if (encounter == 0) return LipidQuestions;
            if (encounter == 1) return GlucoQuestions;
            throw new ArgumentOutOfRangeException(nameof(encounter));
        }
        public static readonly TriviaQuestion[] LipidQuestions = {
            new TriviaQuestion("Menurut diagnosis dalam game, komponen utama Lipid Golem adalah apa?", new[]{"Lemak jenuh", "Gula sederhana", "Air"}, 0, "Diagnosis Lipid Golem dalam game menunjukkan 70% lemak jenuh dan 30% senyawa lain."),
            new TriviaQuestion("Dalam game, nutrisi mana memberi damage x2 pada Lipid Golem?", new[]{"Serat", "Omega-3", "Air"}, 1, "Omega-3 adalah kelemahan Lipid Golem dalam mekanik game ini."),
            new TriviaQuestion("Serangan dasar 20 damage memakai Omega-3 x2 pada Lipid Golem. Berapa damage-nya?", new[]{"20 damage", "2 damage", "40 damage"}, 2, "20 x 2 = 40 damage. Gunakan hasil diagnosis untuk memilih serangan."),
            new TriviaQuestion("Serat hanya memiliki pengali x0,1 pada Lipid Golem. Dari 20 damage dasar, berapa hasilnya?", new[]{"2 damage", "20 damage", "40 damage"}, 0, "20 x 0,1 = 2 damage. Ini aturan pertarungan dalam game."),
            new TriviaQuestion("Label makanan mencantumkan 3 g lemak jenuh per sajian. Berapa dalam 2 sajian?", new[]{"3 gram", "6 gram", "9 gram"}, 1, "3 g x 2 sajian = 6 g lemak jenuh."),
            new TriviaQuestion("Pada porsi sama, camilan A mengandung 2 g lemak jenuh dan B 5 g. Mana lebih rendah?", new[]{"Camilan A", "Camilan B", "Sama"}, 0, "Pada porsi yang sama, 2 g lebih rendah daripada 5 g."),
            new TriviaQuestion("Satu kemasan berisi 4 sajian, masing-masing 5 g lemak total. Berapa lemak seluruh kemasan?", new[]{"5 gram", "10 gram", "20 gram"}, 2, "4 x 5 g = 20 g lemak total. Angka per sajian berbeda dari angka per kemasan."),
            new TriviaQuestion("Label menunjukkan 10 g lemak total, termasuk 3 g lemak jenuh. Berapa lemak selain lemak jenuh?", new[]{"13 gram", "7 gram", "3 gram"}, 1, "10 - 3 = 7 g. Lemak jenuh sudah termasuk dalam angka lemak total."),
            new TriviaQuestion("Produk A memiliki 2 g lemak jenuh per 20 g. Berapa lemak jenuh dalam porsi 40 g?", new[]{"4 gram", "2 gram", "1 gram"}, 0, "Porsi 40 g adalah dua kali 20 g, sehingga 2 x 2 = 4 g."),
            new TriviaQuestion("Minyak pada label mengandung 14 g lemak per sendok. Setengah sendok mengandung berapa?", new[]{"28 gram", "14 gram", "7 gram"}, 2, "14 g dibagi 2 = 7 g lemak untuk setengah ukuran saji."),
            new TriviaQuestion("Produk A: 2 g lemak jenuh per 20 g. B: 3 g per 30 g. Jika porsinya 60 g, bagaimana hasilnya?", new[]{"A lebih tinggi", "Sama: 6 gram", "B lebih tinggi"}, 1, "A: 2 x 3 = 6 g. B: 3 x 2 = 6 g. Samakan porsi sebelum membandingkan."),
            new TriviaQuestion("Lipid Golem memiliki 80 HP. Setiap serangan Omega-3 menghasilkan 40 damage. Perlu berapa serangan?", new[]{"2 serangan", "4 serangan", "8 serangan"}, 0, "80 dibagi 40 = 2 serangan, selama damage tiap serangan tetap sama."),
        };
        public static readonly TriviaQuestion[] GlucoQuestions = {
            new TriviaQuestion("Menurut diagnosis dalam game, komponen utama Gluco Slime adalah apa?", new[]{"Lemak jenuh", "Gula sederhana", "Protein"}, 1, "Diagnosis Gluco Slime dalam game menunjukkan 70% gula sederhana."),
            new TriviaQuestion("Dalam game, nutrisi mana memberi damage x2 pada Gluco Slime?", new[]{"Air", "Omega-3", "Serat"}, 2, "Serat adalah kelemahan Gluco Slime dalam mekanik game ini."),
            new TriviaQuestion("Gluco Slime tersisa 38 HP. Serangan serat menghasilkan 40 damage. Berapa HP yang tersisa?", new[]{"0 HP", "2 HP", "18 HP"}, 0, "Damage melebihi sisa HP, sehingga HP menjadi 0 dan musuh dikalahkan."),
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
