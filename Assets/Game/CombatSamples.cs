using System;

namespace VeinVanguard
{
    public enum SampleClass { FreeSugar, SaturatedFat, Starch }
    public enum SynthesisModule { None = -1, LimitFreeSugars, ImproveFatChoices, RemoveAllCarbs }
    public enum SourceAdapter { None = -1, ReduceSweetener, WholeFruit, ReduceButter, UnsaturatedOil }
    [Flags]
    public enum SampleTraits { None = 0, AddedIngredient = 1, Beverage = 2, Juice = 4, SaturatedFat = 8, Fried = 16, ButterSpread = 32 }

    public sealed class CombatSample
    {
        public readonly string title, record, icon, classificationFeedback, adapterFeedback;
        public readonly SampleClass kind;
        public readonly SynthesisModule module;
        public readonly SourceAdapter adapter;
        public readonly SampleTraits traits;

        internal CombatSample(string title, string record, SampleClass kind, SynthesisModule module,
            SourceAdapter adapter, string icon, string classificationFeedback, string adapterFeedback, SampleTraits traits)
        {
            this.title = title; this.record = record; this.kind = kind; this.module = module;
            this.adapter = adapter; this.icon = icon;
            this.classificationFeedback = classificationFeedback; this.adapterFeedback = adapterFeedback;
            this.traits = traits;
        }
    }

    public static class CombatSamples
    {
        // WHO: https://www.who.int/news-room/fact-sheets/detail/healthy-diet
        // WHO: https://www.who.int/news/item/17-07-2023-who-updates-guidelines-on-fats-and-carbohydrates
        static readonly CombatSample[][] samples = {
            new[] {
                new CombatSample("A · Mentega", "Rekaman sumber: mentega digunakan sebagai olesan roti.",
                    SampleClass.SaturatedFat, SynthesisModule.ImproveFatChoices, SourceAdapter.ReduceButter, "sample_butter",
                    "Mentega merupakan sumber lemak jenuh.",
                    "Olesan mentega lebih tipis mengurangi jumlah lemak jenuh yang dikonsumsi; jenis lemak mentega tetap sama.",
                    SampleTraits.AddedIngredient | SampleTraits.SaturatedFat | SampleTraits.ButterSpread),
                new CombatSample("B · Minyak kelapa", "Rekaman sumber: minyak kelapa digunakan untuk menggoreng.",
                    SampleClass.SaturatedFat, SynthesisModule.ImproveFatChoices, SourceAdapter.UnsaturatedOil, "sample_coconut_oil",
                    "Minyak kelapa merupakan sumber lemak jenuh meskipun berasal dari tumbuhan.",
                    "Ganti minyak kelapa dengan minyak kaya lemak tak jenuh, misalnya kanola, kedelai, atau bunga matahari.",
                    SampleTraits.SaturatedFat | SampleTraits.Fried),
            },
            new[] {
                new CombatSample("A · Sirup", "Rekaman sumber: sirup ditambahkan sebagai pemanis minuman.",
                    SampleClass.FreeSugar, SynthesisModule.LimitFreeSugars, SourceAdapter.ReduceSweetener, "sample_syrup",
                    "Gula dalam sirup termasuk gula bebas.",
                    "Kurangi sirup atau pemanis yang ditambahkan pada minuman untuk membatasi gula bebas.",
                    SampleTraits.Beverage | SampleTraits.AddedIngredient),
                new CombatSample("B · Jus buah", "Rekaman sumber: jus buah berlabel tanpa gula tambahan.",
                    SampleClass.FreeSugar, SynthesisModule.LimitFreeSugars, SourceAdapter.WholeFruit, "sample_juice",
                    "Gula dalam jus buah termasuk gula bebas, termasuk pada jus tanpa gula tambahan.",
                    "Pilih buah utuh sebagai pengganti jus. Label tanpa gula tambahan tidak berarti bebas gula.",
                    SampleTraits.Beverage | SampleTraits.Juice),
            },
        };

