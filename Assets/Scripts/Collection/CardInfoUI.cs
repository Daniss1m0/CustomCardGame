using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardInfoUI : MonoBehaviour
{
    public Image Logo;
    public TextMeshProUGUI Name, Attack, Health, Manacost;

    public void SetName(string name) => Name.text = name;
    public void SetLogo(Sprite sprite) => Logo.sprite = sprite;
    public void SetStats(int attack, int health, int mana)
    {
        Attack.text = attack.ToString();
        Health.text = health.ToString();
        Manacost.text = mana.ToString();
    }
}