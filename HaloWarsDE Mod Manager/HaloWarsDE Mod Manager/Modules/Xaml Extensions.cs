using System.Windows.Media;
using System.Windows;

namespace HaloWarsDE_Mod_Manager.Modules.Xaml
{
    public class ButtonEx
    {
        #region MouseOverBackground
        public static readonly DependencyProperty MouseOverBackgroundProperty =
            DependencyProperty.RegisterAttached("MouseOverBackground",
                typeof(ImageBrush), typeof(ButtonEx));
        public static void SetMouseOverBackground(DependencyObject target, ImageBrush value)
        {
            target.SetValue(MouseOverBackgroundProperty, value);
        }
        public static ImageBrush GetMouseOverBackground(DependencyObject target)
        {
            return (ImageBrush)target.GetValue(MouseOverBackgroundProperty);
        }
        #endregion

        #region IsPressedBackground
        public static readonly DependencyProperty IsPressedBackgroundProperty =
            DependencyProperty.RegisterAttached("IsPressedBackground",
                typeof(ImageBrush), typeof(ButtonEx));

        public static void SetIsPressedBackground(DependencyObject target, ImageBrush value)
        {
            target.SetValue(IsPressedBackgroundProperty, value);
        }
        public static ImageBrush GetIsPressedBackground(DependencyObject target)
        {
            return (ImageBrush)target.GetValue(IsPressedBackgroundProperty);
        }
        #endregion
    }
}
