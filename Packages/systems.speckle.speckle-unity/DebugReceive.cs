using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    public class DebugReceive
    {
         public static Task<Base> Receive(
      string objectId,
      ITransport remoteTransport = null,
      ITransport localTransport = null,
      Action<ConcurrentDictionary<string, int>> onProgressAction = null,
      Action<string, Exception> onErrorAction = null,
      Action<int> onTotalChildrenCountKnown = null,
      bool disposeTransports = false,
      SerializerVersion serializerVersion = SerializerVersion.V2)
    {
      return Operations.Receive(objectId, CancellationToken.None, remoteTransport, localTransport, onProgressAction, onErrorAction, onTotalChildrenCountKnown, disposeTransports, serializerVersion);
    }

    /// <summary>Receives an object from a transport.</summary>
    /// <param name="objectId"></param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to send notice of cancellation.</param>
    /// <param name="remoteTransport">The transport to receive from.</param>
    /// <param name="localTransport">Leave null to use the default cache.</param>
    /// <param name="onProgressAction">Action invoked on progress iterations.</param>
    /// <param name="onErrorAction">Action invoked on internal errors.</param>
    /// <param name="onTotalChildrenCountKnown">Action invoked once the total count of objects is known.</param>
    /// <returns></returns>
    public static async Task<Base> Receive(
      string objectId,
      CancellationToken cancellationToken,
      ITransport remoteTransport = null,
      ITransport localTransport = null,
      Action<ConcurrentDictionary<string, int>> onProgressAction = null,
      Action<string, Exception> onErrorAction = null,
      Action<int> onTotalChildrenCountKnown = null,
      bool disposeTransports = false,
      SerializerVersion serializerVersion = SerializerVersion.V2)
    {
      Log.AddBreadcrumb(nameof (Receive));
      BaseObjectSerializer objectSerializer = (BaseObjectSerializer) null;
      JsonSerializerSettings settings = (JsonSerializerSettings) null;
      BaseObjectDeserializerV2 serializerV2 = (BaseObjectDeserializerV2) null;
      if (serializerVersion == SerializerVersion.V1)
        (objectSerializer, settings) = Operations.GetSerializerInstance();
      else
        serializerV2 = new BaseObjectDeserializerV2();
      
      Dispatcher.Instance().Enqueue(() => { Debug.Log($"a "); });

      
      Action<string, int> internalProgressAction = GetInternalProgressAction(new ConcurrentDictionary<string, int>(), onProgressAction);
      bool hasUserProvidedLocalTransport = localTransport != null;
      localTransport = localTransport != null ? localTransport : (ITransport) new SQLiteTransport();
      localTransport.OnErrorAction = onErrorAction;
      localTransport.OnProgressAction = internalProgressAction;
      localTransport.CancellationToken = cancellationToken;
      Dispatcher.Instance().Enqueue(() => { Debug.Log($"b "); });

      if (serializerVersion == SerializerVersion.V1)
      {
        objectSerializer.ReadTransport = localTransport;
        objectSerializer.OnProgressAction = internalProgressAction;
        objectSerializer.OnErrorAction = onErrorAction;
        objectSerializer.CancellationToken = cancellationToken;
      }
      else
      {
        serializerV2.ReadTransport = localTransport;
        serializerV2.OnProgressAction = internalProgressAction;
        serializerV2.OnErrorAction = onErrorAction;
        serializerV2.CancellationToken = cancellationToken;
      }
      Dispatcher.Instance().Enqueue(() => { Debug.Log($"c "); });

      string objString = localTransport.GetObject(objectId);
      if (objString != null)
      {
        Placeholder placeholder = JsonConvert.DeserializeObject<Placeholder>(objString);
        if (placeholder.__closure != null)
        {
          Action<int> action = onTotalChildrenCountKnown;
          if (action != null)
            action(placeholder.__closure.Count);
        }
        Dispatcher.Instance().Enqueue(() => { Debug.Log($"d "); });

        Base @base;
        if (serializerVersion == SerializerVersion.V1)
        {
          @base = JsonConvert.DeserializeObject<Base>(objString, settings);
        }
        else
        {
          try
          {
            Dispatcher.Instance().Enqueue(() => { Debug.Log($"e "); });

            @base = serializerV2.Deserialize(objString);
          }
          catch (Exception ex)
          {
            if (serializerV2.OnErrorAction == null)
            {
              throw;
            }
            else
            {
              serializerV2.OnErrorAction("A deserialization error has occurred: " + ex.Message, (Exception) new SpeckleException("A deserialization error has occurred: " + ex.Message, ex));
              @base = (Base) null;
            }
          }
        }
        if ((disposeTransports || !hasUserProvidedLocalTransport) && localTransport is IDisposable disposable1)
          disposable1.Dispose();
        if (disposeTransports && remoteTransport != null && remoteTransport is IDisposable disposable2)
          disposable2.Dispose();
        return @base;
      }
      Dispatcher.Instance().Enqueue(() => { Debug.Log($"f "); });

      if (remoteTransport == null)
        throw new SpeckleException("Could not find specified object using the local transport, and you didn't provide a fallback remote from which to pull it.", level: SentryLevel.Error);
      remoteTransport.OnErrorAction = onErrorAction;
      remoteTransport.OnProgressAction = internalProgressAction;
      remoteTransport.CancellationToken = cancellationToken;
      Log.AddBreadcrumb("RemoteHit");
      objString = await remoteTransport.CopyObjectAndChildren(objectId, localTransport, onTotalChildrenCountKnown);
      await localTransport.WriteComplete();
      Base base1;
      Dispatcher.Instance().Enqueue(() => { Debug.Log($"g "); });

      if (serializerVersion == SerializerVersion.V1)
      {
        base1 = JsonConvert.DeserializeObject<Base>(objString, settings);
      }
      else
      {
        try
        {
          base1 = serializerV2.Deserialize(objString);
        }
        catch (Exception ex)
        {
          if (serializerV2.OnErrorAction == null)
          {
            throw;
          }
          else
          {
            serializerV2.OnErrorAction("A deserialization error has occurred: " + ex.Message, ex);
            base1 = (Base) null;
          }
        }
      }
      Dispatcher.Instance().Enqueue(() => { Debug.Log($"h "); });

      if ((disposeTransports || !hasUserProvidedLocalTransport) && localTransport is IDisposable disposable3)
        disposable3.Dispose();
      if (disposeTransports && remoteTransport is IDisposable disposable4)
        disposable4.Dispose();
      return base1;
    }
    internal class Placeholder
    {
      public Dictionary<string, int> __closure { get; set; } = new Dictionary<string, int>();
    }
    
    private static Action<string, int> GetInternalProgressAction(
      ConcurrentDictionary<string, int> localProgressDict,
      Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      return (Action<string, int>) ((name, processed) =>
      {
        if (localProgressDict.ContainsKey(name))
          localProgressDict[name] += processed;
        else
          localProgressDict[name] = processed;
        Action<ConcurrentDictionary<string, int>> action = onProgressAction;
        if (action == null)
          return;
        action(localProgressDict);
      });
    }
    }
}