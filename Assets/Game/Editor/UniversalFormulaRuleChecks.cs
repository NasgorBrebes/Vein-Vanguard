using System;
using System.IO;
using UnityEngine;

namespace VeinVanguard.Editor
{
    public static class UniversalFormulaRuleChecks
    {
        public static string VerifyAll()
        {
            VerifySourceRules();
            VerifyInvalidInputs();
            for (int encounter = 0; encounter < 2; encounter++) VerifyBattle(encounter);
            return "PASS: universal source traits, alternate/duplicate formulas, 0/20/40 damage, invalid inputs, one-scan diagnosis, phase/MP/turn/reset gates";
        }

        public static string VerifyFinalLearning()
        {
            Check(CombatSamples.Get(2, 0).record.Contains("ketergantungan"), "Rokok still uses temporary food records.");
            var method = typeof(UniversalFormula).GetMethod("Palette");
            Check(method != null, "Themed palettes are missing.");
            int[][] expected = { new[] {0,1,2,3,4,5}, new[] {0,1,2,3,4,5},
                new[] {6,7,8,9,10,11}, new[] {0,1,2,3,4,5},
                new[] {12,13,14,15,16,17}, new[] {6,7,8,12,13,16} };
            int[] bankSizes = {12,12,6,6,6,12};
            int pairs = 0;
            for (int encounter = 0; encounter < 6; encounter++)
            {
                var palette = (FormulaIcon[])method.Invoke(null, new object[] {encounter});
                Check(palette.Length == 6, "Each encounter needs six cards.");
                for (int i = 0; i < 6; i++) Check((int)palette[i] == expected[encounter][i], "Palette order changed.");
                foreach (var first in palette)
                    foreach (var second in palette)
                    {
                        int a = ExpectedScore(encounter, 0, (int)first);
                        int b = ExpectedScore(encounter, 1, (int)second);
                        int damage = a == 2 && b == 2 ? 40 : a > 0 || b > 0 ? 20 : 0;
                        var model = new BattleModel(); model.Begin(encounter); model.DiagnoseAll();
                        Check(model.AttackFormula(first, second) == damage && model.energy == 40 &&
                            model.enemyHp == model.MaxEnemyHp - damage && model.phase == BattlePhase.Enemy,
                            "Final formula matrix, MP or turn mismatch.");
                        Check(model.AttackFormula(first, second) == -1 && model.energy == 40, "Double Fire spent MP.");
                        pairs++;
                    }
                var bank = TriviaBank.ForEncounter(encounter);
                Check(bank.Length == bankSizes[encounter], "Incorrect Restore bank size.");
                foreach (var question in bank)
                    Check(question.options.Length == 3 && question.answer >= 0 && question.answer < 3 &&
                        !string.IsNullOrWhiteSpace(question.explanation), "Invalid Restore item.");
                var deck = new TriviaDeck(new System.Random(42));
                var seen = new System.Collections.Generic.HashSet<string>();
                string last = null;
                for (int i = 0; i < bank.Length; i++)
                {
                    var drawn = deck.Next(encounter);
                    Check(seen.Add(drawn.prompt), "Restore repeats before deck exhaustion.");
                    var original = Array.Find(bank, q => q.prompt == drawn.prompt);
                    Check(drawn.options[drawn.answer] == original.options[original.answer], "Shuffling changed the correct option.");
                    last = drawn.prompt;
                }
                Check(deck.Next(encounter).prompt != last, "Restore repeated at the deck boundary.");
                var invalid = new BattleModel(); invalid.Begin(encounter); invalid.DiagnoseAll();
                var foreign = (FormulaIcon)(encounter == 2 ? 12 : 6);
                if (encounter == 5) foreign = (FormulaIcon)9;
                Check(invalid.AttackFormula(foreign, palette[0]) == -1 && invalid.energy == 60,
                    "A foreign card entered the encounter.");
            }
            return "PASS: six final palettes, 216 formula pairs, MP/turn gates, Restore banks and shuffle.";
        }

        static int ExpectedScore(int encounter, int slot, int card)
        {
            if (encounter == 2) return card == (slot == 0 ? 6 : 7) ? 2 : 0;
            if (encounter == 4) return card == (slot == 0 ? 12 : 13) ? 2 : 0;
            if (encounter == 5) return card == (slot == 0 ? 7 : 13) ? 2 : 0;
            int[,] food = { {2,0,0,2,0,2}, {0,0,0,2,1,0}, {2,2,0,0,0,0}, {0,2,2,0,0,0} };
            return food[(encounter == 1 ? 2 : 0) + slot, card];
        }

