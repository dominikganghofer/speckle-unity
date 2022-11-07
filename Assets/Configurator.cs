using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using UnityEngine;
using Stream = Speckle.Core.Api.Stream;
using Streams = Speckle.ConnectorUnity.Streams;

public class Configurator : MonoBehaviour
{
    [SerializeField] private string Server;
    [SerializeField] private List<Stream> StreamList = null;
    [SerializeField] private int ISelectedStream;
    [SerializeField] [Multiline] private string DetailsStreamText;

    [SerializeField]
    private Stream SelectedStream
    {
        get
        {
            if (ISelectedStream >= StreamList.Count)
                ISelectedStream = 0;
            return StreamList[ISelectedStream];
        }
    }

    private async void Start()
    {
        var defaultAccount = AccountManager.GetDefaultAccount();
        if (defaultAccount == null)
        {
            Debug.Log("Please set a default account in SpeckleManager");
            return;
        }

        Server = defaultAccount.serverInfo.name;
        StreamList = await Streams.List(30);
        if (!StreamList.Any())
        {
            Debug.Log("There are no streams in your account, please create one online.");
            return;
        }
    }

    [ContextMenu("Select Receiver")]
    private void SelectReceiver()
    {
        DetailsStreamText =
            $"Description: {SelectedStream.description}\n" +
            $"Link sharing on: {SelectedStream.isPublic}\n" +
            $"Role: {SelectedStream.role}\n" +
            $"Collaborators: {SelectedStream.collaborators.Count}\n" +
            $"Id: {SelectedStream.id}";
    }
    //
    // [ContextMenu("AddReceiver")]
    // private async void AddReceiver()
    // {
    //     rt.anchoredPosition = new Vector3(-10, -110 - StreamPanels.Count * 110, 0);
    //
    //     streamPrefab.AddComponent<InteractionLogic>().InitReceiver(stream, autoReceive);
    //
    //     StreamPanels.Add(streamPrefab);
    // }
}