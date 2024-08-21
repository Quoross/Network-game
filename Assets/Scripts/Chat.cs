using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Chat : NetworkBehaviour
{
    [SerializeField] InputReader inputReader;


    [SerializeField] TextMeshProUGUI text;

    private void Start()
    {
        if (inputReader != null)
        {
            inputReader.SendEvent += OnSend;
        }

    }
    private void OnSend()
    {
        FixedString128Bytes message = new("Hello");
        SubmittMessageRPC(message);
    }
    

    [Rpc(SendTo.Server)]
    public void SubmittMessageRPC(FixedString128Bytes message)
    {
        UpdateMessageRPC(message);
        Debug.Log("Message Sent");
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateMessageRPC(FixedString128Bytes message)
    {
        text.text = message.ToString() + " 2";
        Debug.Log("Message Recived");
    }
}
