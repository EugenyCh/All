using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StateMachine;

namespace DimN
{
    class Program
    {
        static public void TellState(State<string> state)
        {
            Console.WriteLine($"TellState: {state.Identifier}");
        }

        static public void TellTransition(State<string> stateFrom, State<string> stateTo)
        {
            Console.WriteLine($"TellTransition: {stateFrom.Identifier}, {stateTo.Identifier}");
        }

        static public bool ToWeight(State<string> stateFrom, State<string> stateTo, out double weight)
        {
            weight = 0.0;
            if (stateTo.Identifier == "This is Y")
            {
                weight = double.Epsilon;
                return true;
            }
            return true;
        }

        static public void Main(string[] args)
        {
            StateMachine<string> machine = new StateMachine<string>();
            machine.QuantifierDefault = Quantifier.GREEDY;
            machine.ZeroState("Start", TellState);
            machine.AddState("This is X", TellState);
            machine.AddState("This is Y", TellState);
            machine.AddState("This is X1", TellState);
            machine.AddState("This is X2", TellState);
            machine.AddState("This is Z", TellState);
            machine.AddTransition("Start", "This is X", ToWeight, TellTransition);
            machine.AddTransition("Start", "This is Y", ToWeight, TellTransition);
            machine.AddTransition("This is X", "This is X1", ToWeight, TellTransition);
            machine.AddTransition("This is X", "This is X2", ToWeight, TellTransition);
            machine.AddTransition("This is Y", "This is Z", ToWeight, TellTransition);
            Console.WriteLine(machine.Startup());
        }
    }
}