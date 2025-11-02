using UnityEngine;
using UnityEngine.UI;

public static class HighlightHelper
{
    public static void SetTargetHighlight(Image background, bool active, Color normalColor, Color targetColor)
    {
        if (background == null) 
            return;

        background.color = active ? targetColor : normalColor;
    }

    public static void SetSpellTargetHighlight(Image background, bool active, Color normalColor, Color spellTargetColor)
    {
        if (background == null) 
            return;

        background.color = active ? spellTargetColor : normalColor;
    }

    public static void SetManaAvailability(CanvasGroup canvasGroup, bool available)
    {
        if (canvasGroup == null) 
            return;

        canvasGroup.alpha = available ? 1f : 0.5f;
    }
}
