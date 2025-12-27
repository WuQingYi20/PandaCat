#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace BearCar.Editor
{
    /// <summary>
    /// 双人合作游戏设计原则 - 用于指导关卡设计
    /// 基于 Bear Car 的核心机制深度定制
    /// </summary>
    public static class CoopDesignPrinciples
    {
        /// <summary>
        /// 显示设计原则窗口
        /// </summary>
        [MenuItem("BearCar/关卡设计/设计原则指南")]
        public static void ShowPrinciplesWindow()
        {
            CoopPrinciplesWindow.ShowWindow();
        }
    }

    /// <summary>
    /// 设计原则指南窗口
    /// </summary>
    public class CoopPrinciplesWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private int selectedPrinciple = 0;

        public static void ShowWindow()
        {
            var window = GetWindow<CoopPrinciplesWindow>("合作设计原则");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("🎮 Bear Car 双人合作设计原则", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 核心理念
            DrawSection("🎯 核心理念", @"
Bear Car 的合作核心是「互利共生」：
• 绿熊吃红色道具更好，红熊吃绿色道具更好
• 共享背包意味着谁用道具是一个决策点
• 推车需要两人配合，但位置有左右之分
• 成功不是零和游戏，是 1+1>2 的体验");

            EditorGUILayout.Space(10);

            // 五大设计支柱
            DrawSection("🏛️ 五大设计支柱", "");

            DrawPillar("1. 正向依存 (Positive Interdependence)", @"
玩家必须互相需要才能成功。

✓ 好的设计:
  • 合作传送门需要两人同时踩压力板
  • 颜色道具系统让交换变得有价值
  • 一人拿铲子开路，另一人收集奖励

✗ 避免:
  • 两人可以完全独立完成的任务
  • 一人能做所有事，另一人只是旁观");

            DrawPillar("2. 不对称收益 (Asymmetric Benefits)", @"
同一个道具对不同玩家价值不同。

✓ 好的设计:
  • 绿苹果: 绿熊+5, 红熊+10
  • 放置道具位置选择: 离谁更近?
  • 体力低的人应该优先吃东西

✗ 避免:
  • 所有道具对所有人完全相同
  • 没有决策点的资源分配");

            DrawPillar("3. 空间协调 (Spatial Coordination)", @"
利用物理空间创造合作需求。

✓ 好的设计:
  • 车的左右两侧各有推力
  • 传送门分隔两人，需要重新汇合
  • 岔路口需要分头行动

✗ 避免:
  • 两人始终重叠在一起
  • 空间没有任何意义");

            DrawPillar("4. 沟通触发 (Communication Triggers)", @"
设计强迫玩家开口说话的时刻。

✓ 好的设计:
  • '你踩那个压力板，我去拿道具'
  • '这个红苹果给你吃更好'
  • '快用道具！我顶不住了！'

✗ 避免:
  • 不需要任何交流就能完成
  • 沟通没有实际价值");

            DrawPillar("5. 角色流动 (Role Fluidity)", @"
领导者和跟随者的角色应该自然切换。

✓ 好的设计:
  • 有时绿熊在前探路，有时红熊领先
  • 谁的体力多谁就多推车
  • 危险区域让有工具的人先行

✗ 避免:
  • 一人永远是主角，另一人是配角
  • 固化的角色分工");

            EditorGUILayout.Space(20);

            // Aha Moment 类型
            DrawSection("💡 Aha Moment 类型", "");

            DrawAhaType("发现型 (Discovery)", @"
玩家发现游戏中隐藏的机制。

例子:
• '原来吃对方颜色的水果更好！'
• '传送门居然能加速！'
• '曼妥思+可乐会触发特殊效果！'

设计要点:
• 不要直接告诉玩家，让他们自己发现
• 第一次发现要有明显的正反馈
• 发现后能改变玩法策略");

            DrawAhaType("同步型 (Synchronization)", @"
需要精确配合才能成功。

例子:
• '两人同时踩下压力板！'
• '你先进传送门，我跟上！'
• '3, 2, 1, 一起推！'

设计要点:
• 有明确的同步信号
• 失败后能快速重试
• 成功时的满足感要强");

            DrawAhaType("牺牲型 (Sacrifice)", @"
一人放弃利益让团队获益。

例子:
• '你吃这个，我体力还够'
• '我留在这里踩压力板，你去拿奖励'
• '我用道具帮你开路'

设计要点:
• 牺牲要有意义，不是无谓的
• 被帮助的人应该有感恩的机会
• 牺牲后团队总收益要明显更高");

            DrawAhaType("救援型 (Rescue)", @"
一人陷入困境，另一人来救。

例子:
• '我没体力了，快给我道具！'
• '用灵魂网复活我！'
• '帮我清掉前面的障碍！'

设计要点:
• 危机要有紧迫感但不是瞬间死亡
• 救援行为要有实际意义
• 被救者要能继续贡献");

            DrawAhaType("优化型 (Optimization)", @"
发现更高效的合作方式。

例子:
• '你去那边拿绿的，我拿红的，然后交换！'
• '让体力多的人多推一会儿'
• '用加速传送门冲上去更快'

设计要点:
• 有多种可行方案
• 最优解需要思考和配合
• 优化后的收益明显");

            EditorGUILayout.Space(20);

            // 布局准则
            DrawSection("📐 布局准则", @"
关卡中道具和机关的放置原则:

1. 【颜色平衡】绿色和红色道具数量大致相等
   → 除非刻意制造资源竞争

2. 【保底机制】每个区域至少有一个中性道具
   → 香蕉是最好的保底选择

3. 【问题-答案】每个障碍附近要有解决方案
   → 高级钉子旁边要有铲子可拿

4. 【风险-回报】高价值道具放在有风险的位置
   → 稀有道具藏在需要配合才能到达的地方

5. 【节奏控制】紧张和放松交替出现
   → 难点后面跟一个容易的恢复区域

6. 【视觉引导】让玩家自然地看到下一个目标
   → 道具略微突出背景，形成视觉路径");

            EditorGUILayout.Space(20);

            // 禁忌
            DrawSection("⛔ 设计禁忌", @"
1. ❌ 不要让一个玩家无事可做
   → 每个挑战都应该需要两人参与

2. ❌ 不要制造玩家之间的零和竞争
   → 抢道具不应该伤害队友

3. ❌ 不要惩罚沟通失败太严重
   → 失败应该能快速重试

4. ❌ 不要隐藏关键信息让玩家猜
   → 机制可以隐藏，但操作方式要清晰

5. ❌ 不要让最优策略是分开行动
   → 合作应该比单干更有效率

6. ❌ 不要让技术差距决定一切
   → 配合和沟通应该比手速更重要");

            EditorGUILayout.EndScrollView();
        }

        private void DrawSection(string title, string content)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(content))
            {
                EditorGUILayout.HelpBox(content.Trim(), MessageType.None);
            }
        }

        private void DrawPillar(string title, string content)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(content.Trim(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawAhaType(string title, string content)
        {
            EditorGUILayout.BeginVertical("box");
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = new Color(1f, 0.8f, 0.2f);
            EditorGUILayout.LabelField("💡 " + title, style);
            EditorGUILayout.LabelField(content.Trim(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
}
#endif
