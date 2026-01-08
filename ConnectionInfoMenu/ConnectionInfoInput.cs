using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace CobaltCoreArchipelago.ConnectionInfoMenu;

public class ConnectionInfoInput : Route, OnInputPhase, OnMouseDown
{
    internal static Spr TextBoxSpr, TextBoxHoverSpr;

    internal int SlotIdx { get; set; }
    internal State.SaveSlot? SaveSlot { get; set; }
    
    private GameWindow Window;
    private List<string> ConnectionInfo;
    private int selectedTextField = -1;
    private string errorText = "";
    private bool connecting = false;
    private ScreenMode screenMode = ScreenMode.Base;
    private double blinkStartTime;
    private bool seePassword = false;

    private string EditText
    {
        get => ConnectionInfo[selectedTextField];
        set => ConnectionInfo[selectedTextField] = value;
    }

    public ConnectionInfoInput()
    {
        Window = MG.inst.Window;
        Window.TextInput += OnTextInput;
        var saveData = ModEntry.Instance.Archipelago.APSaveData;
        Debug.Assert(saveData != null, nameof(saveData) + " != null");
        ConnectionInfo =
        [
            saveData.Hostname,
            saveData.Port.ToString(),
            saveData.Slot,
            saveData.Password ?? ""
        ];
    }

    private static string Localize(IReadOnlyList<string> key) => ModEntry.Instance.Localizations.Localize(key);
    
    public override void Render(G g)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        Draw.Fill(Colors.black);
        var box = g.Push(null, new Rect(50.0, 86.0), onInputPhase: this, rightHint: null, leftHint: null, upHint: null, downHint: null);
        var titleString = Localize(["connectionMenu", "title"]);
        Draw.Text(titleString, box.rect.x + 1.0, box.rect.y - 30.0,
                  DB.stapler, Colors.textMain, null, null, null, TAlign.Left, lineHeight: null, outline: null);
        // Slot summary
        var summary = Localize(["connectionMenu", "slot"]) + $" {SlotIdx} <c=mainMenuLoopArrows>>></c> ";
        summary += SaveSlot!.state is not null
            ? Localize(["connectionMenu", "existingSave"]) + " <c=mainMenuLoopArrows>>></c> "
                                                           + Localize(["connectionMenu", "roomId"])
                                                           + $" {Archipelago.Instance.APSaveData.RoomId}"
            : Localize(["connectionMenu", "newSave"]);
        Draw.Text(summary, box.rect.x + 1.0, box.rect.y, color: Color.Lerp(Colors.redd, Colors.white, 0.3));

