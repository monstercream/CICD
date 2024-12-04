using TMPro;

namespace PolygonGraph
{
    public static class ExtensionHelper
    {
        public static void SetStringAnim(this TMP_Text value, int endValue, string name = "", float duration = 0.4f)
        {
            if( !int.TryParse( value.text, out int startValue ) )
                startValue = 0;

            CustomTween.DOInt( startValue, endValue, duration, count => value.text = $"{count} {name}" );
        }

        public static void SetStringAnim(this TMP_Text value, float endValue, string name = "", float duration = 0.4f)
        {
            if( !int.TryParse( value.text, out int startValue ) )
                startValue = 0;

            CustomTween.DOFloat( startValue, endValue, duration, count => value.text = $"{count} {name}" );
        }
    }
}
