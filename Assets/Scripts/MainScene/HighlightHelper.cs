using UnityEngine;
using UnityEngine.UI;

public static class HighlightHelper
{
    public static void SetTargetHighlight(Image bg, bool active, Color normalCol, Color targetCol)
    {
        bg.color = active ? targetCol : normalCol;
    }

    public static void SetSpellTargetHighlight(Image bg, bool active, Color normalCol, Color spellTargetCol)
    {
        bg.color = active ? spellTargetCol : normalCol;
    }

    public static void SetManaAvailability(CanvasGroup canvasGroup, bool available)
    {
        canvasGroup.alpha = available ? 1f : 0.5f;
    }
}
