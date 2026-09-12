using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用 0-9 贴图拼出的数字显示组件。
/// 数字图从 Resources/HudIcons/digit_0 ... digit_9 读取。
/// </summary>
public sealed class HudSpriteNumber : MonoBehaviour
{
    [Header("Layout")]
    public float digitHeight = 46f;
    public float spacing = 2f;
    [Tooltip("单个数字的宽度相对高度的比例，越小数字排列越紧凑。")]
    public float digitWidthRatio = 0.55f;

    private readonly List<Image> digitImages = new List<Image>();
    private static readonly Sprite[] DigitSprites = new Sprite[10];

    private int currentValue = int.MinValue;

    private void Awake()
    {
        EnsureLayout();
    }

    public void SetNumber(int value)
    {
        value = Mathf.Max(0, value);
        if (value == currentValue)
            return;

        currentValue = value;
        EnsureLayout();

        string text = value.ToString();

        while (digitImages.Count < text.Length)
            digitImages.Add(CreateDigitImage());

        for (int i = 0; i < digitImages.Count; i++)
        {
            Image image = digitImages[i];
            bool used = i < text.Length;
            image.gameObject.SetActive(used);

            if (!used)
                continue;

            int digit = text[i] - '0';
            image.sprite = GetDigitSprite(digit);
        }
    }

    public void Clear()
    {
        currentValue = int.MinValue;
        for (int i = 0; i < digitImages.Count; i++)
            digitImages[i].gameObject.SetActive(false);
    }

    private void EnsureLayout()
    {
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private Image CreateDigitImage()
    {
        GameObject digitObject = new GameObject("Digit", typeof(RectTransform));
        digitObject.transform.SetParent(transform, false);

        Image image = digitObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;

        LayoutElement element = digitObject.AddComponent<LayoutElement>();
        element.preferredHeight = digitHeight;
        element.minHeight = digitHeight;
        element.preferredWidth = digitHeight * digitWidthRatio;

        return image;
    }

    private static Sprite GetDigitSprite(int digit)
    {
        if (digit < 0 || digit > 9)
            return null;

        if (DigitSprites[digit] == null)
            DigitSprites[digit] = Resources.Load<Sprite>("HudIcons/digit_" + digit);

        return DigitSprites[digit];
    }
}
