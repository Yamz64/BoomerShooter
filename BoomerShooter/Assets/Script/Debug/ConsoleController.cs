using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ConsoleController : NetworkBehaviour
{
    bool show_console;
    bool show_help;
    string input;

    //COMMANDS
    public static ConsoleCommand HELP, KILL, NOCLIP;
    public static ConsoleCommand<float> SET_FOV, SET_VIEWMODEL_FOV, SET_MOUSE_SENS;
    public static ConsoleCommand<string> SET_NAME;
    public static ConsoleCommand<float, float, float> SET_COLOR_1, SET_COLOR_2;

    public List<object> command_list;

    public bool GetConsoleOpen() { return show_console; }

    private void Awake()
    {
        //Define all commands
        HELP = new ConsoleCommand("help", "Displays help info", "help", () =>
        {
            show_help = !show_help;
        });

        KILL = new ConsoleCommand("kill", "Kills the player character", "kill", () =>
        {
            if (isLocalPlayer)
            {
                GetComponent<PlayerStats>().SetHealth(0, false);
                GetComponent<PlayerStats>().SetDead(true);
            }
        });

        NOCLIP = new ConsoleCommand("noclip", "You are a ghost!", "noclip", () =>
        {
            if (isLocalPlayer)
            {
                GetComponent<CharacterMovement>().ToggleNoClip();
            }
        });

        SET_FOV = new ConsoleCommand<float>("set_fov", "Sets the player's main fov to the desired value", "set_fov <value>", (fov) =>
        {
            if (isLocalPlayer)
            {
                GetComponent<CharacterMovement>().GetCam().GetComponent<Camera>().fieldOfView = fov;
            }
        });

        SET_VIEWMODEL_FOV = new ConsoleCommand<float>("set_viewmodel_fov", "Sets the player's viewmodel fov to the desired value", "set_viewmodel_fov <value>", (fov) =>
        {
            if (isLocalPlayer)
            {
                GetComponent<CharacterMovement>().GetCam().transform.GetChild(2).GetComponent<Camera>().fieldOfView = fov;
            }
        });

        SET_MOUSE_SENS = new ConsoleCommand<float>("set_mouse_sens", "Sets the player's mouse sensitivity", "set_mouse_sens <value>", (sensitivity) =>
        {
            if (isLocalPlayer)
            {
                GetComponent<CharacterMovement>().SetSensitivity(sensitivity);
            }
        });
        
        SET_NAME = new ConsoleCommand<string>("set_name", "What's your name?", "set_name <name>", (name) =>
        {
            if (isLocalPlayer)
            {
                GetComponent<PlayerStats>().SetPlayerName(name);
            }
        });

        SET_COLOR_1 = new ConsoleCommand<float, float, float>("set_color_1", "Sets the player's primary color", "set_color_1 <R> <G> <B>", (r, g, b) =>
        {
            if (isLocalPlayer)
            {
                GetComponent<PlayerStats>().SetPrimaryColor(r, g, b);
            }
        });
        
        SET_COLOR_2 = new ConsoleCommand<float, float, float>("set_color_2", "Sets the player's secondary color", "set_color_2 <R> <G> <B>", (r, g, b) =>
        {
            if (isLocalPlayer)
            {
                GetComponent<PlayerStats>().SetSecondaryColor(r, g, b);
            }
        });

        //populate the command_list
        command_list = new List<object>
        {
            HELP,
            KILL,
            NOCLIP,
            SET_FOV,
            SET_VIEWMODEL_FOV,
            SET_MOUSE_SENS,
            SET_NAME,
            SET_COLOR_1,
            SET_COLOR_2
        };
    }

    private void Start()
    {
        show_console = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            bool toggle = false;
            if(input != null)
            {
                for(int i = 0; i<input.Length; i++)
                {
                    if (input[i] == '`' && !GetComponent<ChatBehavior>().chat_open) toggle = true;
                }
            }
            //handle opening the console
            if ((Input.GetKeyDown(KeyCode.BackQuote) || toggle) && !GetComponent<ChatBehavior>().chat_open)
            {
                show_console = !show_console;
                GetComponent<PlayerStats>().SetInteractionLock(!GetComponent<PlayerStats>().GetInteractionLock());

                if (show_console)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    input = string.Empty;
                }
            }
        }
    }

    Vector2 scroll;

    private void OnGUI()
    {
        if (isLocalPlayer)
        {
            if (!show_console) { return; }

            float y = 0;

            //showhelp
            if (show_help)
            {
                GUI.Box(new Rect(0, y, Screen.width, 100), "");

                Rect viewport = new Rect(0, 0, Screen.width - 30, 20 * command_list.Count);

                scroll = GUI.BeginScrollView(new Rect(0, y + 5f, Screen.width, 90), scroll, viewport);

                for(int i=0; i<command_list.Count; i++)
                {
                    ConsoleCommandBase command = command_list[i] as ConsoleCommandBase;

                    string label = $"{command.GetCommandFormat()} - {command.GetCommandDesc()}";

                    Rect label_rect = new Rect(5, 20 * i, viewport.width - 100, 20);

                    GUI.Label(label_rect, label);
                }

                GUI.EndScrollView();

                y += 100;
            }

            GUI.Box(new Rect(0, y, Screen.width, 30), "");
            GUI.backgroundColor = new Color(0, 0, 0, 0);
            GUI.SetNextControlName("ConsoleField");
            input = GUI.TextField(new Rect(10f, y + 5f, Screen.width - 20f, 20f), input);

            GUI.FocusControl("ConsoleField");

            //Enter Pressed
            if (Event.current.keyCode == KeyCode.Return)
            {
                HandleInput();
                input = "";
            }
        }
    }

    private void HandleInput()
    {
        string[] properties = input.Split(' ');

        for(int i=0; i<command_list.Count; i++)
        {
            ConsoleCommandBase command_base = command_list[i] as ConsoleCommandBase;

            if (input.Contains(command_base.GetCommandID()))
            {
                if (command_list[i] as ConsoleCommand != null)
                {
                    (command_list[i] as ConsoleCommand).Invoke();
                }
                else if (command_list[i] as ConsoleCommand<float> != null)
                {
                    (command_list[i] as ConsoleCommand<float>).Invoke(float.Parse(properties[1]));
                //THIS MUST ALWAYS BE LAST IN THE LIST OF SINGLE TYPES!!!
                }else if(command_list[i] as ConsoleCommand<string> != null)
                {
                    (command_list[i] as ConsoleCommand<string>).Invoke(properties[1]);
                }else if(command_list[i] as ConsoleCommand<float, float, float> != null)
                {
                    (command_list[i] as ConsoleCommand<float, float, float>).Invoke(float.Parse(properties[1]), float.Parse(properties[2]), float.Parse(properties[3]));
                }
            }
        }
    }
}
