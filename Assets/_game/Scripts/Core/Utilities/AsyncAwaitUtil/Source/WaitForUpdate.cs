using UnityEngine;

// This can be used as a way to return to the main unity thread when using multiple threads
// with async methods
namespace Core.Utilities.AsyncAwaitUtil.Source
{
    public class WaitForUpdate : CustomYieldInstruction
    {
        public override bool keepWaiting
        {
            get { return false; }
        }
    }
}