        static void VerifySourceRules()
        {
            int[,] scores = {
                { 2, 0, 0, 2, 0, 2 }, // Butter: added ingredient, saturated fat, spread.
                { 0, 0, 0, 2, 1, 0 }, // Coconut oil: saturated fat used for frying.
                { 2, 2, 0, 0, 0, 0 }, // Syrup: an added beverage ingredient.
                { 0, 2, 2, 0, 0, 0 }, // Juice: beverage and juice, no added ingredient.
            };
            string[] icons = { "formula_added", "formula_water", "formula_whole", "formula_unsaturated", "formula_steam", "formula_avocado" };
            for (int source = 0; source < 4; source++)
                for (int icon = 0; icon < 6; icon++)
                    Check(UniversalFormula.Score(source / 2, source % 2, (FormulaIcon)icon) == scores[source, icon], "Source trait matched the wrong formula");
            for (int icon = 0; icon < 6; icon++)
            {
                var choice = (FormulaIcon)icon;
                Check(UniversalFormula.IsValid(choice) && UniversalFormula.Complete(choice, choice), "Repeated known icons must fill both sockets");
                Check(!string.IsNullOrWhiteSpace(UniversalFormula.Title(choice)) && UniversalFormula.IconName(choice) == icons[icon], "Formula cannot resolve its visible title/icon");
            }
            Check(UniversalFormula.Damage(0, FormulaIcon.ReduceAdded, FormulaIcon.UnsaturatedFat) == 40, "Lipid source-specific pair failed");
            Check(UniversalFormula.Damage(0, FormulaIcon.UnsaturatedFat, FormulaIcon.UnsaturatedFat) == 40, "Repeated unsaturated-fat replacements failed");
            Check(UniversalFormula.Damage(0, FormulaIcon.VariedSpread, FormulaIcon.UnsaturatedFat) == 40, "Avocado spread replacement failed");
            Check(UniversalFormula.Damage(1, FormulaIcon.ReduceAdded, FormulaIcon.WholeFruit) == 40, "Sugar source-specific pair failed");
            Check(UniversalFormula.Damage(1, FormulaIcon.Water, FormulaIcon.Water) == 40, "Repeated water replacements failed");
            Check(UniversalFormula.Damage(1, FormulaIcon.Water, FormulaIcon.WholeFruit) == 40, "Alternative beverage replacement failed");
            Check(UniversalFormula.Damage(0, FormulaIcon.ReduceAdded, FormulaIcon.Steam) == 20, "Cooking must remain partial without changing fat type");
            Check(UniversalFormula.Damage(0, FormulaIcon.Water, FormulaIcon.Steam) == 20, "One partial source must produce partial damage");
            Check(UniversalFormula.Damage(1, FormulaIcon.WholeFruit, FormulaIcon.ReduceAdded) == 0, "Sockets must stay mapped to source A/B");
            Check(UniversalFormula.Damage(0, FormulaIcon.Water, FormulaIcon.WholeFruit) == 0, "Unrelated choices produced damage");
            Check(CombatSamples.Get(0, 1).record.Contains("menggoreng"), "Partial cooking has no frying evidence in the record");
            for (int encounter = 0; encounter < 2; encounter++)
                foreach (FormulaIcon icon in Enum.GetValues(typeof(FormulaIcon)))
                {
                    string feedback = UniversalFormula.Feedback(encounter, icon, icon);
                    Check(feedback.Contains(CombatSamples.Get(encounter, 0).title) && feedback.Contains(CombatSamples.Get(encounter, 1).title), "Formula feedback omitted a source");
                }
        }

