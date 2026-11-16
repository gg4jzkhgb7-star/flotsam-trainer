using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace FlotsamTrainer
{
    /// <summary>
    /// 插件入口。只负责：加载配置、注册 Harmony 补丁、驱动 UI 与每帧逻辑。
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class TrainerPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "local.flotsam.trainer";
        public const string PluginName = "Flotsam Trainer";
        public const string PluginVersion = "0.6.0";

        internal static TrainerPlugin Instance;

        /// <summary>供其他类写日志（BaseUnityPlugin.Logger 是 protected）。</summary>
        internal static BepInEx.Logging.ManualLogSource Log =>
            Instance != null ? Instance.Logger : null;

        internal static ConfigEntry<KeyboardShortcut> ToggleUiKey;
        internal static ConfigEntry<bool> ShowOnStart;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;

            ToggleUiKey = Config.Bind("界面", "开关面板快捷键", new KeyboardShortcut(KeyCode.F1),
                "按该键显示/隐藏修改器面板");
            ShowOnStart = Config.Bind("界面", "启动时显示面板", false, "进入游戏后是否立即显示面板");

            TrainerUI.Visible = ShowOnStart.Value;

            _harmony = new Harmony(PluginGuid);
            ApplyPatches();

            Logger.LogInfo($"{PluginName} v{PluginVersion} 已加载（Unity {Application.unityVersion}）");
        }

        /// <summary>
        /// 逐个类打补丁。某个补丁目标找不到时只让该功能失效，不影响其他功能，也方便看日志定位。
        /// </summary>
        private void ApplyPatches()
        {
            int ok = 0, fail = 0;

            foreach (var type in GetType().Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyLib.HarmonyPatch), true).Length == 0) continue;

                try
                {
                    _harmony.CreateClassProcessor(type).Patch();
                    ok++;
                    Logger.LogInfo("[补丁成功] " + type.Name);
                }
                catch (System.Exception e)
                {
                    fail++;
                    Logger.LogError("[补丁失败] " + type.Name + " -> " + e.Message);
                }
            }

            Logger.LogInfo($"补丁统计：成功 {ok}，失败 {fail}");
        }

        private void Update()
        {
            if (ToggleUiKey.Value.IsDown())
            {
                TrainerUI.Visible = !TrainerUI.Visible;
            }

            Trainer.Tick();
            TrainerUI.TickHotkeys();
        }

        private void OnGUI()
        {
            if (!TrainerUI.Visible) return;
            TrainerUI.Draw();
        }

        private void OnDestroy()
        {
            try { _harmony?.UnpatchSelf(); } catch { /* 退出时忽略 */ }
        }
    }
}
