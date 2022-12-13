using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace HaloWarsDE_Mod_Manager.Core.Xaml
{
    public class ButtonEx: Button
    {
        public ImageBrush IsMouseOverBackground
        {
            get => (ImageBrush)GetValue(IsMouseOverBackgroundProperty);
            set => SetValue(IsMouseOverBackgroundProperty, value);
        }

        public ImageBrush IsPressedBackground
        {
            get => (ImageBrush)GetValue(IsPressedBackgroundProperty);
            set => SetValue(IsPressedBackgroundProperty, value);
        }

        #region DependencyProperty Registration
        public static readonly DependencyProperty IsMouseOverBackgroundProperty =
            DependencyProperty.RegisterAttached(name: "IsMouseOverBackground",
                propertyType: typeof(ImageBrush), ownerType: typeof(ButtonEx));

        public static readonly DependencyProperty IsPressedBackgroundProperty =
            DependencyProperty.RegisterAttached(name: "IsPressedBackground",
                propertyType: typeof(ImageBrush), ownerType: typeof(ButtonEx));
        #endregion
    }
}
