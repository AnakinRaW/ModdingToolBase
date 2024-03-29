using System;
using System.Collections;
using System.Windows.Data;

namespace AnakinRaW.CommonUtilities.Wpf.Utilities;

internal class ItemCollectionAdapter : CollectionAdapter<object, object>
{
    public ItemCollectionAdapter(IEnumerable source)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        Initialize(CollectionViewSource.GetDefaultView(source)); ;
    }

    protected override object AdaptItem(object item)
    {
        return item;
    }
}