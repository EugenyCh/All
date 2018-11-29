using System;
using System.Collections.Generic;
using System.Linq;

namespace StateMachine
{
    public enum Quantifier
    {
        LAZY,
        GREEDY
    }

    public delegate void StateAction<T>(State<T> state) where T : IComparable<T>;
    public delegate void TransitionAction<T>(State<T> stateFrom, State<T> stateTo) where T : IComparable<T>;
    public delegate bool TransitionTracer<T>(State<T> stateFrom, State<T> stateTo, ref double weight) where T : IComparable<T>;

    public class State<T> : IComparable<State<T>> where T : IComparable<T>
    {
        public T Identifier { get; set; }
        public StateAction<T> SAction { get; set; }
        public void Do(State<T> state)
        {
            SAction?.Invoke(state);
        }
        public bool Equals(State<T> obj)
        {
            return Identifier.Equals(obj.Identifier);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public int CompareTo(State<T> other)
        {
            return Identifier.CompareTo(other.Identifier);
        }
    }

    public class Transition<T> : IComparable<Transition<T>> where T : IComparable<T>
    {
        public State<T> StateFrom = new State<T>();
        public State<T> StateTo = new State<T>();
        public TransitionTracer<T> Tracer { get; set; }
        public bool Trace(State<T> stateFrom, State<T> stateTo, out double weight)
        {
            weight = 0.0;
            return Tracer?.Invoke(stateFrom, stateTo, ref weight) ?? true;
        }
        public TransitionAction<T> TAction { get; set; }
        public void Do(State<T> stateFrom, State<T> stateTo)
        {
            TAction?.Invoke(stateFrom, stateTo);
        }
        public bool Equals(Transition<T> obj)
        {
            return StateFrom.Equals(obj.StateFrom)
                 & StateTo.Equals(obj.StateTo);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public int CompareTo(Transition<T> other)
        {
            int rFrom = StateFrom.CompareTo(other.StateFrom);
            int rTo = StateTo.CompareTo(other.StateTo);
            return (rFrom == 0 & rTo == 0) ? 0 : (rFrom != 0 ? rFrom : rTo);
        }
    }

