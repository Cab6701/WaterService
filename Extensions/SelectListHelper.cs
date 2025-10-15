using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using WaterService.Extensions;
using WaterService.Models;

public static class SelectListHelper
{
    public static List<SelectListItem> GetAddressSelectList()
    {
        var selectListItems = Enum.GetValues(typeof(CustomerAddress))
            .Cast<CustomerAddress>()
            .Select(e => new SelectListItem
            {
                Text = e.GetDisplayName(),
                Value = e.GetDisplayName()
            })
            .ToList();

        return selectListItems;
    }
}