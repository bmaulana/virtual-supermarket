using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;

namespace UCL.COMPGV07
{
    class Program
    {
        /* These types have the exact same member variables as the Unity project. They also have helpful member functions not required in Unity. */

#pragma warning disable 0649    // disable 'is never assigned to' warning because the following members are only meant to be populated from the file

        [Serializable]
        public struct Purchase
        {
            public int Code;
            public float Time;
        }

        [Serializable]
        protected class Trial
        {
            public int participantNumber;
            public int groupNumber;
            public DateTime session;
            public float startTime;
            public int[] itemsToCollect;        //convenience copy so we don't have to import the experiment configuration as well
            public List<Purchase> itemsCollected;
            public List<Frame> frames = new List<Frame>();
        }

        [Serializable]
        protected struct PortableVector
        {
            public float x;
            public float y;
            public float z;

            public static PortableVector operator -(PortableVector a, PortableVector b)
            {
                PortableVector c;
                c.x = a.x - b.x;
                c.y = a.y - b.y;
                c.z = a.z - b.z;
                return c;
            }

            public float Length()
            {
                return (float)Math.Sqrt((x * x) + (y * y) + (z * z));
            }
        }

        [Serializable]
        protected struct Frame
        {
            public float time;
            public PortableVector headPosition;
            public PortableVector playspacePosition;
            public int inputCount;

            public PortableVector RealHeadPosition {
                get {
                    return headPosition - playspacePosition;
                }
            }
        }

#pragma warning restore 0649

        /* Manually map the type names in the serialised file to local native types, so we don't have to share a DLL */

        sealed class TypeConverter : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                switch (typeName)
                {
                    case "UCL.COMPGV07.Logging+Trial":
                        return typeof(Trial);

                    case "UCL.COMPGV07.Logging+Frame":
                        return typeof(Frame);

                    case "UCL.COMPGV07.Logging+PortableVector":
                        return typeof(PortableVector);

                    case "System.Collections.Generic.List`1[[UCL.COMPGV07.Purchase, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]":
                        return typeof(List<Purchase>);

                    case "System.Collections.Generic.List`1[[UCL.COMPGV07.Logging+Frame, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]":
                        return typeof(List<Frame>);

                    case "UCL.COMPGV07.Purchase":
                        return typeof(Purchase);

                    default:
                        break;
                };

                return typeof(Nullable);
            }
        }

        public struct Report
        {
            /*
            #1 Completion Time
            * The time from the user first entering the environment(Start() called on data collection script) to the last item registering on the conveyer belt.

            #2 Error Rate
            * The total number of redundant or otherwise incorrect items placed on the conveyer belt

            #3 Inputs/Expended Energy
            * The amount of time the user spends providing input to the system.
            * This includes button presses, joystick actuation, gesturing (estimated as the typical time to complete a gesture), or moving the feet if walking in place.
            * Button presses will measure how long the button is held down for.
            * This will be measured to begin with using simply the unity Input.GetKeyXXXX() methods, but will be extended depending on the design of the interaction techniques.

            #4 Distance Travelled (real)
            * The total distance covered by the head throughout the trial
            * Computed by finding the difference in positions between two frames and accumulating it

            #5 Distance Travelled (virtual)
            * The total distance covered by the virtual viewpoint throughout the trial.
            * Computed by finding the difference in positions between two frames and accumulating it
            */

            public int participantNumber;
            public int groupNumber;
            public bool completed;
            public float completionTime;
            public int errorRate;
            public int inputEvents;
            public float realDistanceTravelled;
            public float virtualDistanceTravelled;
        }

        /* This is the class that does all the processing for the logs. This way we can either run this program, or load the assembly into matlab and use its
        interop functionality to create an instance of this class & call its methods directly. */
        public class Metrics
        {
            private List<Trial> trials = new List<Trial>();
            private List<Report> reports = new List<Report>();

            private const float maxTime = 15 * 60;

            public void Import(string path)
            {
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (var v in di.GetFiles("*.bin", SearchOption.AllDirectories)) //recursive search
                {
                    try
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Binder = new TypeConverter();
                        trials.Add(formatter.Deserialize(new FileStream(v.FullName, FileMode.Open, FileAccess.Read)) as Trial);
                    }
                    catch (Exception)
                    {
                        //ignore any files that cannot be imported. there may be other files with a .bin extension lying around
                    }
                }

                foreach(var trial in trials)
                {
                    Report report;

                    report.participantNumber = trial.participantNumber;
                    report.groupNumber = trial.groupNumber;

                    // Get the codes of all items collected, and subtract them from the items to collect list. If the participant was successful, there should be none remaining.
                    report.completed = trial.itemsToCollect.Except(trial.itemsCollected.Select(x => x.Code)).Count() == 0;

                    // Get all 'checkout events' that include pertinent items, and get the last of them.
                    if (report.completed)
                    {
                        report.completionTime = trial.itemsCollected.Where(x => trial.itemsToCollect.Contains(x.Code)).Select(x => x.Time).Max() - trial.startTime;
                    }
                    else
                    {
                        report.completionTime = maxTime;
                    }

                    // Get all the item codes from the checkout events and remove all the expected items leaving only the erroneous ones.
                    report.errorRate = trial.itemsCollected.Where(x => !trial.itemsToCollect.Contains(x.Code)).Count();

                    // The sum of all the 'input events'
                    report.inputEvents = trial.frames.Sum(x => x.inputCount);

                    // The distance travelled is the sum of all the distances moved between each sample point
                    report.realDistanceTravelled = 0;
                    report.virtualDistanceTravelled = 0; 
                    for (int i = 1; i < trial.frames.Count(); i++)
                    {
                        report.virtualDistanceTravelled += (trial.frames[i].headPosition - trial.frames[i - 1].headPosition).Length();
                        report.realDistanceTravelled += (trial.frames[i].RealHeadPosition - trial.frames[i - 1].RealHeadPosition).Length();
                    }

                    reports.Add(report);
                }
            }

            public void PrintResultsTable()
            {
                Console.WriteLine("Group  Trial   Time      Error     Inputs    RealDistance VirtualDistance");
                foreach(var report in reports)
                {
                    Console.WriteLine("{0,-6} {1,-7} {2,-9} {3,-9} {4,-9} {5,-12} {6,-16}", report.groupNumber, report.participantNumber, report.completionTime, report.errorRate, report.inputEvents, report.realDistanceTravelled, report.virtualDistanceTravelled);
                }
            }
        }

        static void Main(string[] args)
        {
            string path = @"C:\COMPGV07_Experiment\";

            Metrics m = new Metrics();
            m.Import(path);
            m.PrintResultsTable();
            Console.ReadLine();
        }
    }
}
