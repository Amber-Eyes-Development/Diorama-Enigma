using System.Collections.Generic;
using Extensions.Data;
using Extensions.Log;

namespace Extensions.Sequences
{
    /// <summary>
    /// Сохранение/восстановление графа последовательности через общий <see cref="ISaveService"/>.
    /// Capture: граф → снапшот; Restore: снапшот → новый граф (для процедурных);
    /// ApplyState: наложить состояние снапшота на уже построенный граф по id (для авторских — переживает правки дизайнера).
    /// </summary>
    public sealed class SequencePersistence
    {
        private readonly ISaveService save;
        private readonly string profile;

        /// <summary> Новый менеджер персистентности </summary>
        public SequencePersistence(ISaveService save, string profile = null)
        {
            this.save = save;
            this.profile = profile;
        }

        #region Save / Load

        /// <summary> Захватить и сохранить снапшот последовательности по её Id </summary>
        public bool Save(Sequence sequence)
        {
            if (sequence == null || save == null) return false;
            return save.Save(Capture(sequence), Key(sequence.Id), profile);
        }

        /// <summary> Загрузить снапшот по Id последовательности </summary>
        public bool TryLoad(string sequenceId, out SequenceSnapshot snapshot)
        {
            snapshot = null;
            if (save == null || string.IsNullOrEmpty(sequenceId)) return false;
            if (!save.Exists(Key(sequenceId), profile)) return false;

            snapshot = save.Load<SequenceSnapshot>(Key(sequenceId), null, profile);
            return snapshot != null;
        }

        private static string Key(string id) => "sequence:" + id;

        #endregion

        #region Capture

        /// <summary> Захватить снапшот (структура + состояние) </summary>
        public SequenceSnapshot Capture(Sequence sequence)
        {
            var catalog = new Dictionary<string, Step>();
            foreach (var entry in sequence.Steps)
                CollectSteps(entry?.Step, catalog);

            var snapshot = new SequenceSnapshot { Id = sequence.Id };

            var steps = new List<SequenceSnapshot.StepData>(catalog.Count);
            foreach (var step in catalog.Values)
                steps.Add(CaptureStep(step));
            snapshot.Steps = steps.ToArray();

            var entries = new List<SequenceSnapshot.EntryData>();
            foreach (var entry in sequence.Steps)
            {
                if (entry?.Step == null) continue;
                entries.Add(CaptureEntry(entry));
            }
            snapshot.Entries = entries.ToArray();

            return snapshot;
        }

        private static void CollectSteps(Step step, Dictionary<string, Step> catalog)
        {
            if (step == null || string.IsNullOrEmpty(step.Id)) return;
            if (catalog.ContainsKey(step.Id)) return;

            catalog[step.Id] = step;

            if (step is CompositeStep composite)
                foreach (var child in composite.Children)
                    CollectSteps(child.Step, catalog);
        }

        private static SequenceSnapshot.StepData CaptureStep(Step step)
        {
            var data = new SequenceSnapshot.StepData
            {
                Id = step.Id,
                Irreversible = step.Irreversible,
            };

            switch (step)
            {
                case BoolStep boolStep:
                    data.Kind = (int)SequenceSnapshot.StepKind.Bool;
                    data.BoolValue = boolStep.CaptureValue();
                    data.CompletionState = boolStep.CompletionState;
                    break;

                case CounterStep counterStep:
                    data.Kind = (int)SequenceSnapshot.StepKind.Counter;
                    data.Count = counterStep.CaptureCount();
                    data.Target = counterStep.Target;
                    break;

                case CompositeStep composite:
                    data.Kind = (int)SequenceSnapshot.StepKind.Composite;
                    data.Mode = (int)composite.Mode;
                    data.N = composite.N;
                    int count = composite.Children.Count;
                    data.ChildIds = new string[count];
                    data.ChildTriggers = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        data.ChildIds[i] = composite.Children[i].Step?.Id;
                        data.ChildTriggers[i] = (int)composite.Children[i].Trigger;
                    }
                    break;

                default:
                    data.Kind = (int)SequenceSnapshot.StepKind.Custom;
                    ServiceDebug.LogError<SequencePersistence>(
                        $"Незнакомый тип шага для снапшота: {step.GetType().Name}");
                    break;
            }

            return data;
        }

        private static SequenceSnapshot.EntryData CaptureEntry(StepEntry entry)
        {
            var data = new SequenceSnapshot.EntryData
            {
                StepId = entry.Step.Id,
                Group = entry.GroupIndex,
                Availability = (int)entry.Availability,
                InteractableAfterCompletion = entry.InteractableAfterCompletion,
            };

            var gates = new List<SequenceSnapshot.GateData>();
            foreach (var gate in entry.Gates)
            {
                if (gate is StepStateGate stepGate)
                {
                    var ids = new List<string>();
                    foreach (var required in stepGate.RequiredSteps)
                        if (required != null) ids.Add(required.Id);

                    gates.Add(new SequenceSnapshot.GateData
                    {
                        Kind = (int)SequenceSnapshot.GateKind.StepState,
                        Trigger = (int)stepGate.Trigger,
                        StepIds = ids.ToArray(),
                    });
                }
            }
            data.Gates = gates.ToArray();

            var effects = new List<SequenceSnapshot.EffectData>();
            foreach (var effectEntry in entry.Effects)
            {
                if (effectEntry?.Effect is SetStepEffect set)
                {
                    effects.Add(new SequenceSnapshot.EffectData
                    {
                        Trigger = (int)effectEntry.Trigger,
                        Kind = (int)SequenceSnapshot.EffectKind.SetStep,
                        TargetId = set.Target?.Id,
                        Value = set.Value,
                    });
                }
                // Поведенческие эффекты (CallbackEffect и т.п.) не сериализуются — переподвязывает владелец
            }
            data.Effects = effects.ToArray();

            return data;
        }