        static void VerifyInvalidInputs()
        {
            Check(!UniversalFormula.IsValid(FormulaIcon.None) && UniversalFormula.IconName(FormulaIcon.None) == null, "Empty socket was treated as a formula");
            Check(!string.IsNullOrWhiteSpace(UniversalFormula.Title(FormulaIcon.None)), "Empty socket has no visible label");
            foreach (int id in new[] { -2, -1, 18, int.MaxValue })
            {
                var icon = (FormulaIcon)id;
                Check(!UniversalFormula.IsValid(icon) && !UniversalFormula.Complete(icon, FormulaIcon.Water) && !UniversalFormula.Complete(FormulaIcon.Water, icon), "Unknown or empty socket completed a formula");
                Check(UniversalFormula.Score(1, 0, icon) == 0, "Unknown icon matched a source");
                if (icon == FormulaIcon.None) continue;
                RejectsArgument(() => UniversalFormula.Title(icon));
                RejectsArgument(() => UniversalFormula.IconName(icon));
                RejectsArgument(() => UniversalFormula.Feedback(0, icon, FormulaIcon.Water));
            }
            foreach (int enc in new[] { -1, EncounterCatalog.Count, int.MaxValue })
            {
                RejectsArgument(() => UniversalFormula.Score(enc, 0, FormulaIcon.Water));
                RejectsArgument(() => UniversalFormula.Damage(enc, FormulaIcon.Water, FormulaIcon.Water));
                RejectsArgument(() => UniversalFormula.Feedback(enc, FormulaIcon.Water, FormulaIcon.Water));
            }
            foreach (int target in new[] { -1, 2, int.MaxValue })
            {
                RejectsArgument(() => UniversalFormula.Score(0, target, FormulaIcon.Water));
            }
        }

        static void VerifyBattle(int encounter)
        {
            FormulaIcon first = encounter == 0 ? FormulaIcon.ReduceAdded : FormulaIcon.Water;
            FormulaIcon second = encounter == 0 ? FormulaIcon.UnsaturatedFat : FormulaIcon.WholeFruit;
            var model = new BattleModel();
            Check(!model.DiagnoseAll(), "Menu allowed diagnosis");
            model.Begin(encounter);
            Check(!model.CanSynthesizeFormula(first, second) && model.AttackFormula(first, second) == -1 && model.energy == 60 && model.enemyHp == model.MaxEnemyHp, "Undiagnosed attack changed combat");
            Check(model.DiagnoseAll() && model.diagnosed && model.ClassifiedCount == 2 && model.WasScanned(0) && model.WasScanned(1) && model.CanAttack && model.phase == BattlePhase.Player, "Single scan failed to reveal both sources");
            Check(!model.DiagnoseAll() && model.CanSynthesizeFormula(first, second) && model.energy == 60 && model.enemyHp == model.MaxEnemyHp, "Diagnosis/preview mutated a ready turn");

            foreach (int id in new[] { -2, -1, 6, int.MaxValue })
            {
                var invalid = (FormulaIcon)id;
                Check(!model.CanSynthesizeFormula(invalid, second) && !model.CanSynthesizeFormula(first, invalid), "Invalid socket enabled firing");
                Check(model.AttackFormula(invalid, second) == -1 && model.AttackFormula(first, invalid) == -1 && model.energy == 60 && model.enemyHp == model.MaxEnemyHp && model.phase == BattlePhase.Player, "Invalid formula mutated battle");
            }
            model.energy = 19;
            Check(!model.CanSynthesizeFormula(first, second) && model.AttackFormula(first, second) == -1 && model.energy == 19 && model.phase == BattlePhase.Player, "Low-energy attack was accepted");

            foreach (int damage in new[] { 0, 20, 40 })
            {
                model = Ready(encounter); model.energy = 20;
                var a = damage == 0 ? (encounter == 0 ? FormulaIcon.Water : FormulaIcon.Steam) : first;
                var b = damage == 40 ? second : (encounter == 0 ? FormulaIcon.WholeFruit : FormulaIcon.VariedSpread);
                int beforeHp = model.enemyHp;
                Check(model.CanSynthesizeFormula(a, b) && model.energy == 20 && model.enemyHp == beforeHp, "Complete preview was rejected or spent resources");
                Check(model.AttackFormula(a, b) == damage && model.energy == 0 && model.enemyHp == beforeHp - damage && model.phase == BattlePhase.Enemy, "Formula damage or single 20-MP cost failed");
                Check(model.AttackFormula(a, b) == -1 && model.energy == 0 && model.enemyHp == beforeHp - damage && model.phase == BattlePhase.Enemy, "Duplicate shot bypassed turn lock");
            }
            foreach (BattlePhase phase in Enum.GetValues(typeof(BattlePhase)))
            {
                if (phase == BattlePhase.Player) continue;
                model = Ready(encounter); model.phase = phase;
                Check(!model.CanSynthesizeFormula(first, second) && model.AttackFormula(first, second) == -1 && model.energy == 60 && model.enemyHp == model.MaxEnemyHp && model.phase == phase, "Wrong-phase shot mutated combat");
            }
            foreach (BattlePhase phase in Enum.GetValues(typeof(BattlePhase)))
            {
                if (phase == BattlePhase.Diagnose) continue;
                model = new BattleModel(); model.Begin(encounter); model.phase = phase;
                Check(!model.DiagnoseAll() && !model.diagnosed && model.ClassifiedCount == 0 && !model.WasScanned(0) && !model.WasScanned(1) && model.phase == phase, "Wrong-phase scan mutated diagnosis");
            }
            model = new BattleModel(); model.Begin(encounter);
            model.diagnosed = true; model.phase = BattlePhase.Player;
            Check(!model.CanSynthesizeFormula(first, second) && model.AttackFormula(first, second) == -1, "Public diagnosis flag bypassed source masks");

            model = new BattleModel(); model.Begin(encounter);
            Check(model.ScanSample(0) && model.ClassifySample(0, encounter == 0 ? SampleClass.SaturatedFat : SampleClass.FreeSugar) && model.DiagnoseAll() && model.IsClassified(1), "Single scan could not finish an existing partial diagnosis");
            model = Ready(encounter); model.enemyHp = 30;
            Check(model.AttackFormula(first, second) == 40 && model.enemyHp == 0 && model.energy == 40 && model.phase == BattlePhase.Victory, "Lethal formula failed");
            Check(model.AttackFormula(first, second) == -1 && model.EnemyAttack() == -1 && model.hp == 100, "Victory allowed another combat action");
            model.hp = 42; model.energy = 37;
            model.Begin(1 - encounter);
            Check(!model.diagnosed && model.ClassifiedCount == 0 && !model.WasScanned(0) && !model.WasScanned(1) && !model.CanSynthesizeFormula(first, second) && model.phase == BattlePhase.Diagnose && model.hp == 42 && model.energy == 37 && model.enemyHp == model.MaxEnemyHp, "Encounter reset kept a formula gate or lost persistent resources");
        }

