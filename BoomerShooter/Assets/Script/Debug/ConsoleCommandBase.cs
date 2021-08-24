using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleCommandBase
{
    private string _command_id;
    private string _command_description;
    private string _command_format;

    public string GetCommandID() { return _command_id; }
    public string GetCommandDesc() { return _command_description; }
    public string GetCommandFormat() { return _command_format; }

    public ConsoleCommandBase(string id, string description, string format)
    {
        _command_id = id;
        _command_description = description;
        _command_format = format;
    }
}

public class ConsoleCommand : ConsoleCommandBase
{
    private Action command;

    public ConsoleCommand(string id, string description, string format, Action command) : base(id, description, format)
    {
        this.command = command;
    }

    public void Invoke()
    {
        command.Invoke();
    }
}

public class ConsoleCommand<T> : ConsoleCommandBase
{
    private Action<T> command;

    public ConsoleCommand(string id, string description, string format, Action<T> command) : base(id, description, format)
    {
        this.command = command;
    }

    public void Invoke(T value)
    {
        command.Invoke(value);
    }
}
