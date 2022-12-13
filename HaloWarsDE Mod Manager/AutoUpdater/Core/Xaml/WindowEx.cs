//
// Parago Media GmbH & Co. KG, Jürgen Bäurle (jbaurle@parago.de)
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//

using System.Windows;

namespace AutoUpdater.Core.Xaml
{
    public class WindowEx
    {
        #region HideCloseButton
        public static bool GetHideCloseButton(FrameworkElement element)
            => (bool)element.GetValue(HideCloseButtonProperty);

        public static void SetHideCloseButton(FrameworkElement element, bool hideCloseButton)
            => element.SetValue(HideCloseButtonProperty, hideCloseButton);
        #endregion

        #region IsCloseButtonHidden
        public static bool GetIsCloseButtonHidden(FrameworkElement element)
            => (bool)element.GetValue(IsCloseButtonHiddenProperty);

        public static void SetIsCloseButtonHidden(FrameworkElement element, bool isCloseButtonHidden)
            => element.SetValue(IsHiddenCloseButtonKey, isCloseButtonHidden);
        #endregion

        #region DependencyPropertyKey Registration
        public static readonly DependencyPropertyKey IsHiddenCloseButtonKey =
            DependencyProperty.RegisterAttachedReadOnly(name: "IsCloseButtonHidden",
                propertyType: typeof(bool), ownerType: typeof(WindowEx),
                    defaultMetadata: new FrameworkPropertyMetadata(false));
        #endregion

        #region DependencyProperty Registration
        public static readonly DependencyProperty HideCloseButtonProperty =
             DependencyProperty.RegisterAttached(name: "HideCloseButton",
                 propertyType: typeof(bool), ownerType: typeof(WindowEx),
                    defaultMetadata: new FrameworkPropertyMetadata(false,
                        new PropertyChangedCallback(OnHideCloseButtonPropertyChanged)));

        public static readonly DependencyProperty IsCloseButtonHiddenProperty =
             IsHiddenCloseButtonKey.DependencyProperty;
        #endregion

        #region Event Handlers
        private static readonly RoutedEventHandler OnWindowLoaded = (s, e) => {

            if (s is Window window)
            {
                Utils.HideCloseButton(window);
                window.Loaded -= OnWindowLoaded;
            }
        };

        private static void OnHideCloseButtonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window window = (Window)d;

            if (window != null)
            {
                bool hideCloseButton = (bool)e.NewValue;

                if (hideCloseButton && !GetIsCloseButtonHidden(window))
                {
                    if (!window.IsLoaded)
                        window.Loaded += OnWindowLoaded;
                    else
                        Utils.HideCloseButton(window);

                    SetIsCloseButtonHidden(window, true);
                }
                else if (!hideCloseButton && GetIsCloseButtonHidden(window))
                {
                    if (!window.IsLoaded)
                        window.Loaded -= OnWindowLoaded;
                    else
                        Utils.ShowCloseButton(window);

                    SetIsCloseButtonHidden(window, false);
                }
            }
        }
        #endregion
    }
}