    public class StateMachine<T> where T : IComparable<T>
    {
        State<T> zeroState = null;
        SortedSet<State<T>> states = new SortedSet<State<T>>();
        SortedSet<Transition<T>> transitions = new SortedSet<Transition<T>>();
        public Quantifier QuantifierDefault { get; set; } = Quantifier.LAZY;
        public StateMachine() { }
        #region AddState(...)
        public bool AddState(T identifier)
        {
            return states.Add(new State<T>
            {
                Identifier = identifier
            });
        }
        public bool AddState(T identifier, StateAction<T> stateAction)
        {
            return states.Add(new State<T>
            {
                Identifier = identifier,
                SAction = stateAction
            });
        }
        public bool AddState(State<T> state)
        {
            return states.Add(state);
        }
        #endregion
        #region GetState(...)
        public State<T> GetState(T identifier)
        {
            return (from state in states
                    where state.Identifier.Equals(identifier)
                    select state).FirstOrDefault();
        }
        public State<T> GetState(State<T> otherState)
        {
            return (from state in states
                    where state.Equals(otherState)
                    select state).FirstOrDefault();
        }
        #endregion
        #region SetStateAction(...)
        public bool SetStateAction(T identifier, StateAction<T> stateAction)
        {
            State<T> state = GetState(identifier);
            if (state == null | stateAction == null)
                return false;
            state.SAction = stateAction;
            return true;
        }
        public bool SetStateAction(State<T> otherState, StateAction<T> stateAction)
        {
            State<T> state = GetState(otherState);
            if (state == null | stateAction == null)
                return false;
            state.SAction = stateAction;
            return true;
        }
        #endregion
        #region RemoveState(...)
        public bool RemoveState(T identifier)
        {
            return states.Remove(new State<T>
            {
                Identifier = identifier
            });
        }
        public bool RemoveState(State<T> state)
        {
            return states.Remove(state);
        }
        #endregion
        #region ZeroState(...)
        public bool ZeroState(T identifier)
        {
            State<T> valState = new State<T>
            {
                Identifier = identifier
            };
            if (GetState(valState) != null)
                return false;
            zeroState = valState;
            return AddState(zeroState);
        }
        public bool ZeroState(T identifier, StateAction<T> stateAction)
        {
            State<T> valState = new State<T>
            {
                Identifier = identifier,
                SAction = stateAction
            };
            if (GetState(valState) != null)
                return false;
            zeroState = valState;
            return AddState(zeroState);
        }
        public bool ZeroState(State<T> state)
        {
            if (GetState(state) != null)
                return false;
            zeroState = state;
            return AddState(zeroState);
        }
        #endregion
        #region AddTransition(...)
        public bool AddTransition(T identifierFrom, T identifierTo)
        {
            State<T> stateFrom = GetState(identifierFrom);
            State<T> stateTo = GetState(identifierTo);
            if (stateFrom == null | stateTo == null)
                return false;
            return transitions.Add(new Transition<T>
            {
                StateFrom = stateFrom,
                StateTo = stateTo
            });
        }
        public bool AddTransition(T identifierFrom, T identifierTo, TransitionTracer<T> transitionTracer)
        {
            State<T> stateFrom = GetState(identifierFrom);
            State<T> stateTo = GetState(identifierTo);
            if (stateFrom == null | stateTo == null)
                return false;
            return transitions.Add(new Transition<T>
            {
                StateFrom = stateFrom,
                StateTo = stateTo,
                Tracer = transitionTracer
            });
        }
        public bool AddTransition(State<T> otherStateFrom, State<T> otherStateTo)
        {
            State<T> stateFrom = GetState(otherStateFrom);
            State<T> stateTo = GetState(otherStateTo);
            if (stateFrom == null | stateTo == null)
                return false;
            return transitions.Add(new Transition<T>
            {
                StateFrom = stateFrom,
                StateTo = stateTo
            });
        }
        public bool AddTransition(State<T> otherStateFrom, State<T> otherStateTo, TransitionTracer<T> transitionTracer)
        {
            State<T> stateFrom = GetState(otherStateFrom);
            State<T> stateTo = GetState(otherStateTo);
            if (stateFrom == null | stateTo == null)
                return false;
            return transitions.Add(new Transition<T>
            {
                StateFrom = stateFrom,
                StateTo = stateTo,
                Tracer = transitionTracer
            });
        }
        public bool AddTransition(T identifierFrom, T identifierTo, TransitionTracer<T> transitionTracer, TransitionAction<T> transitionAction)
        {
            State<T> stateFrom = GetState(identifierFrom);
            State<T> stateTo = GetState(identifierTo);
            if (stateFrom == null | stateTo == null)
                return false;
            return transitions.Add(new Transition<T>
            {
                StateFrom = stateFrom,
                StateTo = stateTo,
                Tracer = transitionTracer,
                TAction = transitionAction
            });
        }
        public bool AddTransition(State<T> otherStateFrom, State<T> otherStateTo, TransitionTracer<T> transitionTracer, TransitionAction<T> transitionAction)
        {
            State<T> stateFrom = GetState(otherStateFrom);
            State<T> stateTo = GetState(otherStateTo);
            if (stateFrom == null | stateTo == null)
                return false;
            return transitions.Add(new Transition<T>
            {
                StateFrom = stateFrom,
                StateTo = stateTo,
                Tracer = transitionTracer,
                TAction = transitionAction
            });
        }
        public bool AddTransition(Transition<T> transition)
        {
            return transitions.Add(transition);
        }
        #endregion
        #region GetTransition(...)
        public Transition<T> GetTransition(T identifierFrom, T identifierTo)
        {
            State<T> stateFrom = new State<T>
            {
                Identifier = identifierFrom
            };
            State<T> stateTo = new State<T>
            {
                Identifier = identifierTo
            };
            return (from transition in transitions
                    where transition.StateFrom.Equals(stateFrom)
                    & transition.StateTo.Equals(stateTo)
                    select transition).FirstOrDefault();
        }
        public Transition<T> GetTransition(State<T> otherStateFrom, State<T> otherStateTo)
        {
            State<T> stateFrom = GetState(otherStateFrom);
            State<T> stateTo = GetState(otherStateTo);
            return (from transition in transitions
                    where transition.StateFrom.Equals(stateFrom)
                    & transition.StateTo.Equals(stateTo)
                    select transition).FirstOrDefault();
        }
        public Transition<T> GetTransition(Transition<T> otherTransition)
        {
            return (from transition in transitions
                    where transition.Equals(otherTransition)
                    select transition).FirstOrDefault();
        }
        #endregion
        #region SetTransitionTracer(...)
        public bool SetTransitionTracer(T identifierFrom, T identifierTo, TransitionTracer<T> transitionTracer)
        {
            Transition<T> transition = GetTransition(identifierFrom, identifierTo);
            if (transition == null)
                return false;
            transition.Tracer = transitionTracer;
            return true;
        }
        public bool SetTransitionTracer(State<T> stateFrom, State<T> stateTo, TransitionTracer<T> transitionTracer)
        {
            Transition<T> transition = GetTransition(stateFrom, stateTo);
            if (transition == null)
                return false;
            transition.Tracer = transitionTracer;
            return true;
        }
        public bool SetTransitionTracer(Transition<T> otherTransition, TransitionTracer<T> transitionTracer)
        {
            Transition<T> transition = GetTransition(otherTransition);
            if (transition == null)
                return false;
            transition.Tracer = transitionTracer;
            return true;
        }
        #endregion
        #region SetTransitionAction(...)
        public bool SetTransitionAction(T identifierFrom, T identifierTo, TransitionAction<T> transitionAction)
        {
            Transition<T> transition = GetTransition(identifierFrom, identifierTo);
            if (transition == null)
                return false;
            transition.TAction = transitionAction;
            return true;
        }
        public bool SetTransitionAction(State<T> stateFrom, State<T> stateTo, TransitionAction<T> transitionAction)
        {
            Transition<T> transition = GetTransition(stateFrom, stateTo);
            if (transition == null)
                return false;
            transition.TAction = transitionAction;
            return true;
        }
        public bool SetTransitionAction(Transition<T> otherTransition, TransitionAction<T> transitionAction)
        {
            Transition<T> transition = GetTransition(otherTransition);
            if (transition == null)
                return false;
            transition.TAction = transitionAction;
            return true;
        }
        #endregion
        #region RemoveTransition(...)
        public bool RemoveTransition(T identifierFrom, T identifierTo)
        {
            State<T> stateFrom = GetState(identifierFrom);
            State<T> stateTo = GetState(identifierTo);
            return transitions.Remove(new Transition<T>
            {
                StateFrom = stateFrom,
                StateTo = stateTo
            });
        }
        public bool RemoveTransition(State<T> otherStateFrom, State<T> otherStateTo)
        {
            State<T> stateFrom = GetState(otherStateFrom);
            State<T> stateTo = GetState(otherStateTo);
            return transitions.Remove(new Transition<T>
            {
                StateFrom = stateFrom,
                StateTo = stateTo
            });
        }
        #endregion
        #region GetTransitionStartingWith(...)
        public IEnumerable<Transition<T>> GetTransitionStartingWith(T identifierFrom)
        {
            State<T> stateFrom = GetState(identifierFrom);
            return from transition in transitions
                   where transition.StateFrom.Equals(stateFrom)
                   select transition;
        }
        public IEnumerable<Transition<T>> GetTransitionStartingWith(State<T> otherStateFrom)
        {
            State<T> stateFrom = GetState(otherStateFrom);
            return from transition in transitions
                   where transition.StateFrom.Equals(stateFrom)
                   select transition;
        }
        #endregion
        public bool Startup()
        {
            if (zeroState == null)
                return false;
            zeroState.Do(zeroState);
            State<T> stateFrom = zeroState;
            State<T> stateTo = null;
            var ts = new List<Transition<T>>(GetTransitionStartingWith(stateFrom));
            if (QuantifierDefault == Quantifier.LAZY)
            {
                while (ts.Count() > 0)
                {
                    bool r = false;
                    var tc = ts[0];
                    foreach (var t in ts)
                    {
                        double weight;
                        r = t.Trace(stateFrom, t.StateTo, out weight);
                        if (r)
                        {
                            stateTo = t.StateTo;
                            tc = t;
                            break;
                        }
                    }
                    if (r)
                    {
                        tc.Do(stateFrom, stateTo);
                        stateTo.Do(stateTo);
                        stateFrom = stateTo;
                        ts = new List<Transition<T>>(GetTransitionStartingWith(stateFrom));
                    }
                    else
                        return false;
                }
            }
            else if (QuantifierDefault == Quantifier.GREEDY)
            {
                while (ts.Count() > 0)
                {
                    double bestWeight = double.NegativeInfinity;
                    bool r = false;
                    var tc = ts[0];
                    foreach (var t in ts)
                    {
                        double weight;
                        r = t.Trace(stateFrom, t.StateTo, out weight);
                        if (r)
                            if (weight > bestWeight)
                            {
                                bestWeight = weight;
                                stateTo = t.StateTo;
                                tc = t;
                            }
                    }
                    if (r)
                    {
                        tc.Do(stateFrom, stateTo);
                        stateTo.Do(stateTo);
                        stateFrom = stateTo;
                        ts = new List<Transition<T>>(GetTransitionStartingWith(stateFrom));
                    }
                    else
                        return false;
                }
            }
            return true;
        }
    }
}