        public static CombatSample Get(int encounter, int target)
        {
            if (encounter < 0 || encounter >= samples.Length) throw new ArgumentOutOfRangeException(nameof(encounter));
            if (target < 0 || target >= samples[encounter].Length) throw new ArgumentOutOfRangeException(nameof(target));
            return samples[encounter][target];
        }

        public static string ModuleTitle(SynthesisModule module)
        {
            switch (module)
            {
                case SynthesisModule.None: return "Pilih modul";
                case SynthesisModule.LimitFreeSugars: return "Batasi gula bebas";
                case SynthesisModule.ImproveFatChoices: return "Perbaiki pilihan lemak";
                case SynthesisModule.RemoveAllCarbs: return "Hapus semua karbohidrat";
                default: throw new ArgumentOutOfRangeException(nameof(module));
            }
        }

        public static string ModuleIcon(SynthesisModule module)
        {
            switch (module)
            {
                case SynthesisModule.None: return null;
                case SynthesisModule.LimitFreeSugars: return "module_limit_free_sugars";
                case SynthesisModule.ImproveFatChoices: return "module_change_fats";
                case SynthesisModule.RemoveAllCarbs: return "module_remove_all_carbs";
                default: throw new ArgumentOutOfRangeException(nameof(module));
            }
        }

        public static string AdapterTitle(SourceAdapter adapter)
        {
            switch (adapter)
            {
                case SourceAdapter.None: return "Pilih adaptor";
                case SourceAdapter.ReduceSweetener: return "Kurangi pemanis";
                case SourceAdapter.WholeFruit: return "Pilih buah utuh";
                case SourceAdapter.ReduceButter: return "Kurangi olesan mentega";
                case SourceAdapter.UnsaturatedOil: return "Ganti ke minyak tak jenuh";
                default: throw new ArgumentOutOfRangeException(nameof(adapter));
            }
        }

        public static string AdapterIcon(SourceAdapter adapter)
        {
            switch (adapter)
            {
                case SourceAdapter.None: return null;
                case SourceAdapter.ReduceSweetener: return "adapter_reduce_sweetener";
                case SourceAdapter.WholeFruit: return "adapter_whole_fruit";
                case SourceAdapter.ReduceButter: return "adapter_reduce_butter";
                case SourceAdapter.UnsaturatedOil: return "adapter_unsaturated_oil";
                default: throw new ArgumentOutOfRangeException(nameof(adapter));
            }
        }

        public static string AttackFeedback(int encounter, int target, SynthesisModule module, SourceAdapter adapter)
        {
            var sample = Get(encounter, target);
            ModuleTitle(module); AdapterTitle(adapter);
            if (module == SynthesisModule.None) throw new ArgumentOutOfRangeException(nameof(module));
            if (adapter == SourceAdapter.None) throw new ArgumentOutOfRangeException(nameof(adapter));
            if (module == SynthesisModule.RemoveAllCarbs)
                return "Menghapus semua karbohidrat tidak menyasar sumber pada rekaman. Serealia utuh, sayur, buah, dan kacang-kacangan tetap dapat menjadi bagian pola makan.";
            if (module != sample.module)
                return sample.classificationFeedback + " Pilihan ini belum menyasar sumber pada rekaman. Gunakan modul " + ModuleTitle(sample.module).ToLowerInvariant() + ".";
            if (adapter != sample.adapter)
            {
                if (encounter == 0 && target == 0 && adapter == SourceAdapter.UnsaturatedOil)
                    return "Mengganti lemak jenuh dengan lemak tak jenuh juga prinsip yang baik. Tugas rekaman olesan ini memasangkan adaptor kurangi olesan mentega.";
                return "Modul sudah sesuai; pasangkan adaptor dengan penggunaan sumber pada rekaman. " + sample.adapterFeedback;
            }
            return sample.adapterFeedback;
        }
    }
}
