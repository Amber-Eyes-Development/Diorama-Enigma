using System.Collections.Generic;
using Extensions.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Самопроверка POCO-ядра без сцены: строит последовательности в коде, прогоняет сценарии с in-memory сейвом.
    /// </summary>
    public static class SequencesSelfTest
    {
        private static int passed;
        private static int failed;

        [MenuItem("Tools/Sequences/Run Self-Test")]
        public static void Run()
        {
            passed = 0;
            failed = 0;

            LinearProgress();
            CompositeAtLeast();
            GateBlocksUntilDependency();
            EffectsFireOnce();
            ReconcileOnMutation();
            ResumeAuthoredOverlay();
            ResumeProceduralRebuild();

            if (failed == 0)
                Debug.Log($"<b>Sequences self-test: ВСЕ {passed} OK</b>");
            else
                Debug.LogError($"<b>Sequences self-test: {passed} OK, {failed} ОШИБОК</b>");
        }

        #region 1. Линейный прогресс

        private static void LinearProgress()
        {
            var a = new BoolStep("lin.a");
            var b = new BoolStep("lin.b");
            var sequence = new Sequence("selftest.linear", new[]
            {
                new StepEntry(a, 0),
                new StepEntry(b, 1),
            });

            var engine = new SequenceEngine(sequence);
            engine.Start();

            Check("linear: этап 0 на старте", engine.CurrentStage == 0);
            Check("linear: a активен", a.IsActive);
            Check("linear: b не активен", !b.IsActive);

            a.SetValue(true);

            Check("linear: этап сдвинулся к 1", engine.CurrentStage == 1);
            Check("linear: b активен после a", b.IsActive);

            b.SetValue(true);
            Check("linear: последовательность завершена", engine.IsCompleted);
        }

        #endregion

        #region 2. Композит AtLeast(N)

        private static void CompositeAtLeast()
        {
            var c1 = new BoolStep("comp.c1");
            var c2 = new BoolStep("comp.c2");
            var c3 = new BoolStep("comp.c3");
            var composite = new CompositeStep("comp.root", CompletionMode.AtLeast, 2, new[]
            {
                new CompositeStep.Child(c1, TriggerKind.Completed),
                new CompositeStep.Child(c2, TriggerKind.Completed),
                new CompositeStep.Child(c3, TriggerKind.Completed),
            });

            var sequence = new Sequence("selftest.composite", new[] { new StepEntry(composite, 0) });
            var engine = new SequenceEngine(sequence);
            engine.Start();

            Check("composite: не завершён изначально", !composite.IsCompleted);

            c1.SetValue(true);
            Check("composite: 1/3 — мало", !composite.IsCompleted);

            c2.SetValue(true);
            Check("composite: 2/3 — AtLeast(2) выполнен", composite.IsCompleted);
        }

        #endregion

        #region 3. Гейт держит шаг до зависимости

        private static void GateBlocksUntilDependency()
        {
            var a = new BoolStep("gate.a");
            var b = new BoolStep("gate.b");
            var gate = new StepStateGate(TriggerKind.Completed, new Step[] { a });

            var sequence = new Sequence("selftest.gate", new[]
            {
                new StepEntry(a, 0),
                new StepEntry(b, 0, gates: new Gate[] { gate }),
            });

            var engine = new SequenceEngine(sequence);
            engine.Start();

            Check("gate: a разблокирован", a.IsUnlocked);
            Check("gate: b заблокирован гейтом", !b.IsUnlocked);

            a.SetValue(true);
            Check("gate: b разблокирован после завершения a", b.IsUnlocked);
        }

        #endregion

        #region 4. Эффекты срабатывают один раз

        private static void EffectsFireOnce()
        {
            int rewardCount = 0;
            var x = new BoolStep("eff.x");
            var y = new BoolStep("eff.y");

            var sequence = new Sequence("selftest.effects", new[]
            {
                new StepEntry(x, 0, effects: new[]
                {
                    new EffectEntry(TriggerKind.Completed, new CallbackEffect(() => rewardCount++)),
                    new EffectEntry(TriggerKind.Completed, new SetStepEffect(y, true)),
                }),
                new StepEntry(y, 1),
            });

            var engine = new SequenceEngine(sequence);
            engine.Start();

            x.SetValue(true);

            Check("effects: колбэк сработал один раз", rewardCount == 1);
            Check("effects: SetStepEffect выставил y", y.IsCompleted);
        }

        #endregion

        #region 5. Reconcile при мутации

        private static void ReconcileOnMutation()
        {
            var m1 = new BoolStep("mut.1");
            var sequence = new Sequence("selftest.mutate", new[] { new StepEntry(m1, 0) });

            var engine = new SequenceEngine(sequence);
            engine.Start();

            var m2 = new BoolStep("mut.2");
            sequence.Add(new StepEntry(m2, 0));
            engine.Refresh();

            Check("mutate: добавленный шаг активен после Refresh", m2.IsActive);
        }

        #endregion

        #region 6. Резюм авторского (оверлей)

        private static Sequence BuildOverlay() => new Sequence("selftest.overlay", new[]
        {
            new StepEntry(new BoolStep("ov.a"), 0),
            new StepEntry(new BoolStep("ov.b"), 1),
        });

        private static void ResumeAuthoredOverlay()
        {
            var persistence = new SequencePersistence(new MemorySaveService());

            var first = BuildOverlay();
            var firstEngine = new SequenceEngine(first, persistence);
            firstEngine.Start();
            first.Step<BoolStep>("ov.a").SetValue(true);

            // новая сессия: свежий граф + наложение состояния
            var second = BuildOverlay();
            Check("overlay: снапшот существует", persistence.TryLoad("selftest.overlay", out var snapshot));
            persistence.ApplyState(second, snapshot);

            var secondEngine = new SequenceEngine(second, persistence);
            secondEngine.Start();

            Check("overlay: возобновлено на этапе 1", secondEngine.CurrentStage == 1);
            Check("overlay: a завершён после резюма", second.Step<BoolStep>("ov.a").IsCompleted);
        }

        #endregion

        #region 7. Резюм процедурного (полный реконструкт)

        private static void ResumeProceduralRebuild()
        {
            var persistence = new SequencePersistence(new MemorySaveService());

            var counter = new CounterStep("proc.count", 3);
            var turnIn = new BoolStep("proc.turnin");
            var generated = new Sequence("selftest.proc", new[]
            {
                new StepEntry(counter, 0),
                new StepEntry(turnIn, 1),
            });

            var engine = new SequenceEngine(generated, persistence);
            engine.Start();
            counter.Add(2); // 2/3 — прогресс без завершения

            // выбрасываем граф, восстанавливаем только из снапшота
            Check("proc: снапшот существует", persistence.TryLoad("selftest.proc", out var snapshot));
            var restored = persistence.Restore(snapshot);
            var restoredCounter = restored.Step<CounterStep>("proc.count");

            Check("proc: прогресс счётчика пережил рестарт", restoredCounter != null && restoredCounter.Count == 2);
            Check("proc: порог счётчика пережил рестарт", restoredCounter != null && restoredCounter.Target == 3);

            var restoredEngine = new SequenceEngine(restored, persistence);
            restoredEngine.Start();

            Check("proc: возобновлено на этапе 0 (счётчик не добран)", restoredEngine.CurrentStage == 0);

            restoredCounter.Add(1); // 3/3
            Check("proc: счётчик завершился", restoredCounter.IsCompleted);
            Check("proc: переход на этап 1", restoredEngine.CurrentStage == 1);
        }

        #endregion

        #region Helpers

        private static void Check(string name, bool condition)
        {
            if (condition)
            {
                passed++;
                Debug.Log($"PASS: {name}");
            }
            else
            {
                failed++;
                Debug.LogError($"FAIL: {name}");
            }
        }

        /// <summary> In-memory реализация шва: честный round-trip через JSON, без диска </summary>
        private sealed class MemorySaveService : ISaveService
        {
            private readonly Dictionary<string, string> data = new();

            public bool Save<T>(T value, string key, string profile = null)
            {
                data[Key(key, profile)] = JsonConvert.SerializeObject(value);
                return true;
            }

            public T Load<T>(string key, T defaultValue = default, string profile = null) =>
                data.TryGetValue(Key(key, profile), out var json)
                    ? JsonConvert.DeserializeObject<T>(json)
                    : defaultValue;

            public bool Exists(string key, string profile = null) => data.ContainsKey(Key(key, profile));

            private static string Key(string key, string profile) => (profile ?? string.Empty) + ":" + key;
        }

        #endregion
    }
}
