using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using Logger = Modding.Logger;

// Taken and modified from https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/FsmUtil.cs

namespace FiveKnights
{
    internal static class FsmUtil
    {
        // ReSharper disable once InconsistentNaming
        private static readonly FieldInfo FsmStringParamsFi = typeof(ActionData).GetField("fsmStringParams", BindingFlags.NonPublic | BindingFlags.Instance);

        [PublicAPI]
        public static void RemoveAction<T>(this PlayMakerFSM fsm, string stateName) where T : FsmStateAction
        {
            FsmState t = fsm.GetState(stateName);

            FsmStateAction[] actions = t.Actions;

            FsmStateAction action = fsm.GetAction<T>(stateName);
            actions = actions.Where(x => x != action).ToArray();
            Log(action.GetType().ToString());

            t.Actions = actions;
        }

        [PublicAPI]
        public static FsmState GetState(this PlayMakerFSM fsm, string stateName)
        {
            return fsm.FsmStates.FirstOrDefault(t => t.Name == stateName);
        }
        
        [PublicAPI]
        public static FsmStateAction GetAction(this PlayMakerFSM fsm, string stateName, int index)
        {
            return fsm.GetState(stateName).Actions[index];
        }

        [PublicAPI]
        public static T GetAction<T>(this PlayMakerFSM fsm, string stateName, int index) where T : FsmStateAction
        {
            return GetAction(fsm, stateName, index) as T;
        }

        [PublicAPI]
        public static T GetAction<T>(this PlayMakerFSM fsm, string stateName) where T : FsmStateAction
        {
            return fsm.GetState(stateName).Actions.FirstOrDefault(x => x is T) as T;
        }

        [PublicAPI]
        public static void InsertAction(this PlayMakerFSM fsm, string stateName, FsmStateAction action, int index)
        {
            FsmState t = fsm.GetState(stateName);

            List<FsmStateAction> actions = t.Actions.ToList();

            actions.Insert(index, action);

            t.Actions = actions.ToArray();

            action.Init(t);
        }

        [PublicAPI]
        public static void InsertAction(this PlayMakerFSM fsm, string state, int ind, FsmStateAction action)
        {
            InsertAction(fsm, state, action, ind);
        }

        [PublicAPI]
        public static void AddTransition(this PlayMakerFSM fsm, string stateName, FsmEvent @event, string toState)
        {
            FsmState t = fsm.GetState(stateName);

            List<FsmTransition> transitions = t.Transitions.ToList();
            transitions.Add(new FsmTransition
            {
                FsmEvent = @event,
                ToState = toState
            });
            t.Transitions = transitions.ToArray();
        }

        [PublicAPI]
        public static void AddTransition(this PlayMakerFSM fsm, string stateName, string eventName, string toState)
        {
            FsmState t = fsm.GetState(stateName);

            List<FsmTransition> transitions = t.Transitions.ToList();
            transitions.Add(new FsmTransition
            {
                FsmEvent = new FsmEvent(eventName),
                ToState = toState
            });
            t.Transitions = transitions.ToArray();
        }

        [PublicAPI]
        public static void RemoveTransitions
        (
            this PlayMakerFSM fsm,
            IEnumerable<string> states,
            IEnumerable<string> transitions
        )
        {
            IEnumerable<string> enumerable = states as string[] ?? states.ToArray();

            foreach (FsmState t in fsm.FsmStates)
            {
                if (!enumerable.Contains(t.Name)) continue;

                t.Transitions = t.Transitions.Where(trans => !transitions.Contains(trans.ToState)).ToArray();
            }
        }

        [PublicAPI]
        public static void RemoveTransition(this PlayMakerFSM fsm, string stateName, string transition)
        {
            FsmState t = fsm.GetState(stateName);

            t.Transitions = t.Transitions.Where(trans => transition != trans.ToState).ToArray();
        }

        [PublicAPI]
        public static void InsertMethod(this PlayMakerFSM fsm, string stateName, int index, Action method)
        {
            InsertAction(fsm, stateName, new InvokeMethod(method), index);
        }

        [PublicAPI]
        public static void InsertCoroutine(this PlayMakerFSM fsm, string stateName, int index, Func<IEnumerator> coro, bool wait = true)
        {
            InsertAction(fsm, stateName, new InvokeCoroutine(coro, wait), index);
        }

