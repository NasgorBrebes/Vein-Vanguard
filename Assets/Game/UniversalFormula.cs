using System;

namespace VeinVanguard
{
    public enum FormulaIcon { None = -1, ReduceAdded, Water, WholeFruit, UnsaturatedFat, Steam, VariedSpread }

    public static class UniversalFormula
    {
        public static bool IsValid(FormulaIcon icon) => icon >= FormulaIcon.ReduceAdded && icon <= FormulaIcon.VariedSpread;
        public static bool Complete(FormulaIcon first, FormulaIcon second) => IsValid(first) && IsValid(second);

        public static string Title(FormulaIcon icon)
        {
            switch (icon)
            {
                case FormulaIcon.None: return "Pilih formula";
                case FormulaIcon.ReduceAdded: return "Batasi tambahan";
                case FormulaIcon.Water: return "Air putih";
                case FormulaIcon.WholeFruit: return "Buah utuh";
                case FormulaIcon.UnsaturatedFat: return "Lemak tak jenuh";
                case FormulaIcon.Steam: return "Kukus/rebus";
                case FormulaIcon.VariedSpread: return "Variasi olesan";
                default: throw new ArgumentOutOfRangeException(nameof(icon));
            }
        }

        public static string IconName(FormulaIcon icon)
        {
            switch (icon)
            {
                case FormulaIcon.None: return null;
                case FormulaIcon.ReduceAdded: return "formula_added";
                case FormulaIcon.Water: return "formula_water";
                case FormulaIcon.WholeFruit: return "formula_whole";
                case FormulaIcon.UnsaturatedFat: return "formula_unsaturated";
                case FormulaIcon.Steam: return "formula_steam";
                case FormulaIcon.VariedSpread: return "formula_avocado";
                default: throw new ArgumentOutOfRangeException(nameof(icon));
            }
        }

        public static int Score(int encounter, int target, FormulaIcon icon)
        {
            var traits = CombatSamples.Get(encounter, target).traits;
            SampleTraits required;
            switch (icon)
            {
                case FormulaIcon.ReduceAdded: required = SampleTraits.AddedIngredient; break;
                case FormulaIcon.Water: required = SampleTraits.Beverage; break;
                case FormulaIcon.WholeFruit: required = SampleTraits.Juice; break;
                case FormulaIcon.UnsaturatedFat: required = SampleTraits.SaturatedFat; break;
                case FormulaIcon.Steam: required = SampleTraits.Fried; break;
                case FormulaIcon.VariedSpread: required = SampleTraits.ButterSpread; break;
                default: return 0;
            }
            return (traits & required) == 0 ? 0 : icon == FormulaIcon.Steam ? 1 : 2;
        }

        public static int Damage(int encounter, FormulaIcon first, FormulaIcon second)
        {
            int a = Score(encounter, 0, first), b = Score(encounter, 1, second);
            return a == 2 && b == 2 ? 40 : a > 0 || b > 0 ? 20 : 0;
        }

        public static string Feedback(int encounter, FormulaIcon first, FormulaIcon second)
        {
            return CombatSamples.Get(encounter, 0).title + " / " + Title(first) + ": " + Explanation(encounter, 0, first) + "\n\n" +
                CombatSamples.Get(encounter, 1).title + " / " + Title(second) + ": " + Explanation(encounter, 1, second);
        }

        static string Explanation(int encounter, int target, FormulaIcon icon)
        {
            bool fits = Score(encounter, target, icon) > 0;
            switch (icon)
            {
                case FormulaIcon.None: return "Soket belum terisi formula.";
                case FormulaIcon.ReduceAdded:
                    return fits ? "Mengurangi bahan yang ditambahkan langsung menyasar sumber pada rekaman. Jumlah bahan berkurang; jenis lemak atau gulanya tidak berubah." :
                        "Rekaman ini tidak menunjukkan tambahan bahan yang dimaksud. Mengurangi tambahan yang tidak ada belum menyasar sumbernya.";
                case FormulaIcon.Water:
                    return fits ? "Mengganti minuman sumber gula bebas dengan air mengurangi sumber tersebut; air tidak menetralkan gula di dalam tubuh." :
                        "Sumber ini bukan minuman. Penggantian minuman belum menjawab rekaman bahan lemak ini.";
                case FormulaIcon.WholeFruit:
                    return fits ? "Mengganti sajian jus dengan buah utuh sesuai dengan bukti bentuk pangan pada rekaman. Label tanpa gula tambahan tidak berarti bebas gula." :
                        "Sampel ini bukan sajian jus. Tindakan yang dipasang belum menyasar rekaman ini.";
                case FormulaIcon.UnsaturatedFat:
                    return fits ? "Penggantian menyasar kualitas lemak: gunakan pilihan kaya lemak tak jenuh, misalnya minyak kanola, kedelai, atau bunga matahari." :
                        "Rekaman ini berfokus pada sumber gula bebas, bukan sumber lemak jenuh. Pengganti harus menjawab bukti yang ditemukan.";
                case FormulaIcon.Steam:
                    return fits ? "Mengukus atau merebus dapat mengurangi minyak yang digunakan. Ini tidak otomatis mengubah lemak jenuh menjadi lemak tak jenuh, sehingga hasil formula masih sebagian." :
                        "Rekaman ini tidak menunjukkan proses menggoreng. Mengubah cara memasak belum menyasar bukti yang sedang diuji.";
                case FormulaIcon.VariedSpread:
                    return fits ? "Variasi ini mengganti sebagian mentega dengan alpukat sebagai pilihan kaya lemak tak jenuh, bukan sekadar menambah makanan di samping sumber lama." :
                        "Tindakan ini khusus penggantian olesan mentega. Rekaman sasaran tidak menunjukkan sumber tersebut.";
                default: throw new ArgumentOutOfRangeException(nameof(icon));
            }
        }
    }
}