        // Text fields and their names
        var line = 0;
        foreach (var (sectionName, uk) in ((string, UK)[])[
                     (Localize(["connectionMenu", "host"]), ArchipelagoUK.connection_host.ToUK()),
                     (Localize(["connectionMenu", "port"]), ArchipelagoUK.connection_port.ToUK()),
                     (Localize(["connectionMenu", "slotName"]), ArchipelagoUK.connection_slot.ToUK()),
                     (Localize(["connectionMenu", "password"]), ArchipelagoUK.connection_password.ToUK())
                 ])
        {
            var offsetY = 24 * (line + 1);
            // Name of the field
            Draw.Text(sectionName, box.rect.x + 98.0, box.rect.y + offsetY, font: DB.thicket, color: Colors.textBold, align: TAlign.Right);
            // Clickable field with background
            var immButton = SharedArt.ButtonSprite(
                g,
                new Rect(x: 100.0, y: offsetY - 6.0, w: 225.0, h: 21.0),
                new UIKey(uk, line),
                TextBoxSpr, TextBoxHoverSpr,
                boxColor: Colors.buttonBoxNormal,
                onMouseDown: this
            );
            // Get text data from storage
            var textToDraw = ConnectionInfo[line];
            // Password view button
            if (uk == ArchipelagoUK.connection_password.ToUK())
            {
                Spr baseSpr, downSpr;
                if (seePassword)
                {
                    baseSpr = StableSpr.buttons_eyeball_on;
                    downSpr = StableSpr.buttons_eyeball_on_down;
                }
                else
                {
                    baseSpr = StableSpr.buttons_eyeball_off;
                    downSpr = StableSpr.buttons_eyeball_off_down;
                    // Hide text if not viewing
                    textToDraw = new string('*', textToDraw.Length);
                }
                // Draw eyeball button
                SharedArt.ButtonSprite(
                    g,
                    new Rect(336.0, offsetY - 4.0, 18.0, 18.0),
                    ArchipelagoUK.connection_seePassword.ToUK(),
                    baseSpr, downSpr,
                    onMouseDown: this
                );
            }

            // Add blinking cursor if selected
            if (line == selectedTextField)
            {
                if (blinkStartTime == 0.0) blinkStartTime = g.time;
                var offsetTime = g.time - blinkStartTime;
                if (offsetTime - Math.Floor(offsetTime) < 0.5) textToDraw += "<c=boldPink>|</c>";
                Draw.Text("~", immButton.v.x + 226.0, immButton.v.y + 3.0, font: DB.stapler, color: Colors.boldPink);
            }
            // Draw editable text
            Draw.Text(textToDraw, immButton.v.x + 5.0, immButton.v.y + 8.0,
                      color: immButton.isHover ? Colors.textChoiceHoverActive : Colors.textChoice);
            line++;
        }
        
        // Potential error text
        Draw.Text(errorText, 240, 205,
                  color: Color.Lerp(Colors.redd, Colors.white, 0.3),
                  align: TAlign.Center);
        
        // Connect and exit buttons
        var buttonBack = SharedArt.ButtonText(
            g, new Vec(79.0, 140.0), ArchipelagoUK.connection_back.ToUK(),
            Localize(["connectionMenu", "back"]),
            onMouseDown: this
        );

        if (screenMode == ScreenMode.RoomIdConflict)
        {
            SharedArt.ButtonText(
                g, new Vec(240.0, 140.0), ArchipelagoUK.connection_finalizeConnection.ToUK(),
                Localize(["connectionMenu", "connectAnyway"]),
                onMouseDown: this,
                boxColor: Colors.boldPink,
                textColor: Colors.boldPink
            );
        }
        else
        {
            SharedArt.ButtonText(
                g, new Vec(240.0, 140.0), ArchipelagoUK.connection_connect.ToUK(),
                screenMode == ScreenMode.RetryConnection
                    ? Localize(["connectionMenu", "retry"])
                    : Localize(["connectionMenu", "connect"]),
                onMouseDown: this,
                boxColor: Colors.boldPink,
                textColor: Colors.boldPink
            );
        }
        
