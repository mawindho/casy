using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace OLS.Casy.App.Controls
{
    public interface IBindableGridItem
    {
        double WidthPercentage { get; }
    }

    public class BindableGrid : Grid
    {
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(BindableGrid),
                propertyChanged: (bindable, oldValue, newValue) => ((BindableGrid) bindable).PopulateItems());

        public DataTemplate ItemDataTemplate
        {
            get { return (DataTemplate) GetValue(ItemDataTemplateProperty); }
            set { SetValue(ItemDataTemplateProperty, value); }
        }

        public static readonly BindableProperty ItemDataTemplateProperty =
            BindableProperty.Create(nameof(ItemDataTemplate), typeof(DataTemplate), typeof(BindableStackLayout));

        public bool AllInSameRow
        {
            get { return (bool)GetValue(AllInSameRowProperty); }
            set { SetValue(AllInSameRowProperty, value); }
        }

        public static readonly BindableProperty AllInSameRowProperty =
            BindableProperty.Create(nameof(AllInSameRow), typeof(bool), typeof(BindableStackLayout), false);

        private void PopulateItems()
        {
            ColumnDefinitions.Clear();
            RowDefinitions.Clear();
            if (ItemsSource == null) return;

            //var sum = ((IEnumerable<IBindableGridItem>) ItemsSource).Sum(x => ((IBindableGridItem) x).WidthPercentage);

            if (AllInSameRow)
            {
                ColumnDefinitions.Add(new ColumnDefinition()
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

                RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });
            }

            int i = 0;
            foreach (var item in ItemsSource)
            {
                if (!AllInSameRow)
                {
                    var bindableGridItem = item as IBindableGridItem;
                    ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        Width = new GridLength(bindableGridItem.WidthPercentage, GridUnitType.Star)
                    });
                }

                var itemTemplate = ItemDataTemplate.CreateContent() as View;
                itemTemplate.BindingContext = item;

                if (!AllInSameRow)
                {
                    SetColumn(itemTemplate, i++);
                }
                else
                {
                    SetColumn(itemTemplate, 0);
                    SetRow(itemTemplate, 0);
                }

                Children.Add(itemTemplate);
            }
        }
    }
}
