using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using Speckle.Core.Logging;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    public class StreamWrapperTest : MonoBehaviour
    {
        [ContextMenu("Test")]
        void Test()
        {
            try
            {
                var wrapper = new StreamWrapper();
                Task task = new Task(async () =>
                {
                    await wrapper.Initialize();
                    wrapper.Receive();
                });
                task.Start();
                task.Wait();
                wrapper.Bake();
            }
            catch (Exception e)
            {
                throw new SpeckleException(e.Message, e, true, SentryLevel.Error);
            }
        }
    }
}