        #endregion

        #region Restore

        /// <summary> Полностью восстановить граф из снапшота (для процедурных квестов) </summary>
        public Sequence Restore(SequenceSnapshot snapshot)
        {
            if (snapshot == null) return null;

            var byId = new Dictionary<string, Step>();

            // Pass 1: листовые шаги
            foreach (var data in snapshot.Steps)
            {
                if (data.Kind == (int)SequenceSnapshot.StepKind.Composite) continue;
                var step = BuildLeafStep(data);
                if (step != null) byId[data.Id] = step;
            }

            // Pass 2+: композиты (могут ссылаться друг на друга) — фикспоинт
            var pending = new List<SequenceSnapshot.StepData>();
            foreach (var data in snapshot.Steps)
                if (data.Kind == (int)SequenceSnapshot.StepKind.Composite)
                    pending.Add(data);

            int guard = pending.Count + 1;
            while (pending.Count > 0 && guard-- > 0)
            {
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    var data = pending[i];
                    if (!AllChildrenReady(data, byId)) continue;
                    byId[data.Id] = BuildComposite(data, byId);
                    pending.RemoveAt(i);
                }
            }
            if (pending.Count > 0)
                ServiceDebug.LogError<SequencePersistence>(
                    "Не удалось восстановить композиты (циклические/недостающие ссылки)");

            var entries = new List<StepEntry>(snapshot.Entries.Length);
            foreach (var entryData in snapshot.Entries)
            {
                if (!byId.TryGetValue(entryData.StepId, out var step)) continue;
                entries.Add(BuildEntry(entryData, step, byId));
            }

            return new Sequence(snapshot.Id, entries);
        }

        /// <summary> Наложить состояние снапшота на уже построенный граф (по id) </summary>
        public void ApplyState(Sequence sequence, SequenceSnapshot snapshot)
        {
            if (sequence == null || snapshot == null) return;

            var byId = new Dictionary<string, Step>();
            foreach (var entry in sequence.Steps)
                CollectSteps(entry?.Step, byId);

            foreach (var data in snapshot.Steps)
                if (byId.TryGetValue(data.Id, out var step))
                    ApplyStepState(step, data);
        }

        private static Step BuildLeafStep(SequenceSnapshot.StepData data)
        {
            switch (data.Kind)
            {
                case (int)SequenceSnapshot.StepKind.Bool:
                    var boolStep = new BoolStep(data.Id, data.CompletionState, irreversible: data.Irreversible);
                    boolStep.RestoreValue(data.BoolValue);
                    return boolStep;

                case (int)SequenceSnapshot.StepKind.Counter:
                    var counterStep = new CounterStep(data.Id, data.Target, irreversible: data.Irreversible);
                    counterStep.RestoreCount(data.Count);
                    return counterStep;

                default:
                    ServiceDebug.LogError<SequencePersistence>(
                        $"Неизвестный StepKind={data.Kind} (id={data.Id})");
                    return null;
            }
        }

        private static bool AllChildrenReady(SequenceSnapshot.StepData data, Dictionary<string, Step> byId)
        {
            if (data.ChildIds == null) return true;

            foreach (var childId in data.ChildIds)
                if (!string.IsNullOrEmpty(childId) && !byId.ContainsKey(childId)) return false;

            return true;
        }

        private static CompositeStep BuildComposite(SequenceSnapshot.StepData data, Dictionary<string, Step> byId)
        {
            var children = new List<CompositeStep.Child>();
            if (data.ChildIds != null)
            {
                for (int i = 0; i < data.ChildIds.Length; i++)
                {
                    byId.TryGetValue(data.ChildIds[i] ?? "", out var childStep);
                    var trigger = (TriggerKind)(data.ChildTriggers != null && i < data.ChildTriggers.Length
                        ? data.ChildTriggers[i]
                        : 0);
                    children.Add(new CompositeStep.Child(childStep, trigger));
                }
            }

            return new CompositeStep(data.Id, (CompletionMode)data.Mode, data.N, children, data.Irreversible);
        }

        private static StepEntry BuildEntry(SequenceSnapshot.EntryData data, Step step, Dictionary<string, Step> byId)
        {
            var gates = new List<Gate>();
            if (data.Gates != null)
                foreach (var gateData in data.Gates)
                    if (gateData.Kind == (int)SequenceSnapshot.GateKind.StepState)
                    {
                        var required = new List<Step>();
                        if (gateData.StepIds != null)
                            foreach (var sid in gateData.StepIds)
                                if (byId.TryGetValue(sid ?? "", out var rs)) required.Add(rs);

                        gates.Add(new StepStateGate((TriggerKind)gateData.Trigger, required));
                    }

            var effects = new List<EffectEntry>();
            if (data.Effects != null)
                foreach (var effData in data.Effects)
                    if (effData.Kind == (int)SequenceSnapshot.EffectKind.SetStep
                        && byId.TryGetValue(effData.TargetId ?? "", out var target)
                        && target is BoolStep boolTarget)
                        effects.Add(new EffectEntry((TriggerKind)effData.Trigger, new SetStepEffect(boolTarget, effData.Value)));

            return new StepEntry(
                step,
                data.Group,
                (GroupAvailability)data.Availability,
                data.InteractableAfterCompletion,
                gates,
                effects);
        }

        private static void ApplyStepState(Step step, SequenceSnapshot.StepData data)
        {
            switch (step)
            {
                case BoolStep boolStep: boolStep.RestoreValue(data.BoolValue); break;
                case CounterStep counterStep: counterStep.RestoreCount(data.Count); break;
                case CompositeStep _: break; // завершённость производная — восстанавливается из детей
            }
        }

        #endregion
    }
}
