using Gameplay.GameControllers.Entities;
using Gameplay.UI;
using Gameplay.UI.Others.UIGameLogic;

namespace Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
/// <summary>
/// Extension methods for Enemies' and Bosses' Health Bar
/// </summary>
public static class EnemyHealthBarExtensions
{
    /// <summary>
    /// Gets the Enemy owner from an EnemyHealthBar
    /// </summary>
    public static Enemy GetOwner(this EnemyHealthBar bar)
    {
        return TraverseUtils.GetValue<Enemy>(bar, "Owner");
    }

    /// <summary>
    /// Gets the target Entity from a BossHealth component
    /// </summary>
    public static Entity GetTarget(this BossHealth bossHealth)
    {
        return TraverseUtils.GetValue<Entity>(bossHealth, "target");
    }

    /// <summary>
    /// Gets the currently active BossHealth instance from the UIController
    /// </summary>
    public static BossHealth GetBossHealth(this UIController UiController)
    {
        return TraverseUtils.GetValue<BossHealth>(UiController, "bossHealth");
    }
}
