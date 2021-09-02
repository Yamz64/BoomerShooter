using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Mirror;

public class ChatBehavior : NetworkBehaviour
{
    public bool chat_open;

    private GameObject chat_ui = null;
    private TMP_Text chat_text = null;
    private TMP_InputField input_field = null;

    private static event Action<string> OnMessage;

    public override void OnStartAuthority()
    {
        chat_open = false;
        chat_ui = transform.GetChild(1).GetChild(10).gameObject;
        chat_text = chat_ui.transform.GetChild(0).GetComponent<TMP_Text>();
        input_field = chat_ui.transform.GetChild(1).GetComponent<TMP_InputField>();
        
        OnMessage += HandleNewMessage;
    }

    private void Update()
    {
        if (Input.GetButtonDown("Chat") && !GetComponent<ConsoleController>().GetConsoleOpen() && !chat_open)
        {
            chat_open = true;
            if(input_field != null) input_field.gameObject.SetActive(true);
            GetComponent<PlayerStats>().SetInteractionLock(true);
            EventSystem.current.SetSelectedGameObject(input_field.gameObject, null);
            input_field.OnPointerClick(new PointerEventData(EventSystem.current));
        }
    }

    [ClientCallback]
    private void OnDestroy()
    {
        if(!hasAuthority) { return; }

        OnMessage -= HandleNewMessage;
    }

    private void HandleNewMessage(string message)
    {
        chat_text.text += message;
    }

    [Client]
    public void Send(string message)
    {
        if (!Input.GetKeyDown(KeyCode.Return)) { return; }

        if (!string.IsNullOrWhiteSpace(message)) CmdSendMessage(message);

        input_field.text = string.Empty;

        chat_open = false;
        input_field.gameObject.SetActive(false);
        GetComponent<PlayerStats>().SetInteractionLock(false);
    }

    [Client]
    public void SendMisc(string message)
    {
        CmdMiscMessage(message);
    }

    [Command]
    private void CmdSendMessage(string message)
    {
        //player's data
        PlayerStats stats = connectionToClient.identity.gameObject.GetComponent<PlayerStats>();
        if(stats.GetPlayerName() == null)
            RpcHandleMessage($"<#{ColorUtility.ToHtmlStringRGB(stats.GetPrimaryColor())}>[{connectionToClient.connectionId}]</color>: {message}");
        else if(stats.GetPlayerName() == "")
            RpcHandleMessage($"<#{ColorUtility.ToHtmlStringRGB(stats.GetPrimaryColor())}>[{connectionToClient.connectionId}]</color>: {message}");
        else
            RpcHandleMessage($"<#{ColorUtility.ToHtmlStringRGB(stats.GetPrimaryColor())}>[{stats.GetPlayerName()}]</color>: {message}");
    }

    [Command]
    private void CmdMiscMessage(string message)
    {
        RpcHandleMessage($"{message}");
    }

    [ClientRpc]
    private void RpcHandleMessage(string message)
    {
        OnMessage?.Invoke($"\n{message}");
    }
}
