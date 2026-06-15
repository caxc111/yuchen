using System.Windows;
using System.Windows.Controls;

namespace FactoryProductManager.Views
{
    /// <summary>
    /// 根据 SelectedMaterial.IsComposite 选择不同的 DataTemplate：
    /// - IsComposite=true → CompositeTemplate
    /// - 其他 → SingleTemplate（普通物料）
    /// </summary>
    public class SelectedMaterialTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? CompositeTemplate { get; set; }
        public DataTemplate? SingleTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is SelectedMaterial sm && sm.IsComposite)
            {
                return CompositeTemplate;
            }
            return SingleTemplate ?? base.SelectTemplate(item, container);
        }
    }
}
