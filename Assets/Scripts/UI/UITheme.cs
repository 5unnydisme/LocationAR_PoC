using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "DefaultTheme", menuName = "UI/Theme", order = 0)]
public class UITheme : ScriptableObject
{
    [System.Serializable]
    public class TypeStyle
    {
        public TMP_FontAsset font;
        public int size;

        [Tooltip("Reference metadata only in Phase 1. ThemedText does not force line-height yet.")]
        public float lineHeight;

        public Color color;
    }

    #region Inspector Configuration

    [Header("Palette - Primary")]
    [SerializeField] private Color primary50;
    [SerializeField] private Color primary100;
    [SerializeField] private Color primary200;
    [SerializeField] private Color primary300;
    [SerializeField] private Color primary400;
    [SerializeField] private Color primary500;
    [SerializeField] private Color primary600;

    [Header("Palette - Teal")]
    [SerializeField] private Color teal50;
    [SerializeField] private Color teal100;
    [SerializeField] private Color teal200;
    [SerializeField] private Color teal300;
    [SerializeField] private Color teal400;
    [SerializeField] private Color teal500;
    [SerializeField] private Color teal600;

    [Header("Palette - Yellow")]
    [SerializeField] private Color yellow50;
    [SerializeField] private Color yellow100;
    [SerializeField] private Color yellow200;
    [SerializeField] private Color yellow300;
    [SerializeField] private Color yellow400;
    [SerializeField] private Color yellow500;
    [SerializeField] private Color yellow600;

    [Header("Palette - Red")]
    [SerializeField] private Color red50;
    [SerializeField] private Color red100;
    [SerializeField] private Color red200;
    [SerializeField] private Color red300;
    [SerializeField] private Color red400;
    [SerializeField] private Color red500;
    [SerializeField] private Color red600;

    [Header("Palette - Grey")]
    [SerializeField] private Color grey50;
    [SerializeField] private Color grey100;
    [SerializeField] private Color grey200;
    [SerializeField] private Color grey300;
    [SerializeField] private Color grey400;
    [SerializeField] private Color grey500;
    [SerializeField] private Color grey600;

    [Header("Palette - Base")]
    [SerializeField] private Color white;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset notoRegular;
    [SerializeField] private TMP_FontAsset notoMedium;
    [SerializeField] private TMP_FontAsset notoSemiBold;
    [SerializeField] private TMP_FontAsset notoBold;

    [Header("Type Styles - Semantic")]
    [SerializeField] private TypeStyle displaySemiboldLg;
    [SerializeField] private TypeStyle displaySemiboldMd;
    [SerializeField] private TypeStyle displayMediumLg;
    [SerializeField] private TypeStyle bodyRegularMd;
    [SerializeField] private TypeStyle bodyRegularLg;
    [SerializeField] private TypeStyle bodyMediumMd;
    [SerializeField] private TypeStyle bodyMediumLg;
    [SerializeField] private TypeStyle labelMediumSm;
    [SerializeField] private TypeStyle labelRegularSm;
    [SerializeField] private TypeStyle labelMediumMd;
    [SerializeField] private TypeStyle buttonLabel;

    [Header("Spacing")]
    [SerializeField] private int spacing4 = 4;
    [SerializeField] private int spacing8 = 8;
    [SerializeField] private int spacing12 = 12;
    [SerializeField] private int spacing16 = 16;
    [SerializeField] private int spacing24 = 24;
    [SerializeField] private int spacing32 = 32;

    [Header("Corner Radius")]
    [SerializeField] private int radiusSm = 6;
    [SerializeField] private int radiusMd = 8;
    [SerializeField] private int radiusLg = 10;
    [SerializeField] private int radiusPill = 999;

    #endregion

    public Color Primary50 => primary50;
    public Color Primary100 => primary100;
    public Color Primary200 => primary200;
    public Color Primary300 => primary300;
    public Color Primary400 => primary400;
    public Color Primary500 => primary500;
    public Color Primary600 => primary600;

    public Color Teal50 => teal50;
    public Color Teal100 => teal100;
    public Color Teal200 => teal200;
    public Color Teal300 => teal300;
    public Color Teal400 => teal400;
    public Color Teal500 => teal500;
    public Color Teal600 => teal600;

    public Color Yellow50 => yellow50;
    public Color Yellow100 => yellow100;
    public Color Yellow200 => yellow200;
    public Color Yellow300 => yellow300;
    public Color Yellow400 => yellow400;
    public Color Yellow500 => yellow500;
    public Color Yellow600 => yellow600;

    public Color Red50 => red50;
    public Color Red100 => red100;
    public Color Red200 => red200;
    public Color Red300 => red300;
    public Color Red400 => red400;
    public Color Red500 => red500;
    public Color Red600 => red600;

    public Color Grey50 => grey50;
    public Color Grey100 => grey100;
    public Color Grey200 => grey200;
    public Color Grey300 => grey300;
    public Color Grey400 => grey400;
    public Color Grey500 => grey500;
    public Color Grey600 => grey600;

    public Color White => white;

    public TMP_FontAsset NotoRegular => notoRegular;
    public TMP_FontAsset NotoMedium => notoMedium;
    public TMP_FontAsset NotoSemiBold => notoSemiBold;
    public TMP_FontAsset NotoBold => notoBold;

    public TypeStyle DisplaySemiboldLg => displaySemiboldLg;
    public TypeStyle DisplaySemiboldMd => displaySemiboldMd;
    public TypeStyle DisplayMediumLg => displayMediumLg;
    public TypeStyle BodyRegularMd => bodyRegularMd;
    public TypeStyle BodyRegularLg => bodyRegularLg;
    public TypeStyle BodyMediumMd => bodyMediumMd;
    public TypeStyle BodyMediumLg => bodyMediumLg;
    public TypeStyle LabelMediumSm => labelMediumSm;
    public TypeStyle LabelRegularSm => labelRegularSm;
    public TypeStyle LabelMediumMd => labelMediumMd;
    public TypeStyle ButtonLabel => buttonLabel;

    public int Spacing4 => spacing4;
    public int Spacing8 => spacing8;
    public int Spacing12 => spacing12;
    public int Spacing16 => spacing16;
    public int Spacing24 => spacing24;
    public int Spacing32 => spacing32;

    public int RadiusSm => radiusSm;
    public int RadiusMd => radiusMd;
    public int RadiusLg => radiusLg;
    public int RadiusPill => radiusPill;
}