        static BattleModel Ready(int encounter)
        {
            var model = new BattleModel(); model.Begin(encounter);
            Check(model.DiagnoseAll(), "Formula setup diagnosis failed");
            return model;
        }

        public static string VerifyExpansionCatalog()
        {
            var catalogType = typeof(BattleModel).Assembly.GetType("VeinVanguard.EncounterCatalog");
            Check(catalogType != null, "EncounterCatalog class missing");
            var countField = catalogType.GetField("Count");
            Check(countField != null && (int)countField.GetValue(null) == 6, "EncounterCatalog.Count must be 6");

            int[] expectedHp = { 100, 130, 130, 130, 130, 160 };
            int[] expectedIncoming = { 14, 18, 18, 18, 18, 18 };
            string[] expectedTitle = { "LIPID GOLEM", "GLUCO SLIME", "SMOKE IMP", "FRYSTACK DUO", "ETHANOL MIMIC", "TOXIFANG" };

            var maxHpMethod = catalogType.GetMethod("MaxHp");
            var incomingMethod = catalogType.GetMethod("IncomingDamage");
            var titleMethod = catalogType.GetMethod("Title");

            Check(maxHpMethod != null && incomingMethod != null && titleMethod != null, "EncounterCatalog methods missing");

            for (int i = 0; i < 6; i++)
            {
                Check((int)maxHpMethod.Invoke(null, new object[] { i }) == expectedHp[i], $"MaxHp mismatch for {i}");
                Check((int)incomingMethod.Invoke(null, new object[] { i }) == expectedIncoming[i], $"Incoming mismatch for {i}");
                Check((string)titleMethod.Invoke(null, new object[] { i }) == expectedTitle[i], $"Title mismatch for {i}");

                var model = new BattleModel();
                model.Begin(i);
                Check(model.enemyHp == expectedHp[i], $"Model enemyHp mismatch for {i}");
                Check(model.encounter == i, $"Model encounter mismatch for {i}");
            }

            foreach (int invalid in new[] { -2, -1, 6, 99 })
            {
                RejectsTargetInvocation(() => maxHpMethod.Invoke(null, new object[] { invalid }));
                RejectsTargetInvocation(() => incomingMethod.Invoke(null, new object[] { invalid }));
                RejectsTargetInvocation(() => titleMethod.Invoke(null, new object[] { invalid }));

                var model = new BattleModel();
                try { model.Begin(invalid); throw new Exception($"Begin({invalid}) was accepted"); }
                catch (ArgumentOutOfRangeException) { }
            }
            return "PASS: expansion catalog";
        }

