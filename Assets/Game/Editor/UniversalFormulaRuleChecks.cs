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
            foreach (int id in new[] { -2, -1, 6, int.MaxValue })
            {
                var icon = (FormulaIcon)id;
                Check(!UniversalFormula.IsValid(icon) && !UniversalFormula.Complete(icon, FormulaIcon.Water) && !UniversalFormula.Complete(FormulaIcon.Water, icon), "Unknown or empty socket completed a formula");
                Check(UniversalFormula.Score(1, 0, icon) == 0, "Unknown icon matched a source");
                if (icon == FormulaIcon.None) continue;
                RejectsArgument(() => UniversalFormula.Title(icon));
                RejectsArgument(() => UniversalFormula.IconName(icon));
                RejectsArgument(() => UniversalFormula.Feedback(0, icon, FormulaIcon.Water));
            }
            foreach (int id in new[] { -1, 2, int.MaxValue })
            {
                RejectsArgument(() => UniversalFormula.Score(id, 0, FormulaIcon.Water));
                RejectsArgument(() => UniversalFormula.Score(0, id, FormulaIcon.Water));
                RejectsArgument(() => UniversalFormula.Damage(id, FormulaIcon.Water, FormulaIcon.Water));
                RejectsArgument(() => UniversalFormula.Feedback(id, FormulaIcon.Water, FormulaIcon.Water));
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
            int[] expectedProfile = { 0, 1, 0, 0, 1, 1 };
            string[] expectedTitle = { "LIPID GOLEM", "GLUCO SLIME", "ROKOK", "BURGER DAN KENTANG", "BOTOL ALKOHOL", "BOSS" };

            var profileMethod = catalogType.GetMethod("LearningProfile");
            var maxHpMethod = catalogType.GetMethod("MaxHp");
            var incomingMethod = catalogType.GetMethod("IncomingDamage");
            var titleMethod = catalogType.GetMethod("Title");

            Check(profileMethod != null && maxHpMethod != null && incomingMethod != null && titleMethod != null, "EncounterCatalog methods missing");

            for (int i = 0; i < 6; i++)
            {
                Check((int)profileMethod.Invoke(null, new object[] { i }) == expectedProfile[i], $"Profile mismatch for {i}");
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
                RejectsTargetInvocation(() => profileMethod.Invoke(null, new object[] { invalid }));
                RejectsTargetInvocation(() => maxHpMethod.Invoke(null, new object[] { invalid }));
                RejectsTargetInvocation(() => incomingMethod.Invoke(null, new object[] { invalid }));
                RejectsTargetInvocation(() => titleMethod.Invoke(null, new object[] { invalid }));

                var model = new BattleModel();
                try { model.Begin(invalid); throw new Exception($"Begin({invalid}) was accepted"); }
                catch (ArgumentOutOfRangeException) { }
            }
            return "PASS: expansion catalog";
        }

        public static string VerifyReusedLearning()
        {
            for (int target = 0; target < 2; target++)
            {
                var sample0 = CombatSamples.Get(0, target);
                var sample1 = CombatSamples.Get(1, target);

                var s2 = CombatSamples.Get(2, target);
                var s3 = CombatSamples.Get(3, target);
                var s4 = CombatSamples.Get(4, target);
                var s5 = CombatSamples.Get(5, target);

                Check(s2.record == sample0.record && s2.traits == sample0.traits, "ID 2 must reuse record and traits from 0");
                Check(s3.record == sample0.record && s3.traits == sample0.traits, "ID 3 must reuse record and traits from 0");
                Check(s4.record == sample1.record && s4.traits == sample1.traits, "ID 4 must reuse record and traits from 1");
                Check(s5.record == sample1.record && s5.traits == sample1.traits, "ID 5 must reuse record and traits from 1");
            }

            for (int first = 0; first < 6; first++)
            {
                for (int second = 0; second < 6; second++)
                {
                    var f = (FormulaIcon)first;
                    var s = (FormulaIcon)second;

                    int dmg0 = UniversalFormula.Damage(0, f, s);
                    int dmg1 = UniversalFormula.Damage(1, f, s);

                    Check(UniversalFormula.Damage(2, f, s) == dmg0, "ID 2 damage must match profile 0");
                    Check(UniversalFormula.Damage(3, f, s) == dmg0, "ID 3 damage must match profile 0");
                    Check(UniversalFormula.Damage(4, f, s) == dmg1, "ID 4 damage must match profile 1");
                    Check(UniversalFormula.Damage(5, f, s) == dmg1, "ID 5 damage must match profile 1");
                }
            }

            var deck = new TriviaDeck(new System.Random(42));
            for (int id = 0; id < 6; id++)
            {
                var seen = new System.Collections.Generic.HashSet<string>();
                string lastQuestion = null;
                for (int q = 0; q < 12; q++)
                {
                    var question = deck.Next(id);
                    Check(!seen.Contains(question.prompt), $"Duplicate question in 12-question cycle for id {id}: {question.prompt}");
                    seen.Add(question.prompt);
                    Check(question.answer >= 0 && question.answer < question.options.Length, "Valid answer index");
                    lastQuestion = question.prompt;
                }
                var thirteenth = deck.Next(id);
                Check(thirteenth.prompt != lastQuestion, $"13th question immediately repeated the 12th for id {id}");
            }

            for (int id = 0; id < 6; id++)
            {
                for (int first = 0; first < 6; first++)
                {
                    for (int second = 0; second < 6; second++)
                    {
                        var f = (FormulaIcon)first;
                        var s = (FormulaIcon)second;

                        var model = new BattleModel();
                        model.Begin(id);
                        Check(model.DiagnoseAll(), "DiagnoseAll");
                        model.energy = 60;
                        int beforeHp = model.enemyHp;
                        int expectedDamage = UniversalFormula.Damage(id, f, s);
                        int dealt = model.AttackFormula(f, s);

                        Check(dealt == expectedDamage, $"Dealt damage mismatch for id {id}");
                        Check(model.energy == 40, $"Energy must be 40 after attack for id {id}");
                        Check(model.enemyHp == Math.Max(0, beforeHp - expectedDamage), $"EnemyHp after attack mismatch for id {id}");
                        Check(model.phase == (model.enemyHp == 0 ? BattlePhase.Victory : BattlePhase.Enemy), $"Phase mismatch for id {id}");
                    }
                }
            }

            for (int id = 0; id < 6; id++)
            {
                var model = new BattleModel();
                model.Begin(id);
                model.DiagnoseAll();
                model.energy = 60;
                int hpBefore = model.enemyHp;
                Check(model.AttackFormula(FormulaIcon.None, FormulaIcon.Water) == -1, "Invalid formula rejected");
                Check(model.energy == 60 && model.enemyHp == hpBefore, "Invalid formula did not spend MP or deal damage");
            }

            return "PASS: reused learning across encounters";
        }

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
                new Vector2(0.76f, 0.76f),
                new Vector2(0.30f, 0.64f)
            };
            for (int i = 0; i < stage0Markers.Length; i++)
            {
                Check(BattleManager.IsWalkable(0, stage0Markers[i]), $"Marker {i} must be walkable in stage 0");
            }

            Check(!BattleManager.IsWalkable(0, new Vector2(0.1f, 0.9f)), "Off-road point must not be walkable in stage 0");
            Check(!BattleManager.IsWalkable(0, new Vector2(0.2f, 0.3f)), "Off-road point (0.2, 0.3) must not be walkable in stage 0");

            Check(!BattleManager.CanTraverse(0, stage0Markers[0], stage0Markers[2]), "Direct traversal across chasm in stage 0 must be rejected");

            Vector2 stage1Spawn = new Vector2(0.08f, 0.48f);
            Vector2 botolPos = new Vector2(0.77f, 0.33f);
            Vector2 bossPos = new Vector2(0.85f, 0.64f);

            Check(BattleManager.IsWalkable(1, stage1Spawn), "Stage 1 spawn must be walkable");
            Check(BattleManager.IsWalkable(1, botolPos), "Stage 1 Botol position must be walkable");
            Check(BattleManager.IsWalkable(1, bossPos), "Stage 1 Boss position must be walkable");

            Check(!BattleManager.IsWalkable(1, new Vector2(0.5f, 0.5f)), "Midpoint chasm in stage 1 must not be walkable");
            Check(!BattleManager.IsWalkable(1, new Vector2(0.1f, 0.1f)), "Bottom-left void in stage 1 must not be walkable");

            Check(!BattleManager.CanTraverse(1, stage1Spawn, botolPos), "Direct traversal from spawn to Botol across chasm must be rejected");
            Check(!BattleManager.CanTraverse(1, stage1Spawn, bossPos), "Direct traversal from spawn to Boss across chasm must be rejected");
            Check(!BattleManager.CanTraverse(1, botolPos, bossPos), "Direct traversal between Botol and Boss across chasm must be rejected");

            Check(!BattleManager.CanTraverse(-1, stage1Spawn, stage1Spawn), "Invalid stage -1 must be rejected");
            Check(!BattleManager.CanTraverse(2, stage1Spawn, stage1Spawn), "Invalid stage 2 must be rejected");
            Check(!BattleManager.CanTraverse(1, new Vector2(float.NaN, 0), stage1Spawn), "NaN must be rejected");
            Check(!BattleManager.CanTraverse(1, stage1Spawn, new Vector2(float.PositiveInfinity, 0)), "Infinity must be rejected");

            Check(BattleManager.CanTraverse(1, stage1Spawn, new Vector2(0.12f, 0.53f)), "Spawn to spawn_path forward");
            Check(BattleManager.CanTraverse(1, new Vector2(0.12f, 0.53f), stage1Spawn), "Spawn to spawn_path backward");
            Check(BattleManager.CanTraverse(1, new Vector2(0.18f, 0.44f), new Vector2(0.28f, 0.43f)), "Lower bridge forward");
            Check(BattleManager.CanTraverse(1, new Vector2(0.28f, 0.43f), new Vector2(0.18f, 0.44f)), "Lower bridge backward");
            Check(BattleManager.CanTraverse(1, new Vector2(0.42f, 0.66f), new Vector2(0.52f, 0.69f)), "Upper bridge forward");
            Check(BattleManager.CanTraverse(1, new Vector2(0.52f, 0.69f), new Vector2(0.42f, 0.66f)), "Upper bridge backward");

            string routeJsonPath = "docs/superpowers/verification/two-stage-existing-gameplay/route.json";
            Check(File.Exists(routeJsonPath), "route.json must exist");

            return "PASS: expansion traversal across both stages, chasm rejection, and edge connectivity";
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
