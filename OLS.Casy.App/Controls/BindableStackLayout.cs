﻿using System.Collections;
using Xamarin.Forms;

namespace OLS.Casy.App.Controls
{
    public class BindableStackLayout : StackLayout
    {
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(BindableStackLayout),
                propertyChanged: (bindable, oldValue, newValue) => ((BindableStackLayout) bindable).PopulateItems());

        public DataTemplate ItemDataTemplate
        {
            get { return (DataTemplate) GetValue(ItemDataTemplateProperty); }
            set { SetValue(ItemDataTemplateProperty, value); }
        }

        public static readonly BindableProperty ItemDataTemplateProperty =
            BindableProperty.Create(nameof(ItemDataTemplate), typeof(DataTemplate), typeof(BindableStackLayout));

        private void PopulateItems()
        {
            Children.Clear();
            if (ItemsSource == null) return;
            foreach (var item in ItemsSource)
            {
                var itemTemplate = ItemDataTemplate.CreateContent() as View;
                itemTemplate.BindingContext = item;
                Children.Add(itemTemplate);
            }
        }
    }
}