        // The expansion runner previously required temporary food reuse; final themed data supersedes it.
        public static string VerifyReusedLearning() => VerifyFinalLearning();

        public static string VerifyExpansionProgression()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(game != null, "BattleManager missing in scene");

            game.StartGame();
            Check(game.defeated != null && game.defeated.Length == 6, "defeated.Length must be 6");
            Check(game.StageIndex == 0, "Initial stage must be 0");

            for (int i = 0; i <= 3; i++)
            {
                Check(game.CanBeginEncounter(i), $"CanBeginEncounter({i}) should be true in stage 0");
            }
            Check(!game.CanBeginEncounter(4), "CanBeginEncounter(4) should be false in stage 0");
            Check(!game.CanBeginEncounter(5), "CanBeginEncounter(5) should be false in stage 0");
            Check(!game.CanBeginEncounter(-1), "CanBeginEncounter(-1) should be false");
            Check(!game.CanBeginEncounter(6), "CanBeginEncounter(6) should be false");

            Check(!game.TryAdvanceStage(), "TryAdvanceStage should return false when no enemies are defeated");

            game.defeated[0] = true;
            game.defeated[1] = true;
            Check(!game.TryAdvanceStage(), "TryAdvanceStage should return false when only 2 enemies defeated");
            Check(game.model.phase != BattlePhase.Complete, "Two victories must not be Complete");

            game.defeated[2] = true;
            game.defeated[3] = true;
            Check(game.TryAdvanceStage(), "TryAdvanceStage should succeed when all 4 stage 1 enemies are defeated");
            Check(game.StageIndex == 1, "StageIndex must be 1 after advance");
            Check(!game.TryAdvanceStage(), "Second TryAdvanceStage must return false");

            for (int i = 0; i <= 3; i++)
            {
                Check(!game.CanBeginEncounter(i), $"CanBeginEncounter({i}) should be false in stage 1");
            }
            Check(game.CanBeginEncounter(4), "CanBeginEncounter(4) should be true in stage 1");
            Check(!game.CanBeginEncounter(5), "CanBeginEncounter(5) should be false before Botol is defeated");

            game.defeated[4] = true;
            Check(game.CanBeginEncounter(5), "CanBeginEncounter(5) should be true after Botol is defeated");

            var commitMethod = typeof(BattleManager).GetMethod("CommitEncounterVictory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            Check(commitMethod != null, "CommitEncounterVictory method missing");

            game.StartGame();
            game.model.hp = 20;
            game.model.energy = 5;
            game.model.enemyHp = 0;
            game.model.phase = BattlePhase.Victory;
            game.model.encounter = 0;
            bool committed = (bool)commitMethod.Invoke(game, new object[] { 0 });
            Check(committed, "CommitEncounterVictory should succeed for encounter 0");
            Check(game.model.hp == 45, $"HP must be 45 (+25 reward), got {game.model.hp}");
            Check(game.model.energy == 25, $"Energy must be 25 (+20 reward), got {game.model.energy}");

            bool secondCommit = (bool)commitMethod.Invoke(game, new object[] { 0 });
            Check(!secondCommit, "Second commit for same encounter must return false");
            Check(game.model.hp == 45 && game.model.energy == 25, "Duplicate commit must not change HP/MP");

            game.model.hp = 95;
            game.model.energy = 55;
            game.model.enemyHp = 0;
            game.model.phase = BattlePhase.Victory;
            game.model.encounter = 1;
            committed = (bool)commitMethod.Invoke(game, new object[] { 1 });
            Check(committed, "Commit for encounter 1 should succeed");
            Check(game.model.hp == 100, $"HP capped at 100, got {game.model.hp}");
            Check(game.model.energy == 60, $"Energy capped at 60, got {game.model.energy}");

            game.StartGame();
            return "PASS: expansion progression gating, stages, and victory rewards";
        }