        [PublicAPI]
        public static FsmInt GetOrCreateInt(this PlayMakerFSM fsm, string intName)
        {
            var @new = new FsmInt(intName);
            List<FsmInt> intVars = fsm.FsmVariables.IntVariables.ToList();

            FsmInt prev = intVars.FirstOrDefault(x => x.Name == intName);

            if (prev != null)
                return prev;

            intVars.Add(@new);
            fsm.Fsm.Variables.IntVariables = intVars.ToArray();
            return @new;
        }

        public static void AddToSendRandomEvent
        (
            this SendRandomEvent sre,
            string toState,
            float weight,
            [CanBeNull] string eventName = null
        )
        {
            var fsm = sre.Fsm.Owner as PlayMakerFSM;
            string state = sre.State.Name;
            eventName = eventName ?? toState.Split(' ').First();

            List<FsmEvent> events = sre.events.ToList();
            List<FsmFloat> weights = sre.weights.ToList();

            fsm.AddTransition(state, eventName, toState);

            events.Add(fsm.GetState(state).Transitions.Single(x => x.FsmEvent.Name == eventName).FsmEvent);
            weights.Add(weight);

            sre.events = events.ToArray();
            sre.weights = weights.ToArray();
        }
        
        public static void AddToSendRandomEventV2
        (
            this SendRandomEventV2 sre,
            string toState,
            float weight,
            int eventMaxAmount,
            [CanBeNull] string eventName = null
        )
        {
            var fsm = sre.Fsm.Owner as PlayMakerFSM;
            string state = sre.State.Name;
            eventName = eventName ?? toState.Split(' ').First();

            List<FsmEvent> events = sre.events.ToList();
            List<FsmFloat> weights = sre.weights.ToList();
            List<FsmInt> trackingInts = sre.trackingInts.ToList();
            List<FsmInt> eventMax = sre.eventMax.ToList();

            fsm.AddTransition(state, eventName, toState);

            events.Add(fsm.GetState(state).Transitions.Single(x => x.FsmEvent.Name == eventName).FsmEvent);
            weights.Add(weight);
            trackingInts.Add(fsm.GetOrCreateInt($"Ct {eventName}"));
            eventMax.Add(eventMaxAmount);

            sre.events = events.ToArray();
            sre.weights = weights.ToArray();
            sre.trackingInts = trackingInts.ToArray();
            sre.eventMax = eventMax.ToArray();
        }
        
        [PublicAPI]
        public static FsmState CreateState(this PlayMakerFSM fsm, string stateName)
        {
            var state = new FsmState(fsm.Fsm) { Name = stateName };

            List<FsmState> fsmStates = fsm.FsmStates.ToList();
            fsmStates.Add(state);
            fsm.Fsm.States = fsmStates.ToArray();

            return state;
        }

        /* Helper method specifically for creating a series of states leading to a final "Idle" state */
        public static void CreateStates(this PlayMakerFSM fsm, string[] stateNames, string finalState)
        {
            for (int i = 0; i < stateNames.Length; i++)
            {
                string state = stateNames[i];
                fsm.CreateState(state);
                fsm.AddTransition(state, FsmEvent.Finished,
                    i + 1 < stateNames.Length ? stateNames[i + 1] : finalState);
            }
        }
        
        private static void Log(string str)
        {
            Logger.Log("[FSM UTIL]: " + str);
        }
    }

    ///////////////////////
    // Method Invocation //
    ///////////////////////

    public class InvokeMethod : FsmStateAction
    {
        private readonly Action _action;

        public InvokeMethod(Action a)
        {
            _action = a;
        }

        public override void OnEnter()
        {
            _action?.Invoke();
            Finish();
        }
    }

    public class InvokeCoroutine : FsmStateAction
    {
        private readonly Func<IEnumerator> _coro;
        private readonly bool _wait;

        public InvokeCoroutine(Func<IEnumerator> f, bool wait)
        {
            _coro = f;
            _wait = wait;
        }

        private IEnumerator Coroutine()
        {
            yield return _coro?.Invoke();
            Finish();
        }

        public override void OnEnter()
        {
            Fsm.Owner.StartCoroutine(_wait ? Coroutine() : _coro?.Invoke());
            if (!_wait) Finish();
        }
    }
}