        g.Pop();
    }

    public void OnInputPhase(G g, Box b)
    {
        if (Input.GetKeyDown(Keys.Escape))
        {
            if (selectedTextField != -1)
                selectedTextField = -1;
            else
                Back(g);
        }
        else if (Input.GetKeyDown(Keys.Enter))
        {
            selectedTextField = -1;
        }
        else if (Input.GetKeyDown(Keys.Tab))
        {
            selectedTextField = (selectedTextField + 1) % 4;
            blinkStartTime = g.time;
        }
    }

    public void OnMouseDown(G g, Box b)
    {
        if (b.key is null)
        {
            selectedTextField = -1;
            return;
        }
        switch (b.key.Value.k)
        {
            case (UK)ArchipelagoUK.connection_host:
                selectedTextField = 0;
                break;
            case (UK)ArchipelagoUK.connection_port:
                selectedTextField = 1;
                break;
            case (UK)ArchipelagoUK.connection_slot:
                selectedTextField = 2;
                break;
            case (UK)ArchipelagoUK.connection_password:
                selectedTextField = 3;
                break;
            case (UK)ArchipelagoUK.connection_connect:
                TryToConnect(g);
                break;
            case (UK)ArchipelagoUK.connection_back:
                Back(g);
                break;
            case (UK)ArchipelagoUK.connection_finalizeConnection:
                FinalizeConnection(g);
                break;
            case (UK)ArchipelagoUK.connection_seePassword:
                seePassword = !seePassword;
                break;
        }

        blinkStartTime = g.time;
    }

    internal void TryToConnect(G g)
    {
        if (connecting) return;
        connecting = true;
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        Archipelago.Instance.APSaveData.Hostname = ConnectionInfo[0];
        if (!ushort.TryParse(ConnectionInfo[1], out var port))
        {
            errorText = Localize(["connectionMenu", "invalidPort"]);
            connecting = false;
            return;
        }
        Archipelago.Instance.APSaveData.Port = port;
        Archipelago.Instance.APSaveData.Slot = ConnectionInfo[2];
        Archipelago.Instance.APSaveData.Password = ConnectionInfo[3].Length > 0 ? ConnectionInfo[3] : null;
        var (loginResult, errorCode) = ModEntry.Instance.Archipelago.Reconnect();
        if (errorCode == ArchipelagoErrorCode.ConnectionIssue || !loginResult.Successful)
        {
            errorText = Localize(["connectionMenu", "connectionIssue"]);
        }
        else if (errorCode == ArchipelagoErrorCode.RoomIdConflict)
        {
            errorText = Localize(["connectionMenu", "roomIdConflict"]);
        }
        connecting = false;
        if (errorCode == ArchipelagoErrorCode.Ok)
        {
            FinalizeConnection(g);
        }
    }

    internal void FinalizeConnection(G g)
    {
        if (ModEntry.Instance.Archipelago.Session is null) return;
        ModEntry.Instance.Archipelago.ApplyArchipelagoConnection();
        // Exit screen to menu
        Window.TextInput -= OnTextInput;
        SelectSlotPatch.SelectSlotReplacement(g, SlotIdx);
    }

    internal void Back(G g)
    {
        ModEntry.Instance.Archipelago.Disconnect();
        g.metaRoute!.subRoute = new ProfileSelect();
        Window.TextInput -= OnTextInput;
    }

    internal void OnTextInput(object? sender, TextInputEventArgs args)
    {
        ModEntry.Instance.Logger.LogInformation("Key: {key}, Character: {character}, Code: {code}", args.Key, args.Character,
                                                (int)args.Character);
        if (selectedTextField == -1) return;
        
        // If we have a pending connection, drop it
        if (screenMode == ScreenMode.RoomIdConflict)
        {
            Archipelago.Instance.Disconnect();
            screenMode = ScreenMode.Base;
            errorText = "";
        }
        
        // Validate character input
        var validChar = selectedTextField switch
        {
            1 => char.IsDigit(args.Character),
            _ => char.IsLetterOrDigit(args.Character)
                 || char.IsSymbol(args.Character)
                 || char.IsPunctuation(args.Character)
                 || args.Character == ' '
        };
        if (validChar)
        {
            EditText += args.Character;
            blinkStartTime = 0.0;  // Ask render loop to reset it
        }
        else if (args.Character == ControlChars.Back)
        {
            if (EditText.Length > 0) EditText = EditText.Remove(EditText.Length - 1);
            blinkStartTime = 0.0;  // Ask render loop to reset it
        }
    }
    
    private enum ScreenMode
    {
        Base,
        RetryConnection,
        RoomIdConflict
    } 
}


// Remove editor debug shortcuts while we are on the connection screen
[HarmonyPatch(typeof(EditorShortcuts), nameof(EditorShortcuts.DebugKeys))]
public static class RemoveEditorShortcutsPatch
{
    public static bool Prefix(G g)
    {
        return g.metaRoute?.subRoute is not ConnectionInfoInput;
    }
}