        public static string VerifyExpansionTraversal()
        {
            Check(BattleManager.IsWalkable(new Vector2(0.50f, 0.04f)), "IsWalkable(Vector2) overload must check stage 0");
            Check(BattleManager.IsWalkable(0, new Vector2(0.50f, 0.04f)), "Spawn must be walkable in stage 0");

            Vector2[] stage0Markers = {
                new Vector2(0.75f, 0.15f),
                new Vector2(0.31f, 0.15f),
                new Vector2(0.75f, 0.40f),
                new Vector2(0.34f, 0.50f)
            };
            for (int i = 0; i < stage0Markers.Length; i++)
            {
                Check(BattleManager.IsWalkable(0, stage0Markers[i]), $"Marker {i} must be walkable in stage 0");
            }

            Check(!BattleManager.IsWalkable(0, new Vector2(0.1f, 0.9f)), "Off-road point must not be walkable in stage 0");
            Check(!BattleManager.IsWalkable(0, new Vector2(0.2f, 0.3f)), "Off-road point (0.2, 0.3) must not be walkable in stage 0");

            // Direct diagonal from bottom-right (Golem) to far upper-road is blocked by non-road territory.
            Check(!BattleManager.CanTraverse(0, new Vector2(0.75f, 0.15f), new Vector2(0.76f, 0.76f)), "Direct traversal across chasm in stage 0 must be rejected");

            Vector2 stage1Spawn = new Vector2(0.08f, 0.48f);
            Vector2 botolPos = new Vector2(0.77f, 0.33f);
            Vector2 bossPos = new Vector2(.785f, .71f);

            Check(BattleManager.IsWalkable(1, stage1Spawn), "Stage 1 spawn must be walkable");
            Check(BattleManager.IsWalkable(1, botolPos), "Stage 1 Botol position must be walkable");
            Check(BattleManager.IsWalkable(1, bossPos), "Stage 1 Boss position must be walkable");

            Check(!BattleManager.IsWalkable(1, new Vector2(0.5f, 0.5f)), "Midpoint chasm in stage 1 must not be walkable");
            Check(!BattleManager.IsWalkable(1, new Vector2(0.1f, 0.1f)), "Bottom-left void in stage 1 must not be walkable");

            Check(BattleManager.CanTraverse(1, stage1Spawn, botolPos), "The lower bridge must allow a direct walk from spawn to Botol");
            Check(!BattleManager.CanTraverse(1, stage1Spawn, bossPos), "Direct traversal from spawn to Boss across chasm must be rejected");
            Check(!BattleManager.CanTraverse(1, botolPos, bossPos), "Direct traversal between Botol and Boss across chasm must be rejected");

            Check(!BattleManager.CanTraverse(-1, stage1Spawn, stage1Spawn), "Invalid stage -1 must be rejected");
            Check(!BattleManager.CanTraverse(2, stage1Spawn, stage1Spawn), "Invalid stage 2 must be rejected");
            Check(!BattleManager.CanTraverse(1, new Vector2(float.NaN, 0), stage1Spawn), "NaN must be rejected");
            Check(!BattleManager.CanTraverse(1, stage1Spawn, new Vector2(float.PositiveInfinity, 0)), "Infinity must be rejected");

            Check(BattleManager.CanTraverse(1, new Vector2(.25f, .445f), new Vector2(.35f, .417f)),
                "Map 2 lower bridge must allow movement off its centerline");
            Check(BattleManager.CanTraverse(1, new Vector2(.148f, .53f), new Vector2(.214f, .61f)),
                "Map 2 upper stairs must allow movement off their centerline");
            Check(BattleManager.CanTraverse(1, new Vector2(.455f, .675f), new Vector2(.61f, .715f)),
                "Map 2 upper bridge must allow movement off its centerline");

            Vector2[] lowerRoute = {
                stage1Spawn, new Vector2(.17f, .473f), new Vector2(.25f, .467f),
                new Vector2(.35f, .439f), new Vector2(.52f, .389f), botolPos
            };
            Vector2[] upperRoute = {
                stage1Spawn, new Vector2(.12f, .53f), new Vector2(.186f, .61f),
                new Vector2(.34f, .60f), new Vector2(.402f, .619f), new Vector2(.455f, .652f),
                new Vector2(.61f, .692f), new Vector2(.692f, .699f), new Vector2(.736f, .679f), bossPos
            };
            foreach (var route in new[] { lowerRoute, upperRoute })
            {
                for (int i = 1; i < route.Length; i++)
                {
                    Vector2 from = route[i - 1];
                    Vector2 to = route[i];
                    Check(BattleManager.CanTraverse(1, from, to), $"Map 2 route blocked from {from} to {to}");
                    Check(BattleManager.CanTraverse(1, to, from), $"Map 2 return route blocked from {to} to {from}");
                }
            }

            return "PASS: expansion traversal across both stages, chasm rejection, and edge connectivity";
        }

