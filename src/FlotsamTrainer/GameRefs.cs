using System;
using System.Reflection;

namespace FlotsamTrainer
{
    /// <summary>
    /// 游戏内部对象的访问入口。走反射，游戏更新导致签名变化时只影响这里，不会让整个插件崩掉。
    /// </summary>
    internal static class GameRefs
    {
        private static PropertyInfo _playerCommunityProp;
        private static bool _resolved;

        internal static Community PlayerCommunity
        {
            get
            {
                try
                {
                    if (!_resolved)
                    {
                        _resolved = true;
                        _playerCommunityProp = typeof(Community).GetProperty("PlayerCommunity",
                            BindingFlags.Public | BindingFlags.Static);
                    }

                    if (_playerCommunityProp != null)
                    {
                        return _playerCommunityProp.GetValue(null, null) as Community;
                    }

                    // 兜底：静态列表里取第一个
                    var list = typeof(Community).GetField("Communities", BindingFlags.Public | BindingFlags.Static);
                    if (list != null)
                    {
                        var value = list.GetValue(null) as System.Collections.IList;
                        if (value != null && value.Count > 0)
                        {
                            return value[0] as Community;
                        }
                    }
                }
                catch (Exception e)
                {
                    TrainerPlugin.Log?.LogWarning("获取 PlayerCommunity 失败: " + e.Message);
                }

                return null;
            }
        }

        private static bool _baseScaleResolved;
        private static MethodInfo _getSpeedSettings;
        private static FieldInfo _gameplayTimeScale;

        /// <summary>
        /// 兜底：直接从 GameplaySettings 读取当前速度档位对应的原始 timeScale。
        /// 万一 SetGameSpeedAndTimeScale 的补丁被 JIT 内联掉，速度功能仍然可用。
        /// </summary>
        internal static bool TryGetBaseTimeScale(out float baseScale)
        {
            baseScale = 0f;
            try
            {
                if (!_baseScaleResolved)
                {
                    _baseScaleResolved = true;
                    var settingsType = typeof(GameSpeedManager).Assembly.GetType("GameplaySettings");
                    if (settingsType != null)
                    {
                        _getSpeedSettings = settingsType.GetMethod("GetGameSpeedSettings",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                        if (_getSpeedSettings != null)
                        {
                            var returnType = _getSpeedSettings.ReturnType;
                            _gameplayTimeScale = returnType.GetField("GameplayTimeScale",
                                BindingFlags.Public | BindingFlags.Instance);
                        }
                    }
                }

                if (_getSpeedSettings == null || _gameplayTimeScale == null) return false;

                object settings = _getSpeedSettings.IsStatic
                    ? _getSpeedSettings.Invoke(null, null)
                    : null;
                if (settings == null) return false;

                object value = _gameplayTimeScale.GetValue(settings);
                if (value is float f && f > 0f)
                {
                    baseScale = f;
                    return true;
                }
            }
            catch { /* 忽略 */ }

            return false;
        }
    }
}
