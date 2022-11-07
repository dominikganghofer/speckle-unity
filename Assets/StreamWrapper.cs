using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sentry;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using UnityEditor;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    public class StreamWrapper
    {
        private Account _selectedAccount;
        private Stream _selectedStream;
        private List<Branch> _branches;
        private int _selectedBranchIndex;
        private int _selectedCommitIndex;
        private int _totalChildrenCount;
        RecursiveConverter _converter;
        private Client _client;
        private Base _receivedBase;

        public async Task Initialize(int streamsLimit = 30, int branchesLimit = 30, int commitsLimit = 25)
        {
            _selectedAccount = AccountManager.GetDefaultAccount();
            _client = new Client(_selectedAccount);
            // EditorUtility.DisplayProgressBar("Loading streams...", "", 0);
            var streams = await _client.StreamsGet(streamsLimit);
            // EditorUtility.ClearProgressBar();
            if (!streams.Any())
                return;
            _selectedStream = streams.First();

            // EditorUtility.DisplayProgressBar("Loading stream details...", "", 0);
            _branches = await _client.StreamGetBranches(_selectedStream.id, branchesLimit, commitsLimit);
            if (_branches.Any())
            {
                _selectedBranchIndex = 0;
                if (_branches[_selectedBranchIndex].commits.items.Any())
                {
                    _selectedCommitIndex = 0;
                }
            }

            // EditorUtility.ClearProgressBar();
        }

        public async void Receive()
        {
            var transport = new ServerTransport(_selectedAccount, _selectedStream.id);
            // EditorUtility.DisplayProgressBar($"Receiving data from {transport.BaseUri}...", "", 0);

            try
            {
                // Receive Speckle Objects
                @_receivedBase = await Operations.Receive(
                    _branches[_selectedBranchIndex].commits.items[_selectedCommitIndex].referencedObject,
                    remoteTransport: transport,
                    onProgressAction: dict =>
                    {
                        // UnityEditor.EditorApplication.delayCall += () =>
                        // {
                        //     EditorUtility.DisplayProgressBar($"Receiving data from {transport.BaseUri}...", "",
                        //         Convert.ToSingle(dict.Values.Average() / _totalChildrenCount));
                        // };
                    },
                    onTotalChildrenCountKnown: count => { _totalChildrenCount = count; }
                );

                // EditorUtility.ClearProgressBar();

                Analytics.TrackEvent(_selectedAccount, Analytics.Events.Receive);

                //Convert Speckle Objects
                int childrenConverted = 0;

                void BeforeConvertCallback(Base b)
                {
                    // EditorUtility.DisplayProgressBar("Converting To Native...", $"{b.speckle_type} - {b.id}",
                    //     Convert.ToSingle(childrenConverted++ / _totalChildrenCount));
                }

                // Read Receipt
                await this._client.CommitReceived(new CommitReceivedInput
                {
                    streamId = _selectedStream.id,
                    commitId = _branches[_selectedBranchIndex].commits.items[_selectedCommitIndex].id,
                    message = $"received commit from {HostApplications.Unity.Name} Editor",
                    sourceApplication = HostApplications.Unity.Name
                });
            }
            catch (Exception e)
            {
                throw new SpeckleException(e.Message, e, true, SentryLevel.Error);
            }
            finally
            {
                // EditorApplication.delayCall += EditorUtility.ClearProgressBar;
            }
        }

        public GameObject Bake()
        {
            return ConvertRecursivelyToNative(
                _receivedBase,
                _branches[_selectedBranchIndex].commits.items[_selectedCommitIndex].id);
        }

        private GameObject ConvertRecursivelyToNative(Base @base, string rootObjectName,
            Action<Base> beforeConvertCallback = null)
        {
            var rootObject = new GameObject(rootObjectName);

            bool Predicate(Base o)
            {
                beforeConvertCallback?.Invoke(o);
                return _converter.ConverterInstance.CanConvertToNative(o) //Accept geometry
                       || o.speckle_type == nameof(Base) &&
                       o.totalChildrenCount > 0; // Or Base objects that have children  
            }


            // For the rootObject only, we will create property GameObjects
            // i.e. revit categories
            foreach (var prop in @base.GetMembers())
            {
                var converted = _converter.RecursivelyConvertToNative(prop.Value, null, Predicate);

                //Skip empties
                if (converted.Count <= 0) continue;

                var propertyObject = new GameObject(prop.Key);
                propertyObject.transform.SetParent(rootObject.transform);
                foreach (var o in converted)
                {
                    if (o.transform.parent == null) o.transform.SetParent(propertyObject.transform);
                }
            }

            return rootObject;
        }
    }
}