        public static string VerifyExpansionFullSession()
        {
            // 1. Verify two victories does NOT complete the session
            var earlyModel = new BattleModel();
            earlyModel.Begin(0);
            earlyModel.DiagnoseAll();
            earlyModel.AttackFormula(FormulaIcon.ReduceAdded, FormulaIcon.UnsaturatedFat);
            Check(earlyModel.phase != BattlePhase.Complete, "First encounter must not complete the session");

            // 2. Separate 1 HP fixture for lethal and defeat/retry branches (labeled DATA UJI)
            var fixture1Hp = new BattleModel();
            fixture1Hp.Begin(0);
            fixture1Hp.hp = 1;
            fixture1Hp.energy = 20;
            fixture1Hp.DiagnoseAll();
            fixture1Hp.phase = BattlePhase.Enemy;
            fixture1Hp.EnemyAttack();
            Check(fixture1Hp.phase == BattlePhase.Defeat && fixture1Hp.hp == 0, "DATA UJI: 1 HP fixture must trigger defeat on enemy attack");

            var lethalFixture = new BattleModel();
            lethalFixture.Begin(0);
            lethalFixture.enemyHp = 1;
            lethalFixture.DiagnoseAll();
            lethalFixture.AttackFormula(FormulaIcon.ReduceAdded, FormulaIcon.UnsaturatedFat);
            Check(lethalFixture.phase == BattlePhase.Victory && lethalFixture.enemyHp == 0, "DATA UJI: lethal hit must trigger victory without counterattack");

            // 3. Full playthrough model simulation (seed 42, route 0..5)
            var rng = new System.Random(42);
            var model = new BattleModel();
            int totalTurns = 0;
            var sessionLines = new System.Collections.Generic.List<string>();
            sessionLines.Add("encounter,turn,action,hp,mp,enemyHp,shieldHits");

            for (int encId = 0; encId < EncounterCatalog.Count; encId++)
            {
                model.Begin(encId);
                model.DiagnoseAll();
                totalTurns++;
                sessionLines.Add($"{encId},{totalTurns},DiagnoseAll,{model.hp},{model.energy},{model.enemyHp},{model.shieldHits}");

                // Find first 40-damage formula pair in enum order
                FormulaIcon bestA = FormulaIcon.None, bestB = FormulaIcon.None;
                for (int a = 0; a < 6; a++)
                {
                    for (int b = 0; b < 6; b++)
                    {
                        if (UniversalFormula.Damage(encId, UniversalFormula.Palette(encId)[a], UniversalFormula.Palette(encId)[b]) == 40)
                        {
                            bestA = UniversalFormula.Palette(encId)[a];
                            bestB = UniversalFormula.Palette(encId)[b];
                            break;
                        }
                    }
                    if (bestA != FormulaIcon.None) break;
                }
                Check(bestA != FormulaIcon.None && bestB != FormulaIcon.None, $"Encounter {encId} must have a 40-damage formula pair");

                while (model.enemyHp > 0 && model.hp > 0)
                {
                    totalTurns++;
                    Check(totalTurns <= 200, "Turn limit exceeded (200 turns)");

                    if (model.energy < 20)
                    {
                        // Restore correctly
                        bool okRestore = model.Restore();
                        Check(okRestore, "Restore should be available when MP < 20");
                        model.Answer(true);
                        model.phase = BattlePhase.Enemy;
                        model.EnemyAttack();
                        sessionLines.Add($"{encId},{totalTurns},Restore,{model.hp},{model.energy},{model.enemyHp},{model.shieldHits}");
                    }
                    else if (model.shieldHits == 0 && model.energy >= 30)
                    {
                        // Homeostasis
                        bool okHomeostasis = model.Homeostasis(10, 90, 2);
                        Check(okHomeostasis, "Homeostasis should be available when shieldHits == 0 and MP >= 30");
                        model.EnemyAttack();
                        sessionLines.Add($"{encId},{totalTurns},Homeostasis,{model.hp},{model.energy},{model.enemyHp},{model.shieldHits}");
                    }
                    else
                    {
                        // Attack with first 40 damage pair
                        int dmg = model.AttackFormula(bestA, bestB);
                        Check(dmg == 40, "AttackFormula must deal 40 damage");
                        if (model.phase == BattlePhase.Enemy)
                        {
                            model.EnemyAttack();
                        }
                        sessionLines.Add($"{encId},{totalTurns},AttackFormula,{model.hp},{model.energy},{model.enemyHp},{model.shieldHits}");
                    }
                }

                Check(model.hp > 0, $"Player must survive encounter {encId}");
                Check(model.phase == (encId == EncounterCatalog.Count - 1 ? BattlePhase.Complete : BattlePhase.Victory) || model.enemyHp == 0,
                    $"Encounter {encId} must be defeated");

                // Victory reward (+25 HP / +20 MP capped at 100/60)
                model.hp = Math.Min(100, model.hp + 25);
                model.energy = Math.Min(60, model.energy + 20);
            }

            Check(totalTurns <= 200, "Full session must complete within 200 turns");

            // Write session.csv
            string csvDir = "docs/superpowers/verification/two-stage-existing-gameplay";
            Directory.CreateDirectory(csvDir);
            File.WriteAllLines(Path.Combine(csvDir, "session.csv"), sessionLines);

            // 4. Run 2: Test one 0-damage formula in each stage (observational)
            var simRun2 = new BattleModel();
            int run2Turns = 0;
            bool run2Completed = true;
            for (int encId = 0; encId < EncounterCatalog.Count; encId++)
            {
                simRun2.Begin(encId);
                simRun2.DiagnoseAll();
                run2Turns++;

                // Find 0-damage pair for stage test (enc 0 in stage 0, enc 4 in stage 1)
                bool firedZero = false;
                FormulaIcon zeroA = FormulaIcon.None, zeroB = FormulaIcon.None;
                if (encId == 0 || encId == 4)
                {
                    for (int a = 0; a < 6; a++)
                    {
                        for (int b = 0; b < 6; b++)
                        {
                            if (UniversalFormula.Damage(encId, UniversalFormula.Palette(encId)[a], UniversalFormula.Palette(encId)[b]) == 0)
                            {
                                zeroA = UniversalFormula.Palette(encId)[a];
                                zeroB = UniversalFormula.Palette(encId)[b];
                                break;
                            }
                        }
                        if (zeroA != FormulaIcon.None) break;
                    }
                }

                // Find 40-damage pair
                FormulaIcon bestA = FormulaIcon.None, bestB = FormulaIcon.None;
                for (int a = 0; a < 6; a++)
                {
                    for (int b = 0; b < 6; b++)
                    {
                        if (UniversalFormula.Damage(encId, UniversalFormula.Palette(encId)[a], UniversalFormula.Palette(encId)[b]) == 40)
                        {
                            bestA = UniversalFormula.Palette(encId)[a];
                            bestB = UniversalFormula.Palette(encId)[b];
                            break;
                        }
                    }
                    if (bestA != FormulaIcon.None) break;
                }

                while (simRun2.enemyHp > 0 && simRun2.hp > 0 && run2Turns < 200)
                {
                    run2Turns++;
                    if (!firedZero && (encId == 0 || encId == 4) && zeroA != FormulaIcon.None)
                    {
                        firedZero = true;
                        simRun2.AttackFormula(zeroA, zeroB);
                        if (simRun2.phase == BattlePhase.Enemy) simRun2.EnemyAttack();
                        continue;
                    }

                    if (simRun2.energy < 20)
                    {
                        simRun2.Restore();
                        simRun2.Answer(true);
                        simRun2.phase = BattlePhase.Enemy;
                        simRun2.EnemyAttack();
                    }
                    else if (simRun2.shieldHits == 0 && simRun2.energy >= 30)
                    {
                        simRun2.Homeostasis(10, 90, 2);
                        simRun2.EnemyAttack();
                    }
                    else
                    {
                        simRun2.AttackFormula(bestA, bestB);
                        if (simRun2.phase == BattlePhase.Enemy) simRun2.EnemyAttack();
                    }
                }

                if (simRun2.hp <= 0)
                {
                    run2Completed = false;
                    break;
                }
                simRun2.hp = Math.Min(100, simRun2.hp + 25);
                simRun2.energy = Math.Min(60, simRun2.energy + 20);
            }
            Check(!run2Completed, "Run 2 with zero-damage formula incurs severe attrition as expected");

            return "PASS: full six-encounter expansion playthrough and balancing verification";
        }

        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void RejectsTargetInvocation(Action action)
        {
            try { action(); }
            catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException) { return; }
            catch (ArgumentOutOfRangeException) { return; }
            throw new Exception("Invalid catalog argument was accepted");
        }
        static void RejectsArgument(Action action)
        {
            try { action(); }
            catch (ArgumentOutOfRangeException) { return; }
            throw new Exception("Invalid formula data argument was accepted");
        }
    }
}
