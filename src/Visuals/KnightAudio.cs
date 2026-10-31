using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.TestSupport;

namespace FullMoonKnight.Visuals;

/// <summary>
/// 女骑士音效：原作音效打包为 res://FullMoonKnight/audio/*.ogg，用 Godot 播放器在 SFX 总线上播放。
/// 游戏的音效接口只认 FMOD 事件路径，这里拦截 <see cref="NAudioManager.PlayOneShot(string, Dictionary{string, float}, float)" />：
/// 路径在模组音效目录下时改由本类播放，所以音效路径可以直接用在角色音效、命中音效等原生接口里。
/// </summary>
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot),
    [typeof(string), typeof(Dictionary<string, float>), typeof(float)])]
public static class KnightAudio
{
    // ===== 音效路径 =====

    /// <summary>模组音效目录。</summary>
    private const string Root = KnightMod.ResRoot + "audio/";

    /// <summary>角色攻击。</summary>
    public const string Attack = Root + "attack.ogg";

    /// <summary>角色施法（技能、能力）。</summary>
    public const string Cast = Root + "cast.ogg";

    /// <summary>角色死亡。</summary>
    public const string Death = Root + "death.ogg";

    /// <summary>布置反制。</summary>
    public const string CounterSet = Root + "counter_set.ogg";

    /// <summary>反制触发。</summary>
    public const string CounterTrigger = Root + "counter_trigger.ogg";

    /// <summary>装备。</summary>
    public const string Equip = Root + "equip.ogg";

    /// <summary>火焰命中。</summary>
    public const string FireHit = Root + "fire_hit.ogg";

    /// <summary>雷电命中。</summary>
    public const string ThunderHit = Root + "thunder_hit.ogg";

    // ===== 播放 =====

    /// <summary>音效总线名（游戏设置的音效音量作用在这条总线上）。</summary>
    private static readonly StringName SfxBus = "SFX";

    /// <summary>已加载的音频流。</summary>
    private static readonly Dictionary<string, AudioStream> Streams = [];

    /// <summary>
    /// 拦截模组音效路径。
    /// </summary>
    /// <param name="path">音效路径。</param>
    /// <param name="volume">音量（线性）。</param>
    /// <returns>是否继续执行原方法（FMOD）。</returns>
    public static bool Prefix(string path, float volume)
    {
        if (!path.StartsWith(Root, StringComparison.Ordinal))
        {
            return true;
        }
        if (!TestMode.IsOn)
        {
            Play(path, volume);
        }
        return false;
    }

    /// <summary>
    /// 播放一次音效，播完自动释放播放器。
    /// </summary>
    /// <param name="path">音效路径。</param>
    /// <param name="volume">音量（线性）。</param>
    private static void Play(string path, float volume)
    {
        // 挂载播放器的节点
        Node? parent = NGame.Instance;
        if (parent == null)
        {
            return;
        }
        if (!Streams.TryGetValue(path, out AudioStream? stream))
        {
            stream = AudioStreamOggVorbis.LoadFromBuffer(Godot.FileAccess.GetFileAsBytes(path));
            Streams[path] = stream;
        }
        // 一次性播放器
        AudioStreamPlayer player = new() { Stream = stream, VolumeLinear = volume, Bus = SfxBus };
        player.Finished += player.QueueFree;
        parent.AddChild(player);
        player.Play();
    }
}
