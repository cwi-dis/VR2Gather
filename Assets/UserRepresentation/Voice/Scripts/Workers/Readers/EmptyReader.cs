using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class EmptyReader : BaseWorker
    {
        Coroutine coroutine;

        public EmptyReader() : base()
        {
            Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void OnStop()
        {
            base.OnStop();
            Debug.Log("EmptyReader Sopped");
        }

    }
}