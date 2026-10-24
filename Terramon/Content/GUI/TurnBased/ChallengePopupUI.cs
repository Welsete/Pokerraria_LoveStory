using Terramon.Core.Battling;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI.TurnBased;

/// <summary>
///     The "{0} wants to battle!" popup shown to a player another player challenged.
///     Declines by itself after <see cref="TimeoutSeconds" /> seconds or if the challenge stops being valid.
/// </summary>
public sealed class ChallengePopupUI : SmartUIState
{
    public const int TimeoutSeconds = 20;

    private static ChallengeQuestion _pending;
    private static ulong _deadline;

    private UIPanel _window;
    private UIText _text;
    private UIText _countdown;

    public static bool HasPending => _pending != null;

    public override bool Visible => _pending != null && !Main.gameMenu;

    private static string Text(string key, params object[] args)
        => Language.GetTextValue("Mods.Terramon.Multiplayer.Challenge." + key, args);

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
        => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

    /// <summary>
    ///     Shows the popup for a challenge sent to the local player.
    /// </summary>
    public static void Open(ChallengeQuestion question)
    {
        _pending = question;
        _deadline = Main.GameUpdateCount + TimeoutSeconds * 60UL;
        SoundEngine.PlaySound(SoundID.MenuOpen);
    }

    /// <summary>
    ///     Answers the pending challenge, if any. Accepting checks again that the local player can still battle.
    /// </summary>
    public static void Answer(bool accept)
    {
        var question = _pending;
        _pending = null;
        if (question == null)
            return;

        var me = TerramonPlayer.LocalPlayer;
        if (accept && me.ChallengeRefusalReason(question.Sender) is { } reason)
        {
            Main.NewText(Text("AutoDeclined", question.Sender.BattleName, reason), Color.Orange);
            accept = false;
        }

        question.Return(new ChallengeAnswer(yes: accept));
    }

    /// <summary>
    ///     Closes the popup without answering, e.g. when the challenger took the challenge back or left.
    /// </summary>
    public static void Close()
    {
        _pending = null;
    }

    /// <summary>
    ///     Runs every tick for the local player: declines on timeout and drops challenges that are no longer valid.
    /// </summary>
    public static void Tick()
    {
        var question = _pending;
        if (question == null)
            return;

        // The challenger left, took it back, or their challenge was ended by the server
        var challenger = question.Sender;
        var me = TerramonPlayer.LocalPlayer;
        if (challenger?.SyncedEntity is not { active: true } || challenger.BattleClient is not { } client ||
            client.State != ClientBattleState.Requested || client.Foe != (IBattleProvider)me)
        {
            Close();
            return;
        }

        if (Main.GameUpdateCount < _deadline)
            return;

        Main.NewText(Text("TimedOut", challenger.BattleName), Color.Orange);
        Answer(false);
    }

    public override void OnInitialize()
    {
        _window = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.95f,
            HAlign = 0.5f
        };
        _window.Width.Pixels = 360f;
        _window.Height.Pixels = 112f;
        _window.Top.Pixels = 110f;
        Append(_window);

        _text = new UIText(string.Empty)
        {
            HAlign = 0.5f
        };
        _window.Append(_text);

        _countdown = new UIText(string.Empty, 0.8f)
        {
            HAlign = 0.5f,
            TextColor = Color.LightGray
        };
        _countdown.Top.Pixels = 26f;
        _window.Append(_countdown);

        _window.Append(MakeButton(Text("Accept"), 0f, new Color(52, 120, 70), () => Answer(true)));
        _window.Append(MakeButton(Text("Decline"), 1f, new Color(130, 50, 50), () => Answer(false)));
    }

    private static UITextPanel<string> MakeButton(string label, float hAlign, Color color, Action onClick)
    {
        var button = new UITextPanel<string>(label, 0.9f)
        {
            HAlign = hAlign,
            VAlign = 1f,
            BackgroundColor = color * 0.8f
        };
        button.Width.Pixels = 150f;
        button.Height.Pixels = 34f;
        button.OnMouseOver += (_, el) => ((UIPanel)el).BackgroundColor = color;
        button.OnMouseOut += (_, el) => ((UIPanel)el).BackgroundColor = color * 0.8f;
        button.OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            onClick();
        };
        return button;
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        var question = _pending;
        if (question == null)
            return;

        _text.SetText(Language.GetTextValue("Mods.Terramon.Battle.ChallengeReceived", question.Sender?.BattleName));
        var secondsLeft = (int)Math.Ceiling(Math.Max(0L, (long)_deadline - Main.GameUpdateCount) / 60f);
        _countdown.SetText(Text("Countdown", secondsLeft));

        if (_window.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
    }
}
