using System.Windows;
using System.Windows.Controls;

namespace HandShakerSimpleSetup
{
    public class HSProgressBar : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(HSProgressBar));

        static HSProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(HSProgressBar), new FrameworkPropertyMetadata(typeof(HSProgressBar)));
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
    }
}
